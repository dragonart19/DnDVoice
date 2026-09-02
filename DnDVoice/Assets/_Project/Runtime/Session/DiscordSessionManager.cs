using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Sdk;
using DndProximityVoice.Discord;
using DndProximityVoice.Realtime;
using UnityEngine;

namespace DndProximityVoice.Session
{
    [DisallowMultipleComponent]
    public sealed class DiscordSessionManager : MonoBehaviour
    {
        private const string MetadataApplicationKey = "application";
        private const string MetadataApplicationValue = "dnd-proximity-voice";
        private const string MetadataCodeKey = "session_code";
        private const string MetadataHostKey = "host_id";
        private const string MetadataProtocolKey = "protocol";
        private const string MetadataProtocolValue = "6";
        private const float LobbyMetadataTimeoutSeconds = 10f;
        private const float LobbyMetadataRetrySeconds = 0.25f;

        private readonly List<SessionMemberSnapshot> members = new List<SessionMemberSnapshot>();

        private DiscordAuthManager authManager;
        private PositionSyncManager positionSyncManager;
        private string pendingSessionCode = string.Empty;
        private bool pendingAsHost;
        private bool callbacksRegistered;
        private bool waitingForLobbyMetadata;
        private float lobbyMetadataDeadline;
        private float nextLobbyMetadataCheck;
        private string lobbyMetadataValidationError = string.Empty;

        public event Action<DiscordSessionState> StateChanged;

        public event Action MembersChanged;

        public DiscordSessionState State { get; private set; } = DiscordSessionState.WaitingForDiscord;

        public string ErrorMessage { get; private set; } = string.Empty;

        public string CurrentSessionCode { get; private set; } = string.Empty;

        public ulong LobbyId { get; private set; }

        public ulong HostUserId { get; private set; }

        public bool IsHost => authManager?.CurrentUser != null && authManager.CurrentUser.Id == HostUserId;

        public IReadOnlyList<SessionMemberSnapshot> Members => members;

        public bool CanCreateOrJoin => State == DiscordSessionState.Ready || State == DiscordSessionState.Failed;

        public void Initialize(DiscordAuthManager manager, PositionSyncManager syncManager)
        {
            if (authManager != null)
            {
                authManager.StateChanged -= OnDiscordAuthStateChanged;
            }

            authManager = manager;
            positionSyncManager = syncManager;
            if (authManager == null)
            {
                SetState(DiscordSessionState.WaitingForDiscord);
                return;
            }

            authManager.StateChanged += OnDiscordAuthStateChanged;
            OnDiscordAuthStateChanged(authManager.State);
        }

        public void CreateSession()
        {
            _ = CreateSessionAsync();
        }

        public void JoinSession(string code)
        {
            BeginJoin(code, false);
        }

        private async Task CreateSessionAsync()
        {
            if (!CanCreateOrJoin || authManager?.State != DiscordAuthState.Connected || authManager.Client == null)
            {
                return;
            }

            ErrorMessage = string.Empty;
            SetState(DiscordSessionState.Joining);
            var relayCode = positionSyncManager == null
                ? null
                : await positionSyncManager.CreateHostSessionAsync();

            if (State != DiscordSessionState.Joining || authManager?.State != DiscordAuthState.Connected ||
                authManager.Client == null)
            {
                positionSyncManager?.StopSync();
                return;
            }

            if (!SessionCode.IsValid(relayCode))
            {
                Fail(
                    string.IsNullOrWhiteSpace(positionSyncManager?.ErrorMessage)
                        ? "Non riesco ad avviare la mappa online. Riprova tra poco."
                        : positionSyncManager.ErrorMessage);
                return;
            }

            BeginPreparedJoin(relayCode, true);
        }

        public void LeaveSession()
        {
            if (State != DiscordSessionState.Joined || LobbyId == 0 || authManager?.Client == null)
            {
                return;
            }

            SetState(DiscordSessionState.Leaving);
            authManager.Client.LeaveLobby(LobbyId, OnLeaveCompleted);
        }

        public void DismissError()
        {
            if (State != DiscordSessionState.Failed)
            {
                return;
            }

            ErrorMessage = string.Empty;
            SetState(LobbyId == 0 ? DiscordSessionState.Ready : DiscordSessionState.Joined);
        }

        private void Update()
        {
            if (!waitingForLobbyMetadata || Time.realtimeSinceStartup < nextLobbyMetadataCheck)
            {
                return;
            }

            if (TryReadAndValidateLobbyMetadata(pendingAsHost, out var validationError))
            {
                CompleteLobbyJoin();
                return;
            }

            lobbyMetadataValidationError = validationError;
            if (Time.realtimeSinceStartup < lobbyMetadataDeadline)
            {
                nextLobbyMetadataCheck = Time.realtimeSinceStartup + LobbyMetadataRetrySeconds;
                return;
            }

            waitingForLobbyMetadata = false;
            var lobbyToLeave = LobbyId;
            if (lobbyToLeave != 0 && authManager?.Client != null)
            {
                authManager.Client.LeaveLobby(lobbyToLeave, leaveResult =>
                {
                    ResetSessionData();
                    Fail(lobbyMetadataValidationError);
                });
            }
            else
            {
                ResetSessionData();
                Fail(lobbyMetadataValidationError);
            }
        }

