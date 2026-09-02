using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Discord.Sdk;
using DndProximityVoice.Discord;
using DndProximityVoice.Map;
using DndProximityVoice.Players;
using DndProximityVoice.Session;
using UnityEngine;

namespace DndProximityVoice.Voice
{
    [DisallowMultipleComponent]
    public sealed class DiscordVoiceManager : MonoBehaviour
    {
        private readonly HashSet<ulong> speakingUsers = new HashSet<ulong>();
        private readonly object speakingUsersLock = new object();
        private readonly object remoteAudioLock = new object();
        private readonly Dictionary<ulong, float> directPanByUser = new Dictionary<ulong, float>();
        private readonly Dictionary<ulong, DirectPcmPanner> directPanners =
            new Dictionary<ulong, DirectPcmPanner>();

        private DiscordAuthManager authManager;
        private DiscordSessionManager sessionManager;
        private PlayerManager playerManager;
        private TacticalMapManager tacticalMapManager;
        private Call call;
        private ulong activeLobbyId;
        private long capturedFrameCount;
        private long receivedFrameCount;
        private long capturedSampleCount;
        private long receivedSampleCount;
        private bool spatialPositionsDirty = true;
        private AnimationCurve directAttenuationCurve;

        public event Action<DiscordVoiceState> StateChanged;

        public event Action VoiceParticipantsChanged;

        public DiscordVoiceState State { get; private set; } = DiscordVoiceState.Unavailable;

        public string ErrorMessage { get; private set; } = string.Empty;

        public bool IsSelfMuted { get; private set; }

        public long CapturedFrameCount => Interlocked.Read(ref capturedFrameCount);

        public long ReceivedFrameCount => Interlocked.Read(ref receivedFrameCount);

        public long CapturedSampleCount => Interlocked.Read(ref capturedSampleCount);

        public long ReceivedSampleCount => Interlocked.Read(ref receivedSampleCount);

        public bool CanStart => call == null &&
                                sessionManager?.State == DiscordSessionState.Joined &&
                                (State == DiscordVoiceState.Ready || State == DiscordVoiceState.Failed);

        public int ParticipantCount
        {
            get
            {
                if (call == null || State == DiscordVoiceState.Unavailable)
                {
                    return 0;
                }

                try
                {
                    return call.GetParticipants().Length;
                }
                catch (ObjectDisposedException)
                {
                    return 0;
                }
            }
        }

        public void Initialize(
            DiscordAuthManager discordAuth,
            DiscordSessionManager discordSession,
            PlayerManager players,
            TacticalMapManager mapManager)
        {
            if (sessionManager != null)
            {
                sessionManager.StateChanged -= OnSessionStateChanged;
            }

            if (playerManager != null)
            {
                playerManager.PlayersChanged -= OnPlayersChanged;
            }

            if (tacticalMapManager != null)
            {
                tacticalMapManager.MapChanged -= OnMapChanged;
            }

            authManager = discordAuth;
            sessionManager = discordSession;
            playerManager = players;
            tacticalMapManager = mapManager;
            if (sessionManager == null)
            {
                SetState(DiscordVoiceState.Unavailable);
                return;
            }

            sessionManager.StateChanged += OnSessionStateChanged;
            if (playerManager != null)
            {
                playerManager.PlayersChanged += OnPlayersChanged;
            }

            if (tacticalMapManager != null)
            {
                tacticalMapManager.MapChanged += OnMapChanged;
            }

            spatialPositionsDirty = true;
            OnSessionStateChanged(sessionManager.State);
        }

