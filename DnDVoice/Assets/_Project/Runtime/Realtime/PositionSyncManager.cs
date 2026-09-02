using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DndProximityVoice.Map;
using DndProximityVoice.Players;
using DndProximityVoice.Session;
using DndProximityVoice.Voice;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace DndProximityVoice.Realtime
{
    [DisallowMultipleComponent]
    public sealed class PositionSyncManager : MonoBehaviour
    {
        private const string PositionMessageName = "dndpv.positions.v2";
        private const string VoiceModeMessageName = "dndpv.voice-mode.v1";
        private const string RelayConnectionType = "dtls";
        private const byte ProtocolVersion = 6;
        private const int MaximumRelayConnections = 7;
        private const int MaximumPlayersInPacket = 8;
        private const float UnreliableSendIntervalSeconds = 1f / 15f;
        private const float ReliableSnapshotIntervalSeconds = 2f;

        private DiscordSessionManager sessionManager;
        private PlayerManager playerManager;
        private TacticalMapManager tacticalMapManager;
        private NetworkManager networkManager;
        private UnityTransport transport;
        private bool callbacksRegistered;
        private bool messageHandlerRegistered;
        private bool positionsDirty;
        private bool receivedFirstSnapshot;
        private float nextUnreliableSendTime;
        private float nextReliableSnapshotTime;
        private int operationGeneration;
        private readonly List<WallNetworkSnapshot> incomingWalls = new List<WallNetworkSnapshot>();

        public event Action<PositionSyncState> StateChanged;

        public PositionSyncState State { get; private set; } = PositionSyncState.Unavailable;

        public string ErrorMessage { get; private set; } = string.Empty;

        public bool IsConnected => State == PositionSyncState.Connected;

        public bool IsHost => networkManager != null && networkManager.IsHost;

        public int ConnectedFriendCount
        {
            get
            {
                if (networkManager == null || State != PositionSyncState.Connected)
                {
                    return 0;
                }

                return networkManager.IsHost
                    ? Mathf.Max(0, networkManager.ConnectedClientsIds.Count - 1)
                    : networkManager.IsConnectedClient ? 1 : 0;
            }
        }

        public void Initialize(
            DiscordSessionManager discordSession,
            PlayerManager players,
            TacticalMapManager mapManager)
        {
            UnsubscribeFromModel();
            sessionManager = discordSession;
            playerManager = players;
            tacticalMapManager = mapManager;

            if (sessionManager != null)
            {
                sessionManager.StateChanged += OnSessionStateChanged;
            }

            if (playerManager != null)
            {
                playerManager.PlayersChanged += OnPlayersChanged;
                playerManager.LocalVoiceModeChanged += OnLocalVoiceModeChanged;
            }

            if (tacticalMapManager != null)
            {
                tacticalMapManager.MapChanged += OnMapChanged;
            }

            SetState(PositionSyncState.Ready);
        }

        public async Task<string> CreateHostSessionAsync()
        {
            StopNetwork(false);
            var generation = operationGeneration;
            ErrorMessage = string.Empty;
            SetState(PositionSyncState.StartingHost);

            try
            {
                await EnsureServicesReadyAsync();
                if (generation != operationGeneration)
                {
                    return null;
                }

                EnsureNetworkInfrastructure();
                var allocation = await RelayService.Instance.CreateAllocationAsync(MaximumRelayConnections);
                if (generation != operationGeneration)
                {
                    return null;
                }

                var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                if (generation != operationGeneration)
                {
                    return null;
                }

                var normalizedCode = SessionCode.Normalize(joinCode);
                if (!SessionCode.IsValid(normalizedCode))
                {
                    throw new InvalidOperationException("Unity Relay ha restituito un codice non valido.");
                }

                transport.SetRelayServerData(allocation.ToRelayServerData(RelayConnectionType));
                RegisterNetworkCallbacks();
                if (!networkManager.StartHost())
                {
                    throw new InvalidOperationException("Unity Relay non ha avviato il tavolo del DM.");
                }

                RegisterPositionMessageHandler();

                receivedFirstSnapshot = true;
                positionsDirty = true;
                nextUnreliableSendTime = 0f;
                nextReliableSnapshotTime = 0f;
                SetState(PositionSyncState.Connected);
                Debug.Log($"Unity Relay pronto come host. Codice sessione: {normalizedCode}.");
                return normalizedCode;
            }
            catch (Exception exception)
            {
                Fail(
                    "Impossibile avviare la mappa online. Controlla la connessione e riprova.",
                    exception);
                return null;
            }
        }

        public void StopSync()
        {
            StopNetwork(true);
        }

        private async void StartClientSession(string joinCode)
        {
            StopNetwork(false);
            var generation = operationGeneration;
            ErrorMessage = string.Empty;
            SetState(PositionSyncState.Connecting);

            try
            {
                await EnsureServicesReadyAsync();
                if (generation != operationGeneration)
                {
                    return;
                }

                EnsureNetworkInfrastructure();
                var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                if (generation != operationGeneration)
                {
                    return;
                }

                transport.SetRelayServerData(allocation.ToRelayServerData(RelayConnectionType));
                RegisterNetworkCallbacks();
                receivedFirstSnapshot = false;
                if (!networkManager.StartClient())
                {
                    throw new InvalidOperationException("Unity Relay non ha avviato il collegamento del giocatore.");
                }

                RegisterPositionMessageHandler();
                Debug.Log($"Connessione a Unity Relay avviata con codice {joinCode}.");
            }
            catch (Exception exception)
            {
                Fail(
                    "Mappa non sincronizzata. Il codice potrebbe appartenere a una sessione solo Discord.",
                    exception);
            }
        }

        private static async Task EnsureServicesReadyAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        private void EnsureNetworkInfrastructure()
        {
            if (networkManager == null)
            {
                networkManager = gameObject.AddComponent<NetworkManager>();
            }

            if (transport == null)
            {
                transport = gameObject.AddComponent<UnityTransport>();
            }

            networkManager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                ProtocolVersion = ProtocolVersion,
                TickRate = 30,
                EnableSceneManagement = false,
                ForceSamePrefabs = false,
                ConnectionApproval = false
            };
        }

        private void RegisterNetworkCallbacks()
        {
            UnregisterNetworkCallbacks();
            networkManager.OnClientConnectedCallback += OnNetworkClientConnected;
            networkManager.OnClientDisconnectCallback += OnNetworkClientDisconnected;
            callbacksRegistered = true;
        }

        private void RegisterPositionMessageHandler()
        {
            var messaging = networkManager?.CustomMessagingManager;
            if (messaging == null)
            {
                throw new InvalidOperationException(
                    "Netcode non ha inizializzato il canale dati dopo l'avvio della connessione.");
            }

            messaging.RegisterNamedMessageHandler(PositionMessageName, OnPositionSnapshotReceived);
            messaging.RegisterNamedMessageHandler(VoiceModeMessageName, OnVoiceModeRequestReceived);
            messageHandlerRegistered = true;
        }

        private void UnregisterNetworkCallbacks()
        {
            if (networkManager == null)
            {
                return;
            }

            if (callbacksRegistered)
            {
                networkManager.OnClientConnectedCallback -= OnNetworkClientConnected;
                networkManager.OnClientDisconnectCallback -= OnNetworkClientDisconnected;
                callbacksRegistered = false;
            }

            if (messageHandlerRegistered)
            {
                networkManager.CustomMessagingManager?.UnregisterNamedMessageHandler(PositionMessageName);
                networkManager.CustomMessagingManager?.UnregisterNamedMessageHandler(VoiceModeMessageName);
                messageHandlerRegistered = false;
            }
        }

        private void Update()
        {
            if (State != PositionSyncState.Connected || networkManager == null || !networkManager.IsHost)
            {
                return;
            }

            var now = Time.realtimeSinceStartup;
            if (now >= nextReliableSnapshotTime)
            {
                SendSnapshotToAll(NetworkDelivery.ReliableSequenced);
                positionsDirty = false;
                nextReliableSnapshotTime = now + ReliableSnapshotIntervalSeconds;
                nextUnreliableSendTime = now + UnreliableSendIntervalSeconds;
            }
            else if (positionsDirty && now >= nextUnreliableSendTime)
            {
                SendSnapshotToAll(NetworkDelivery.UnreliableSequenced);
                positionsDirty = false;
                nextUnreliableSendTime = now + UnreliableSendIntervalSeconds;
            }
        }

        private void SendSnapshotToAll(NetworkDelivery delivery)
        {
            if (networkManager == null || !networkManager.IsListening || playerManager == null ||
                networkManager.ConnectedClientsIds.Count <= 1)
            {
                return;
            }

            using (var writer = CreateSnapshotWriter())
            {
                networkManager.CustomMessagingManager.SendNamedMessageToAll(
                    PositionMessageName,
                    writer,
                    delivery);
            }
        }

        private void SendSnapshotToClient(ulong clientId)
        {
            if (networkManager == null || !networkManager.IsHost || clientId == NetworkManager.ServerClientId)
            {
                return;
            }

            using (var writer = CreateSnapshotWriter())
            {
                networkManager.CustomMessagingManager.SendNamedMessage(
                    PositionMessageName,
                    clientId,
                    writer,
                    NetworkDelivery.ReliableSequenced);
            }
        }

        private FastBufferWriter CreateSnapshotWriter()
        {
            var playerCount = 0;
            foreach (var player in playerManager.Players)
            {
                if (playerCount < MaximumPlayersInPacket)
                {
                    playerCount++;
                }
            }

            var wallCount = Mathf.Min(
                tacticalMapManager?.Walls.Count ?? 0,
                TacticalMapManager.MaximumWalls);
            var writer = new FastBufferWriter(14 + playerCount * 18 + wallCount * 26, Allocator.Temp);
            writer.WriteValueSafe(ProtocolVersion);
            writer.WriteValueSafe((ushort)playerCount);
            writer.WriteValueSafe((byte)(playerManager.PrivateGroupsIsolated ? 1 : 0));
            var written = 0;
            foreach (var player in playerManager.Players)
            {
                if (written >= playerCount)
                {
                    continue;
                }

                writer.WriteValueSafe(player.DiscordUserId);
                writer.WriteValueSafe(player.Position.x);
                writer.WriteValueSafe(player.Position.y);
                writer.WriteValueSafe((byte)player.VoiceMode);
                writer.WriteValueSafe((byte)player.PrivateGroup);
                written++;
            }

            var mapSize = tacticalMapManager?.MapSizeMeters ?? new Vector2(48f, 48f);
            writer.WriteValueSafe(mapSize.x);
            writer.WriteValueSafe(mapSize.y);
            writer.WriteValueSafe((ushort)wallCount);
            if (tacticalMapManager != null)
            {
                for (var index = 0; index < wallCount; index++)
                {
                    var wall = tacticalMapManager.Walls[index];
                    writer.WriteValueSafe(wall.Id);
                    writer.WriteValueSafe(wall.Start.x);
                    writer.WriteValueSafe(wall.Start.y);
                    writer.WriteValueSafe(wall.End.x);
                    writer.WriteValueSafe(wall.End.y);
                    writer.WriteValueSafe(wall.ThicknessMeters);
                    writer.WriteValueSafe((byte)wall.Kind);
                    writer.WriteValueSafe((byte)wall.State);
                }
            }

            return writer;
        }

        private void OnPositionSnapshotReceived(ulong senderClientId, FastBufferReader reader)
        {
            if (networkManager == null || networkManager.IsServer ||
                senderClientId != NetworkManager.ServerClientId || playerManager == null)
            {
                return;
            }

            try
            {
                reader.ReadValueSafe(out byte version);
                reader.ReadValueSafe(out ushort playerCount);
                if (version != ProtocolVersion || playerCount > MaximumPlayersInPacket)
                {
                    return;
                }

                reader.ReadValueSafe(out byte privateGroupsIsolated);
                playerManager.ApplyAuthoritativePrivateGroupsIsolated(privateGroupsIsolated != 0);

                var snapImmediately = !receivedFirstSnapshot;
                for (var index = 0; index < playerCount; index++)
                {
                    reader.ReadValueSafe(out ulong userId);
                    reader.ReadValueSafe(out float x);
                    reader.ReadValueSafe(out float y);
                    reader.ReadValueSafe(out byte voiceModeValue);
                    reader.ReadValueSafe(out byte privateGroupValue);
                    playerManager.ApplyAuthoritativePosition(userId, new Vector2(x, y), snapImmediately);
                    var voiceMode = (VoiceMode)voiceModeValue;
                    if (VoiceModeProfile.IsValid(voiceMode))
                    {
                        playerManager.ApplyAuthoritativeVoiceMode(userId, voiceMode);
                    }

                    var privateGroup = (PrivateVoiceGroup)privateGroupValue;
                    if (PrivateVoiceGroupRules.IsValid(privateGroup))
                    {
                        playerManager.ApplyAuthoritativePrivateGroup(userId, privateGroup);
                    }
                }

                reader.ReadValueSafe(out float mapWidth);
                reader.ReadValueSafe(out float mapHeight);
                reader.ReadValueSafe(out ushort wallCount);
                if (wallCount > TacticalMapManager.MaximumWalls)
                {
                    return;
                }

                incomingWalls.Clear();
                for (var index = 0; index < wallCount; index++)
                {
                    reader.ReadValueSafe(out int wallId);
                    reader.ReadValueSafe(out float startX);
                    reader.ReadValueSafe(out float startY);
                    reader.ReadValueSafe(out float endX);
                    reader.ReadValueSafe(out float endY);
                    reader.ReadValueSafe(out float thicknessMeters);
                    reader.ReadValueSafe(out byte obstacleKindValue);
                    reader.ReadValueSafe(out byte doorStateValue);
                    incomingWalls.Add(new WallNetworkSnapshot(
                        wallId,
                        new Vector2(startX, startY),
                        new Vector2(endX, endY),
                        thicknessMeters,
                        obstacleKindValue == (byte)AcousticObstacleKind.Door
                            ? AcousticObstacleKind.Door
                            : AcousticObstacleKind.Wall,
                        doorStateValue == (byte)DoorState.Open
                            ? DoorState.Open
                            : doorStateValue == (byte)DoorState.Locked
                                ? DoorState.Locked
                                : DoorState.Closed));
                }

                tacticalMapManager?.ApplyAuthoritativeMap(
                    new Vector2(mapWidth, mapHeight),
                    incomingWalls);

                receivedFirstSnapshot = true;
                if (snapImmediately)
                {
                    Debug.Log($"Prima posizione Relay ricevuta: {playerCount} giocatori.");
                }
            }
            catch (OverflowException exception)
            {
                Debug.LogWarning($"Pacchetto posizione ignorato perché incompleto: {exception.Message}");
            }
        }

        private void OnVoiceModeRequestReceived(ulong senderClientId, FastBufferReader reader)
        {
            if (networkManager == null || !networkManager.IsServer ||
                senderClientId == NetworkManager.ServerClientId || playerManager == null)
            {
                return;
            }

            try
            {
                reader.ReadValueSafe(out ulong userId);
                reader.ReadValueSafe(out byte voiceModeValue);
                var voiceMode = (VoiceMode)voiceModeValue;
                if (VoiceModeProfile.IsValid(voiceMode) &&
                    playerManager.ApplyRequestedVoiceMode(userId, voiceMode))
                {
                    positionsDirty = true;
                    Debug.Log($"Modalità voce aggiornata: utente {userId}, {voiceMode}.");
                }
            }
            catch (OverflowException exception)
            {
                Debug.LogWarning($"Richiesta modalità voce ignorata perché incompleta: {exception.Message}");
            }
        }

        private void OnNetworkClientConnected(ulong clientId)
        {
            if (networkManager == null)
            {
                return;
            }

            if (networkManager.IsHost)
            {
                if (clientId != NetworkManager.ServerClientId)
                {
                    Debug.Log($"Giocatore collegato alla mappa Relay: client {clientId}.");
                }

                SendSnapshotToClient(clientId);
            }
            else if (clientId == networkManager.LocalClientId)
            {
                SetState(PositionSyncState.Connected);
                SendLocalVoiceModeToHost();
                Debug.Log("Mappa collegata al DM tramite Unity Relay.");
            }
        }

        private void OnNetworkClientDisconnected(ulong clientId)
        {
            if (networkManager != null && !networkManager.IsHost && clientId == networkManager.LocalClientId &&
                sessionManager?.State == DiscordSessionState.Joined)
            {
                Fail("La sincronizzazione della mappa si è disconnessa.");
            }
        }

        private void OnPlayersChanged()
        {
            if (networkManager != null && networkManager.IsHost)
            {
                positionsDirty = true;
            }
        }

        private void OnMapChanged()
        {
            if (networkManager != null && networkManager.IsHost)
            {
                positionsDirty = true;
            }
        }

        private void OnLocalVoiceModeChanged(VoiceMode voiceMode)
        {
            if (networkManager != null && networkManager.IsHost)
            {
                positionsDirty = true;
                return;
            }

            SendLocalVoiceModeToHost();
        }

        private void SendLocalVoiceModeToHost()
        {
            var localPlayer = playerManager?.LocalPlayer;
            if (localPlayer == null || networkManager == null || !networkManager.IsConnectedClient ||
                networkManager.IsHost || networkManager.CustomMessagingManager == null)
            {
                return;
            }

            using (var writer = new FastBufferWriter(9, Allocator.Temp))
            {
                writer.WriteValueSafe(localPlayer.DiscordUserId);
                writer.WriteValueSafe((byte)localPlayer.VoiceMode);
                networkManager.CustomMessagingManager.SendNamedMessage(
                    VoiceModeMessageName,
                    NetworkManager.ServerClientId,
                    writer,
                    NetworkDelivery.ReliableSequenced);
            }
        }

        private void OnSessionStateChanged(DiscordSessionState sessionState)
        {
            if (sessionState == DiscordSessionState.Joined)
            {
                if (!sessionManager.IsHost && State != PositionSyncState.Connecting &&
                    State != PositionSyncState.Connected)
                {
                    StartClientSession(sessionManager.CurrentSessionCode);
                }

                return;
            }

            if (sessionState == DiscordSessionState.Ready ||
                sessionState == DiscordSessionState.WaitingForDiscord)
            {
                StopNetwork(true);
            }
        }

        private void StopNetwork(bool returnToReady)
        {
            operationGeneration++;
            UnregisterNetworkCallbacks();
            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            positionsDirty = false;
            receivedFirstSnapshot = false;
            nextUnreliableSendTime = 0f;
            nextReliableSnapshotTime = 0f;
            if (returnToReady)
            {
                ErrorMessage = string.Empty;
                SetState(PositionSyncState.Ready);
            }
        }

        private void Fail(string message, Exception exception = null)
        {
            operationGeneration++;
            UnregisterNetworkCallbacks();
            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            ErrorMessage = message;
            if (exception == null)
            {
                Debug.LogWarning(message);
            }
            else
            {
                Debug.LogWarning($"{message}\n{exception}");
            }

            SetState(PositionSyncState.Failed);
        }

        private void SetState(PositionSyncState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke(state);
        }

        private void UnsubscribeFromModel()
        {
            if (sessionManager != null)
            {
                sessionManager.StateChanged -= OnSessionStateChanged;
            }

            if (playerManager != null)
            {
                playerManager.PlayersChanged -= OnPlayersChanged;
                playerManager.LocalVoiceModeChanged -= OnLocalVoiceModeChanged;
            }

            if (tacticalMapManager != null)
            {
                tacticalMapManager.MapChanged -= OnMapChanged;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromModel();
            StopNetwork(false);
            sessionManager = null;
            playerManager = null;
            tacticalMapManager = null;
            networkManager = null;
            transport = null;
        }
    }
}