        private void BeginJoin(string code, bool asHost)
        {
            if (!CanCreateOrJoin || authManager?.State != DiscordAuthState.Connected || authManager.Client == null)
            {
                return;
            }

            var normalizedCode = SessionCode.Normalize(code);
            if (!SessionCode.IsValid(normalizedCode))
            {
                Fail($"Inserisci un codice di {SessionCode.Length} caratteri.");
                return;
            }

            ErrorMessage = string.Empty;
            SetState(DiscordSessionState.Joining);
            BeginPreparedJoin(normalizedCode, asHost);
        }

        private void BeginPreparedJoin(string normalizedCode, bool asHost)
        {
            pendingSessionCode = normalizedCode;
            pendingAsHost = asHost;

            var secret = SessionCode.DeriveLobbySecret(normalizedCode);
            if (asHost)
            {
                var lobbyMetadata = new Dictionary<string, string>
                {
                    [MetadataApplicationKey] = MetadataApplicationValue,
                    [MetadataCodeKey] = normalizedCode,
                    [MetadataHostKey] = authManager.CurrentUser.Id.ToString(),
                    [MetadataProtocolKey] = MetadataProtocolValue
                };

                var memberMetadata = new Dictionary<string, string>
                {
                    ["role"] = "dm"
                };

                authManager.Client.CreateOrJoinLobbyWithMetadata(
                    secret,
                    lobbyMetadata,
                    memberMetadata,
                    OnCreateOrJoinCompleted);
            }
            else
            {
                authManager.Client.CreateOrJoinLobby(secret, OnCreateOrJoinCompleted);
            }
        }

        private void OnCreateOrJoinCompleted(ClientResult result, ulong lobbyId)
        {
            if (!result.Successful() || lobbyId == 0)
            {
                positionSyncManager?.StopSync();
                Fail($"Impossibile entrare nella sessione: {result.Error()}.");
                return;
            }

            LobbyId = lobbyId;
            CurrentSessionCode = pendingSessionCode;
            waitingForLobbyMetadata = true;
            lobbyMetadataDeadline = Time.realtimeSinceStartup + LobbyMetadataTimeoutSeconds;
            nextLobbyMetadataCheck = Time.realtimeSinceStartup;
            lobbyMetadataValidationError = pendingAsHost
                ? "Discord non ha confermato la creazione della sessione."
                : "Sessione non trovata. Controlla il codice e riprova.";

            if (TryReadAndValidateLobbyMetadata(pendingAsHost, out var validationError))
            {
                CompleteLobbyJoin();
            }
            else
            {
                lobbyMetadataValidationError = validationError;
                nextLobbyMetadataCheck = Time.realtimeSinceStartup + LobbyMetadataRetrySeconds;
            }
        }

        private void CompleteLobbyJoin()
        {
            waitingForLobbyMetadata = false;
            lobbyMetadataValidationError = string.Empty;
            pendingSessionCode = string.Empty;
            pendingAsHost = false;
            RefreshMembers();
            SetState(DiscordSessionState.Joined);
        }

        private bool TryReadAndValidateLobbyMetadata(bool createdAsHost, out string validationError)
        {
            validationError = string.Empty;
            using (var lobby = authManager.Client.GetLobbyHandle(LobbyId))
            {
                if (lobby == null)
                {
                    validationError = "Discord non ha caricato i dati della sessione.";
                    return false;
                }

                var metadata = lobby.Metadata();
                var hasValidApplication = metadata.TryGetValue(MetadataApplicationKey, out var application) &&
                                          application == MetadataApplicationValue;
                var hasValidCode = metadata.TryGetValue(MetadataCodeKey, out var code) &&
                                   code == CurrentSessionCode;
                var hasValidProtocol = metadata.TryGetValue(MetadataProtocolKey, out var protocol) &&
                                       protocol == MetadataProtocolValue;
                var hostId = 0UL;
                var hasHost = metadata.TryGetValue(MetadataHostKey, out var hostIdText) &&
                              ulong.TryParse(hostIdText, out hostId);

                if (!hasValidApplication || !hasValidCode || !hasValidProtocol || !hasHost)
                {
                    if (!createdAsHost && TryInferHostFromExistingMembers(lobby, out var inferredHostId))
                    {
                        HostUserId = inferredHostId;
                        Debug.LogWarning(
                            "Discord lobby metadata was not available on the joining client. " +
                            "The host was identified from the connected member list instead.");
                        return true;
                    }

                    validationError = createdAsHost
                        ? "Discord non ha confermato la creazione della sessione."
                        : "Sessione non trovata. Controlla il codice e riprova.";
                    return false;
                }

                HostUserId = hostId;
                return true;
            }
        }