        public void StartVoice()
        {
            if (!CanStart || authManager?.Client == null || sessionManager.LobbyId == 0)
            {
                return;
            }

            ErrorMessage = string.Empty;
            ResetAudioCounters();
            activeLobbyId = sessionManager.LobbyId;
            spatialPositionsDirty = true;
            SetState(DiscordVoiceState.Starting);

            try
            {
                call = authManager.Client.StartCallWithAudioCallbacks(
                    activeLobbyId,
                    OnUserAudioReceived,
                    OnUserAudioCaptured);

                if (call == null)
                {
                    Fail("La chiamata vocale risulta già aperta. Esci e rientra nella sessione.");
                    return;
                }

                call.SetStatusChangedCallback(OnCallStatusChanged);
                call.SetParticipantChangedCallback(OnCallParticipantChanged);
                call.SetSpeakingStatusChangedCallback(OnSpeakingStatusChanged);
                call.SetOnVoiceStateChangedCallback(OnVoiceStateChanged);
                IsSelfMuted = call.GetSelfMute();
                OnCallStatusChanged(call.GetStatus(), Call.Error.None, 0);
            }
            catch (Exception exception)
            {
                Fail("Non è stato possibile avviare la voce Discord.", exception);
            }
        }

        public void StopVoice()
        {
            if (call == null || authManager?.Client == null || activeLobbyId == 0 ||
                State == DiscordVoiceState.Stopping)
            {
                return;
            }

            SetState(DiscordVoiceState.Stopping);
            try
            {
                authManager.Client.EndCall(activeLobbyId, OnEndCallCompleted);
            }
            catch (Exception exception)
            {
                DisposeCall();
                Fail("Non è stato possibile chiudere correttamente la voce Discord.", exception);
            }
        }

        public void ToggleSelfMute()
        {
            if (call == null || State != DiscordVoiceState.Connected)
            {
                return;
            }

            try
            {
                IsSelfMuted = !IsSelfMuted;
                call.SetSelfMute(IsSelfMuted);
                VoiceParticipantsChanged?.Invoke();
            }
            catch (Exception exception)
            {
                Fail("Non è stato possibile cambiare lo stato del microfono.", exception);
            }
        }

        public bool IsUserSpeaking(ulong userId)
        {
            lock (speakingUsersLock)
            {
                return speakingUsers.Contains(userId);
            }
        }

        private void Update()
        {
            ApplyRemoteSpatialPositionsIfNeeded();
        }

        private void OnPlayersChanged()
        {
            spatialPositionsDirty = true;
        }

        private void OnMapChanged()
        {
            spatialPositionsDirty = true;
        }

        private void ApplyRemoteSpatialPositionsIfNeeded()
        {
            if (!spatialPositionsDirty || playerManager == null || call == null)
            {
                return;
            }

            spatialPositionsDirty = false;
            PlayerData localPlayer = null;
            foreach (var player in playerManager.Players)
            {
                if (player.IsLocal)
                {
                    localPlayer = player;
                    break;
                }
            }

            if (localPlayer == null)
            {
                return;
            }

            if (directAttenuationCurve == null)
            {
                directAttenuationCurve = VoiceRangeCalculator.CreateDefaultCurve();
            }

            foreach (var remotePlayer in playerManager.Players)
            {
                if (remotePlayer.IsLocal)
                {
                    continue;
                }

                VoiceRangeCalculator.CalculateRelativePosition(
                    localPlayer.Position,
                    remotePlayer.Position,
                    out var horizontalDirection,
                    out var distanceMeters);
                var blockedByPrivateGroup = !PrivateVoiceGroupRules.CanHear(
                    playerManager.PrivateGroupsIsolated,
                    localPlayer.PrivateGroup,
                    remotePlayer.PrivateGroup);
                var wallOcclusion = tacticalMapManager?.CalculateOcclusion(
                    localPlayer.Position,
                    remotePlayer.Position) ?? 0f;
                var distanceGain = VoiceRangeCalculator.Evaluate(
                    distanceMeters,
                    VoiceModeProfile.GetMinimumDistance(remotePlayer.VoiceMode),
                    VoiceModeProfile.GetMaximumDistance(remotePlayer.VoiceMode),
                    directAttenuationCurve);
                var gain = blockedByPrivateGroup
                    ? 0f
                    : distanceGain *
                      VoiceModeProfile.GetOutputGain(remotePlayer.VoiceMode) *
                      VoiceAudioSource.CalculateWallGain(wallOcclusion);

                lock (remoteAudioLock)
                {
                    directPanByUser[remotePlayer.DiscordUserId] = horizontalDirection;
                }

                try
                {
                    call.SetParticipantVolume(
                        remotePlayer.DiscordUserId,
                        Mathf.Clamp(gain * 100f, 0f, 200f));
                }
                catch (ObjectDisposedException)
                {
                    spatialPositionsDirty = true;
                    return;
                }
            }
        }

