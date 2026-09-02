using System;
using System.Collections.Generic;
using DndProximityVoice.Map;
using DndProximityVoice.Session;
using DndProximityVoice.Voice;
using UnityEngine;

namespace DndProximityVoice.Players
{
    [DisallowMultipleComponent]
    public sealed class PlayerManager : MonoBehaviour
    {
        private const float NetworkPositionSmoothingSpeed = 18f;
        private const float PositionSnapEpsilon = 0.0025f;

        private static readonly Color[] PlayerColors =
        {
            new Color32(88, 166, 255, 255),
            new Color32(255, 184, 77, 255),
            new Color32(190, 112, 255, 255),
            new Color32(75, 210, 160, 255),
            new Color32(255, 105, 135, 255),
            new Color32(86, 214, 225, 255)
        };

        private readonly List<PlayerData> players = new List<PlayerData>();
        private readonly Dictionary<ulong, PlayerData> playersById =
            new Dictionary<ulong, PlayerData>();

        private DiscordSessionManager sessionManager;
        private TacticalMapManager tacticalMapManager;

        public event Action PlayersChanged;

        public event Action<VoiceMode> LocalVoiceModeChanged;

        public IReadOnlyList<PlayerData> Players => players;

        public bool CanMovePlayers => sessionManager?.State == DiscordSessionState.Joined &&
                                      sessionManager.IsHost;

        public bool PrivateGroupsIsolated { get; private set; }

        public PlayerData LocalPlayer
        {
            get
            {
                foreach (var player in players)
                {
                    if (player.IsLocal)
                    {
                        return player;
                    }
                }

                return null;
            }
        }

        public void Initialize(
            DiscordSessionManager discordSessionManager,
            TacticalMapManager mapManager)
        {
            if (sessionManager != null)
            {
                sessionManager.MembersChanged -= RefreshFromSession;
                sessionManager.StateChanged -= OnSessionStateChanged;
            }

            sessionManager = discordSessionManager;
            if (tacticalMapManager != null)
            {
                tacticalMapManager.MapChanged -= OnMapChanged;
            }

            tacticalMapManager = mapManager;
            if (sessionManager == null)
            {
                return;
            }

            sessionManager.MembersChanged += RefreshFromSession;
            sessionManager.StateChanged += OnSessionStateChanged;
            if (tacticalMapManager != null)
            {
                tacticalMapManager.MapChanged += OnMapChanged;
            }

            RefreshFromSession();
        }

        public PlayerData GetPlayer(ulong userId)
        {
            playersById.TryGetValue(userId, out var player);
            return player;
        }

        public bool TryMovePlayer(ulong userId, Vector2 position)
        {
            if (!CanMovePlayers || !playersById.TryGetValue(userId, out var player))
            {
                return false;
            }

            player.Position = ClampToMap(position);
            player.TargetPosition = player.Position;
            PlayersChanged?.Invoke();
            return true;
        }

        public bool TrySetPrivateGroup(ulong userId, PrivateVoiceGroup group)
        {
            if (!CanMovePlayers || !PrivateVoiceGroupRules.IsValid(group) ||
                !playersById.TryGetValue(userId, out var player))
            {
                return false;
            }

            if (player.PrivateGroup != group)
            {
                player.PrivateGroup = group;
                PlayersChanged?.Invoke();
            }

            return true;
        }

        public bool TrySetPrivateGroupsIsolated(bool isolated)
        {
            if (!CanMovePlayers)
            {
                return false;
            }

            if (PrivateGroupsIsolated != isolated)
            {
                PrivateGroupsIsolated = isolated;
                PlayersChanged?.Invoke();
            }

            return true;
        }

        public bool ApplyAuthoritativePrivateGroup(ulong userId, PrivateVoiceGroup group)
        {
            if (CanMovePlayers || !PrivateVoiceGroupRules.IsValid(group) ||
                !playersById.TryGetValue(userId, out var player))
            {
                return false;
            }

            if (player.PrivateGroup != group)
            {
                player.PrivateGroup = group;
                PlayersChanged?.Invoke();
            }

            return true;
        }

        public bool ApplyAuthoritativePrivateGroupsIsolated(bool isolated)
        {
            if (CanMovePlayers)
            {
                return false;
            }

            if (PrivateGroupsIsolated != isolated)
            {
                PrivateGroupsIsolated = isolated;
                PlayersChanged?.Invoke();
            }

            return true;
        }

        public bool TrySetLocalVoiceMode(VoiceMode mode)
        {
            var localPlayer = LocalPlayer;
            if (sessionManager?.State != DiscordSessionState.Joined || localPlayer == null ||
                !VoiceModeProfile.IsValid(mode))
            {
                return false;
            }

            if (localPlayer.VoiceMode == mode)
            {
                return true;
            }

            localPlayer.VoiceMode = mode;
            PlayersChanged?.Invoke();
            LocalVoiceModeChanged?.Invoke(mode);
            return true;
        }

        public bool ApplyRequestedVoiceMode(ulong userId, VoiceMode mode)
        {
            if (!CanMovePlayers || !VoiceModeProfile.IsValid(mode) ||
                !playersById.TryGetValue(userId, out var player))
            {
                return false;
            }

            if (player.VoiceMode != mode)
            {
                player.VoiceMode = mode;
                PlayersChanged?.Invoke();
            }

            return true;
        }