        private bool TryInferHostFromExistingMembers(LobbyHandle lobby, out ulong inferredHostId)
        {
            inferredHostId = 0;
            var localUserId = authManager?.CurrentUser?.Id ?? 0;
            var lobbyMembers = lobby.LobbyMembers();
            foreach (var lobbyMember in lobbyMembers)
            {
                using (lobbyMember)
                {
                    if (lobbyMember.Id() != localUserId && lobbyMember.Connected())
                    {
                        inferredHostId = lobbyMember.Id();
                        return true;
                    }
                }
            }

            return false;
        }

        private void RegisterLobbyCallbacks()
        {
            if (callbacksRegistered || authManager?.Client == null)
            {
                return;
            }

            authManager.Client.SetLobbyDeletedCallback(OnLobbyDeleted);
            authManager.Client.SetLobbyMemberAddedCallback(OnLobbyMemberChanged);
            authManager.Client.SetLobbyMemberRemovedCallback(OnLobbyMemberChanged);
            authManager.Client.SetLobbyMemberUpdatedCallback(OnLobbyMemberChanged);
            authManager.Client.SetLobbyUpdatedCallback(OnLobbyUpdated);
            callbacksRegistered = true;
        }

        private void OnLobbyUpdated(ulong lobbyId)
        {
            if (waitingForLobbyMetadata && lobbyId == LobbyId)
            {
                nextLobbyMetadataCheck = 0f;
            }
        }

        private void OnLobbyDeleted(ulong lobbyId)
        {
            if (lobbyId != LobbyId)
            {
                return;
            }

            ResetSessionData();
            Fail("La sessione Discord non è più disponibile.");
        }

        private void OnLobbyMemberChanged(ulong lobbyId, ulong memberId)
        {
            if (lobbyId != LobbyId)
            {
                return;
            }

            if (waitingForLobbyMetadata)
            {
                nextLobbyMetadataCheck = 0f;
            }
            else if (State == DiscordSessionState.Joined)
            {
                RefreshMembers();
            }
        }

        private void RefreshMembers()
        {
            members.Clear();

            using (var lobby = authManager?.Client?.GetLobbyHandle(LobbyId))
            {
                if (lobby == null)
                {
                    MembersChanged?.Invoke();
                    return;
                }

                var lobbyMembers = lobby.LobbyMembers();
                foreach (var lobbyMember in lobbyMembers)
                {
                    using (lobbyMember)
                    {
                        var memberId = lobbyMember.Id();
                        var displayName = memberId.ToString();
                        using (var user = lobbyMember.User())
                        {
                            if (user != null)
                            {
                                displayName = user.DisplayName();
                            }
                        }

                        members.Add(new SessionMemberSnapshot(
                            memberId,
                            displayName,
                            lobbyMember.Connected(),
                            memberId == HostUserId,
                            memberId == authManager.CurrentUser?.Id));
                    }
                }
            }

            members.Sort((left, right) =>
            {
                if (left.IsHost != right.IsHost)
                {
                    return left.IsHost ? -1 : 1;
                }

                if (left.IsLocal != right.IsLocal)
                {
                    return left.IsLocal ? -1 : 1;
                }

                return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
            });
            MembersChanged?.Invoke();
        }

        private void OnLeaveCompleted(ClientResult result)
        {
            if (!result.Successful())
            {
                Fail($"Non è stato possibile uscire dalla sessione: {result.Error()}.");
                return;
            }

            ResetSessionData();
            SetState(DiscordSessionState.Ready);
        }

        private void OnDiscordAuthStateChanged(DiscordAuthState authState)
        {
            if (authState == DiscordAuthState.Connected)
            {
                RegisterLobbyCallbacks();
                if (LobbyId == 0)
                {
                    SetState(DiscordSessionState.Ready);
                }
            }
            else
            {
                ResetSessionData();
                SetState(DiscordSessionState.WaitingForDiscord);
            }
        }

        private void ResetSessionData()
        {
            positionSyncManager?.StopSync();
            LobbyId = 0;
            HostUserId = 0;
            CurrentSessionCode = string.Empty;
            pendingSessionCode = string.Empty;
            pendingAsHost = false;
            waitingForLobbyMetadata = false;
            lobbyMetadataDeadline = 0f;
            nextLobbyMetadataCheck = 0f;
            lobbyMetadataValidationError = string.Empty;
            members.Clear();
            MembersChanged?.Invoke();
        }

        private void Fail(string message)
        {
            pendingSessionCode = string.Empty;
            pendingAsHost = false;
            waitingForLobbyMetadata = false;
            ErrorMessage = message;
            Debug.LogError(message);
            SetState(DiscordSessionState.Failed);
        }

        private void SetState(DiscordSessionState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke(state);
        }

        private void OnDestroy()
        {
            if (authManager != null)
            {
                authManager.StateChanged -= OnDiscordAuthStateChanged;
            }

            authManager = null;
            positionSyncManager = null;
        }
    }
}