        private void OnUserAudioReceived(
            ulong userId,
            IntPtr data,
            ulong samplesPerChannel,
            int sampleRate,
            ulong channels,
            ref bool outShouldMute)
        {
            Interlocked.Increment(ref receivedFrameCount);
            Interlocked.Add(ref receivedSampleCount, ToSafeSampleCount(samplesPerChannel, channels));
            outShouldMute = false;

            float horizontalPan;
            DirectPcmPanner panner;
            lock (remoteAudioLock)
            {
                directPanByUser.TryGetValue(userId, out horizontalPan);
                if (!directPanners.TryGetValue(userId, out panner))
                {
                    panner = new DirectPcmPanner();
                    directPanners.Add(userId, panner);
                }
            }

            // Discord conserva il proprio jitter buffer e riproduce direttamente il frame.
            // Quando il callback fornisce almeno due canali applichiamo anche il pan senza
            // creare una seconda coda audio in Unity.
            panner.Apply(data, samplesPerChannel, channels, horizontalPan);
        }

        private void OnUserAudioCaptured(
            IntPtr data,
            ulong samplesPerChannel,
            int sampleRate,
            ulong channels)
        {
            Interlocked.Increment(ref capturedFrameCount);
            Interlocked.Add(ref capturedSampleCount, ToSafeSampleCount(samplesPerChannel, channels));
        }

        private static long ToSafeSampleCount(ulong samplesPerChannel, ulong channels)
        {
            if (channels == 0 || samplesPerChannel > (ulong)long.MaxValue / channels)
            {
                return 0;
            }

            return (long)(samplesPerChannel * channels);
        }

        private void OnCallStatusChanged(Call.Status status, Call.Error error, int errorDetail)
        {
            if (error != Call.Error.None)
            {
                Fail($"Errore della chiamata Discord: {error} ({errorDetail}).");
                return;
            }

            switch (status)
            {
                case Call.Status.Joining:
                case Call.Status.Connecting:
                case Call.Status.SignalingConnected:
                    SetState(DiscordVoiceState.Starting);
                    break;
                case Call.Status.Connected:
                    SetState(DiscordVoiceState.Connected);
                    VoiceParticipantsChanged?.Invoke();
                    break;
                case Call.Status.Reconnecting:
                    SetState(DiscordVoiceState.Reconnecting);
                    break;
                case Call.Status.Disconnecting:
                    SetState(DiscordVoiceState.Stopping);
                    break;
                case Call.Status.Disconnected:
                    if (State != DiscordVoiceState.Stopping)
                    {
                        DisposeCall();
                        Fail("La chiamata vocale Discord si è disconnessa.");
                    }
                    break;
            }
        }

        private void OnCallParticipantChanged(ulong userId, bool added)
        {
            if (!added)
            {
                lock (speakingUsersLock)
                {
                    speakingUsers.Remove(userId);
                }

                lock (remoteAudioLock)
                {
                    directPanByUser.Remove(userId);
                    directPanners.Remove(userId);
                }
            }
            else
            {
                spatialPositionsDirty = true;
            }

            VoiceParticipantsChanged?.Invoke();
        }

        private void OnSpeakingStatusChanged(ulong userId, bool isPlayingSound)
        {
            lock (speakingUsersLock)
            {
                if (isPlayingSound)
                {
                    speakingUsers.Add(userId);
                }
                else
                {
                    speakingUsers.Remove(userId);
                }
            }

            VoiceParticipantsChanged?.Invoke();
        }

        private void OnVoiceStateChanged(ulong userId)
        {
            if (userId == authManager?.CurrentUser?.Id && call != null)
            {
                IsSelfMuted = call.GetSelfMute();
            }

            VoiceParticipantsChanged?.Invoke();
        }

        private void OnEndCallCompleted()
        {
            DisposeCall();
            SetState(sessionManager?.State == DiscordSessionState.Joined
                ? DiscordVoiceState.Ready
                : DiscordVoiceState.Unavailable);
        }