        public bool ApplyAuthoritativeVoiceMode(ulong userId, VoiceMode mode)
        {
            if (CanMovePlayers || !VoiceModeProfile.IsValid(mode) ||
                !playersById.TryGetValue(userId, out var player))
            {
                return false;
            }

            if (player.VoiceMode != mode)
            {
                player.VoiceMode = mode;
                PlayersChanged?.Invoke();
            }

            return true;
        }

        public bool ApplyAuthoritativePosition(ulong userId, Vector2 position, bool snapImmediately)
        {
            if (CanMovePlayers || !playersById.TryGetValue(userId, out var player))
            {
                return false;
            }

            player.TargetPosition = ClampToMap(position);
            if (snapImmediately)
            {
                player.Position = player.TargetPosition;
                PlayersChanged?.Invoke();
            }

            return true;
        }

        private void Update()
        {
            if (CanMovePlayers || sessionManager?.State != DiscordSessionState.Joined)
            {
                return;
            }

            var interpolation = 1f - Mathf.Exp(-NetworkPositionSmoothingSpeed * Time.unscaledDeltaTime);
            var changed = false;
            foreach (var player in players)
            {
                var delta = player.TargetPosition - player.Position;
                if (delta.sqrMagnitude <= PositionSnapEpsilon * PositionSnapEpsilon)
                {
                    if (player.Position != player.TargetPosition)
                    {
                        player.Position = player.TargetPosition;
                        changed = true;
                    }

                    continue;
                }

                player.Position = Vector2.Lerp(player.Position, player.TargetPosition, interpolation);
                changed = true;
            }

            if (changed)
            {
                PlayersChanged?.Invoke();
            }
        }

        private void RefreshFromSession()
        {
            if (sessionManager == null || sessionManager.State != DiscordSessionState.Joined)
            {
                return;
            }

            var activeMemberIds = new HashSet<ulong>();
            foreach (var member in sessionManager.Members)
            {
                activeMemberIds.Add(member.Id);
                if (!playersById.TryGetValue(member.Id, out var player))
                {
                    var position = member.IsLocal
                        ? Vector2.zero
                        : CreateInitialPosition(players.Count);
                    player = new PlayerData(
                        member.Id,
                        member.DisplayName,
                        position,
                        PlayerColors[players.Count % PlayerColors.Length]);
                    AddPlayer(player);
                }

                player.DisplayName = member.DisplayName;
                player.IsDM = member.IsHost;
                player.IsLocal = member.IsLocal;
                player.IsConnected = member.Connected;
            }

            for (var index = players.Count - 1; index >= 0; index--)
            {
                var player = players[index];
                if (!activeMemberIds.Contains(player.DiscordUserId))
                {
                    RemovePlayer(player.DiscordUserId);
                }
            }

            SortPlayers();
            PlayersChanged?.Invoke();
        }

        private void OnSessionStateChanged(DiscordSessionState state)
        {
            if (state == DiscordSessionState.Joined)
            {
                RefreshFromSession();
            }
            else if (state == DiscordSessionState.Ready || state == DiscordSessionState.WaitingForDiscord)
            {
                players.Clear();
                playersById.Clear();
                PrivateGroupsIsolated = false;
                PlayersChanged?.Invoke();
            }
        }

        private void AddPlayer(PlayerData player)
        {
            players.Add(player);
            playersById.Add(player.DiscordUserId, player);
        }

        private void RemovePlayer(ulong userId)
        {
            if (!playersById.TryGetValue(userId, out var player))
            {
                return;
            }

            playersById.Remove(userId);
            players.Remove(player);
        }

        private void SortPlayers()
        {
            players.Sort((left, right) =>
            {
                if (left.IsDM != right.IsDM)
                {
                    return left.IsDM ? -1 : 1;
                }

                if (left.IsLocal != right.IsLocal)
                {
                    return left.IsLocal ? -1 : 1;
                }

                return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static Vector2 CreateInitialPosition(int index)
        {
            var angle = index * 2.399963f;
            var radius = 3f + ((index % 3) * 1.5f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private Vector2 ClampToMap(Vector2 position)
        {
            return tacticalMapManager != null
                ? tacticalMapManager.ClampPosition(position)
                : position;
        }

        private void OnMapChanged()
        {
            if (!CanMovePlayers)
            {
                return;
            }

            var changed = false;
            foreach (var player in players)
            {
                var clamped = ClampToMap(player.Position);
                if (clamped == player.Position)
                {
                    continue;
                }

                player.Position = clamped;
                player.TargetPosition = clamped;
                changed = true;
            }

            if (changed)
            {
                PlayersChanged?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (sessionManager != null)
            {
                sessionManager.MembersChanged -= RefreshFromSession;
                sessionManager.StateChanged -= OnSessionStateChanged;
            }

            if (tacticalMapManager != null)
            {
                tacticalMapManager.MapChanged -= OnMapChanged;
            }

            sessionManager = null;
            tacticalMapManager = null;
        }
    }
}