        private void OnSessionStateChanged(DiscordSessionState sessionState)
        {
            if (sessionState == DiscordSessionState.Joined)
            {
                if (call == null)
                {
                    ErrorMessage = string.Empty;
                    SetState(DiscordVoiceState.Ready);
                }

                return;
            }

            if (call != null && State != DiscordVoiceState.Stopping)
            {
                StopVoice();
            }
            else if (call == null)
            {
                SetState(DiscordVoiceState.Unavailable);
            }
        }

        private void ResetAudioCounters()
        {
            Interlocked.Exchange(ref capturedFrameCount, 0);
            Interlocked.Exchange(ref receivedFrameCount, 0);
            Interlocked.Exchange(ref capturedSampleCount, 0);
            Interlocked.Exchange(ref receivedSampleCount, 0);
        }

        private void ClearRemoteAudio()
        {
            lock (remoteAudioLock)
            {
                directPanByUser.Clear();
                directPanners.Clear();
            }
        }

        private void DisposeCall()
        {
            call?.Dispose();
            call = null;
            activeLobbyId = 0;
            IsSelfMuted = false;
            lock (speakingUsersLock)
            {
                speakingUsers.Clear();
            }

            ClearRemoteAudio();

            VoiceParticipantsChanged?.Invoke();
        }

        private void Fail(string message, Exception exception = null)
        {
            var lobbyToClose = activeLobbyId;
            DisposeCall();
            if (lobbyToClose != 0 && authManager?.Client != null)
            {
                try
                {
                    authManager.Client.EndCall(lobbyToClose, () => { });
                }
                catch (Exception endCallException)
                {
                    Debug.LogException(endCallException);
                }
            }

            ErrorMessage = message;
            if (exception == null)
            {
                Debug.LogError(message);
            }
            else
            {
                Debug.LogException(new InvalidOperationException(message, exception));
            }

            SetState(DiscordVoiceState.Failed);
        }

        private void SetState(DiscordVoiceState state)
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
            if (sessionManager != null)
            {
                sessionManager.StateChanged -= OnSessionStateChanged;
            }

            if (playerManager != null)
            {
                playerManager.PlayersChanged -= OnPlayersChanged;
            }

            if (tacticalMapManager != null)
            {
                tacticalMapManager.MapChanged -= OnMapChanged;
            }

            DisposeCall();
            sessionManager = null;
            playerManager = null;
            tacticalMapManager = null;
            authManager = null;
        }

        private sealed class DirectPcmPanner
        {
            private readonly object syncRoot = new object();
            private short[] scratch = Array.Empty<short>();

            public void Apply(IntPtr data, ulong samplesPerChannel, ulong channels, float pan)
            {
                if (data == IntPtr.Zero || samplesPerChannel == 0 || channels < 2 ||
                    samplesPerChannel > int.MaxValue || channels > int.MaxValue)
                {
                    return;
                }

                int frameCount;
                int channelCount;
                int totalSamples;
                try
                {
                    frameCount = (int)samplesPerChannel;
                    channelCount = (int)channels;
                    totalSamples = checked(frameCount * channelCount);
                }
                catch (OverflowException)
                {
                    return;
                }

                var clampedPan = Math.Max(-1f, Math.Min(1f, pan));
                var leftGain = clampedPan > 0f ? 1f - clampedPan : 1f;
                var rightGain = clampedPan < 0f ? 1f + clampedPan : 1f;

                lock (syncRoot)
                {
                    if (scratch.Length < totalSamples)
                    {
                        scratch = new short[totalSamples];
                    }

                    Marshal.Copy(data, scratch, 0, totalSamples);
                    for (var frame = 0; frame < frameCount; frame++)
                    {
                        var offset = frame * channelCount;
                        scratch[offset] = ScaleSample(scratch[offset], leftGain);
                        scratch[offset + 1] = ScaleSample(scratch[offset + 1], rightGain);
                    }

                    Marshal.Copy(scratch, 0, data, totalSamples);
                }
            }

            private static short ScaleSample(short sample, float gain)
            {
                var scaled = (int)Math.Round(sample * gain);
                return (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, scaled));
            }
        }
    }
}
