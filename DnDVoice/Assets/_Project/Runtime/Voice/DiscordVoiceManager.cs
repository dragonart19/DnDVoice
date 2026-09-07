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
    public readonly struct DiscordAudioDeviceInfo
    {
        public DiscordAudioDeviceInfo(string id, string name, bool isDefault)
        {
            Id = id ?? string.Empty;
            Name = string.IsNullOrWhiteSpace(name) ? "Dispositivo senza nome" : name;
            IsDefault = isDefault;
        }

        public string Id { get; }

        public string Name { get; }

        public bool IsDefault { get; }
    }

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
        private bool voiceRequested;
        private int automaticRestartAttempts;
        private float restartVoiceAt = -1f;
        private readonly List<DiscordAudioDeviceInfo> inputDevices = new List<DiscordAudioDeviceInfo>();
        private readonly List<DiscordAudioDeviceInfo> outputDevices = new List<DiscordAudioDeviceInfo>();
        private bool muteBeforePushToTalk;

        public event Action<DiscordVoiceState> StateChanged;

        public event Action VoiceParticipantsChanged;

        public event Action AudioSettingsChanged;

        public DiscordVoiceState State { get; private set; } = DiscordVoiceState.Unavailable;

        public string ErrorMessage { get; private set; } = string.Empty;

        public bool IsSelfMuted { get; private set; }

        public bool IsSelfDeafened { get; private set; }

        public bool PushToTalkEnabled { get; private set; }

        public bool IsPushToTalkPressed { get; private set; }

        public bool AutomaticVoiceSensitivity { get; private set; } = true;

        public float VoiceSensitivityDb { get; private set; } = -60f;

        public float InputVolume { get; private set; } = 100f;

        public float OutputVolume { get; private set; } = 100f;

        public string CurrentInputDeviceId { get; private set; } = string.Empty;

        public string CurrentInputDeviceName { get; private set; } = "Predefinito di sistema";

        public string CurrentOutputDeviceId { get; private set; } = string.Empty;

        public string CurrentOutputDeviceName { get; private set; } = "Predefinito di sistema";

        public string AudioSettingsMessage { get; private set; } = string.Empty;

        public IReadOnlyList<DiscordAudioDeviceInfo> InputDevices => inputDevices;

        public IReadOnlyList<DiscordAudioDeviceInfo> OutputDevices => outputDevices;

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

            voiceRequested = true;
            automaticRestartAttempts = 0;
            restartVoiceAt = -1f;
            BeginVoiceCall(false);
        }

        private void BeginVoiceCall(bool automaticRestart)
        {
            if (!voiceRequested || call != null || authManager?.Client == null ||
                authManager.State != DiscordAuthState.Connected ||
                sessionManager?.State != DiscordSessionState.Joined || sessionManager.LobbyId == 0)
            {
                return;
            }

            ErrorMessage = string.Empty;
            if (!automaticRestart)
            {
                ResetAudioCounters();
            }

            activeLobbyId = sessionManager.LobbyId;
            spatialPositionsDirty = true;
            SetState(automaticRestart
                ? DiscordVoiceState.Reconnecting
                : DiscordVoiceState.Starting);

            try
            {
                call = authManager.Client.StartCallWithAudioCallbacks(
                    activeLobbyId,
                    OnUserAudioReceived,
                    OnUserAudioCaptured);

                if (call == null)
                {
                    ScheduleVoiceRestart(
                        "Discord non ha ancora liberato la chiamata precedente.");
                    return;
                }

                call.SetStatusChangedCallback(OnCallStatusChanged);
                call.SetParticipantChangedCallback(OnCallParticipantChanged);
                call.SetSpeakingStatusChangedCallback(OnSpeakingStatusChanged);
                call.SetOnVoiceStateChangedCallback(OnVoiceStateChanged);
                IsSelfMuted = call.GetSelfMute();
                IsSelfDeafened = call.GetSelfDeaf();
                using (var vadSettings = call.GetVADThreshold())
                {
                    AutomaticVoiceSensitivity = vadSettings.Automatic();
                    VoiceSensitivityDb = Mathf.Clamp(vadSettings.VadThreshold(), -100f, 0f);
                }

                if (PushToTalkEnabled)
                {
                    SetSelfMuteInternal(true);
                }

                RefreshAudioSettings();
                OnCallStatusChanged(call.GetStatus(), Call.Error.None, 0);
            }
            catch (Exception exception)
            {
                ScheduleVoiceRestart(
                    "Non è stato possibile avviare la voce Discord.",
                    exception);
            }
        }

        public void StopVoice()
        {
            voiceRequested = false;
            automaticRestartAttempts = 0;
            restartVoiceAt = -1f;
            if (call == null || authManager?.Client == null || activeLobbyId == 0 ||
                State == DiscordVoiceState.Stopping)
            {
                if (call == null && sessionManager?.State == DiscordSessionState.Joined)
                {
                    ErrorMessage = string.Empty;
                    SetState(DiscordVoiceState.Ready);
                }

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
                if (PushToTalkEnabled)
                {
                    muteBeforePushToTalk = !muteBeforePushToTalk;
                    SetSelfMuteInternal(muteBeforePushToTalk || !IsPushToTalkPressed);
                }
                else
                {
                    SetSelfMuteInternal(!IsSelfMuted);
                }
            }
            catch (Exception exception)
            {
                Fail("Non è stato possibile cambiare lo stato del microfono.", exception);
            }
        }

        public void ToggleSelfDeafen()
        {
            if (call == null || State != DiscordVoiceState.Connected)
            {
                return;
            }

            try
            {
                IsSelfDeafened = !IsSelfDeafened;
                call.SetSelfDeaf(IsSelfDeafened);
                AudioSettingsMessage = IsSelfDeafened ? "Audio in uscita disattivato." : "Audio in uscita riattivato.";
                AudioSettingsChanged?.Invoke();
            }
            catch (Exception exception)
            {
                ReportAudioSettingsError("Non è stato possibile cambiare lo stato delle cuffie.", exception);
            }
        }

        public void SetPushToTalkEnabled(bool enabled)
        {
            if (PushToTalkEnabled == enabled)
            {
                return;
            }

            PushToTalkEnabled = enabled;
            IsPushToTalkPressed = false;
            if (enabled)
            {
                muteBeforePushToTalk = IsSelfMuted;
                SetSelfMuteInternal(true);
                AudioSettingsMessage = "Push-to-talk attivo: tieni premuto V per parlare.";
            }
            else
            {
                SetSelfMuteInternal(muteBeforePushToTalk);
                AudioSettingsMessage = "Modalità voce attiva ripristinata.";
            }

            AudioSettingsChanged?.Invoke();
        }

        public void SetPushToTalkPressed(bool pressed)
        {
            if (!PushToTalkEnabled || IsPushToTalkPressed == pressed)
            {
                return;
            }

            IsPushToTalkPressed = pressed;
            SetSelfMuteInternal(muteBeforePushToTalk || !pressed);
            AudioSettingsChanged?.Invoke();
        }

        public void SetInputVolume(float volume)
        {
            var client = authManager?.Client;
            if (client == null)
            {
                return;
            }

            try
            {
                InputVolume = Mathf.Clamp(volume, 0f, 100f);
                client.SetInputVolume(InputVolume);
                AudioSettingsChanged?.Invoke();
            }
            catch (Exception exception)
            {
                ReportAudioSettingsError("Non è stato possibile cambiare il volume del microfono.", exception);
            }
        }

        public void SetOutputVolume(float volume)
        {
            var client = authManager?.Client;
            if (client == null)
            {
                return;
            }

            try
            {
                OutputVolume = Mathf.Clamp(volume, 0f, 200f);
                client.SetOutputVolume(OutputVolume);
                AudioSettingsChanged?.Invoke();
            }
            catch (Exception exception)
            {
                ReportAudioSettingsError("Non è stato possibile cambiare il volume delle cuffie.", exception);
            }
        }

        public void SetAutomaticVoiceSensitivity(bool automatic)
        {
            AutomaticVoiceSensitivity = automatic;
            ApplyVoiceSensitivity();
        }

        public void SetVoiceSensitivity(float thresholdDb)
        {
            VoiceSensitivityDb = Mathf.Clamp(thresholdDb, -100f, 0f);
            ApplyVoiceSensitivity();
        }

        public void RefreshAudioSettings()
        {
            var client = authManager?.Client;
            if (client == null)
            {
                return;
            }

            try
            {
                InputVolume = Mathf.Clamp(client.GetInputVolume(), 0f, 100f);
                OutputVolume = Mathf.Clamp(client.GetOutputVolume(), 0f, 200f);
                client.GetInputDevices(OnInputDevicesReceived);
                client.GetOutputDevices(OnOutputDevicesReceived);
                client.GetCurrentInputDevice(OnCurrentInputDeviceReceived);
                client.GetCurrentOutputDevice(OnCurrentOutputDeviceReceived);
                AudioSettingsMessage = "Dispositivi audio aggiornati.";
                AudioSettingsChanged?.Invoke();
            }
            catch (Exception exception)
            {
                ReportAudioSettingsError("Non è stato possibile leggere i dispositivi audio.", exception);
            }
        }

        public void CycleInputDevice(int direction)
        {
            CycleDevice(inputDevices, CurrentInputDeviceId, direction, true);
        }

        public void CycleOutputDevice(int direction)
        {
            CycleDevice(outputDevices, CurrentOutputDeviceId, direction, false);
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
            RestartVoiceIfDue();
        }

        private void SetSelfMuteInternal(bool muted)
        {
            if (call == null)
            {
                IsSelfMuted = muted;
                return;
            }

            try
            {
                IsSelfMuted = muted;
                call.SetSelfMute(IsSelfMuted);
                VoiceParticipantsChanged?.Invoke();
            }
            catch (Exception exception)
            {
                ReportAudioSettingsError("Non è stato possibile cambiare lo stato del microfono.", exception);
            }
        }

        private void ApplyVoiceSensitivity()
        {
            if (call == null || State != DiscordVoiceState.Connected)
            {
                AudioSettingsChanged?.Invoke();
                return;
            }

            try
            {
                call.SetVADThreshold(AutomaticVoiceSensitivity, VoiceSensitivityDb);
                AudioSettingsMessage = AutomaticVoiceSensitivity
                    ? "Sensibilità automatica attiva."
                    : $"Soglia microfono impostata a {VoiceSensitivityDb:0} dB.";
                AudioSettingsChanged?.Invoke();
            }
            catch (Exception exception)
            {
                ReportAudioSettingsError("Non è stato possibile cambiare la sensibilità del microfono.", exception);
            }
        }

        private void OnInputDevicesReceived(AudioDevice[] devices)
        {
            ReplaceDeviceList(inputDevices, devices);
            AudioSettingsChanged?.Invoke();
        }

        private void OnOutputDevicesReceived(AudioDevice[] devices)
        {
            ReplaceDeviceList(outputDevices, devices);
            AudioSettingsChanged?.Invoke();
        }

        private void OnCurrentInputDeviceReceived(AudioDevice device)
        {
            ReadCurrentDevice(device, out var id, out var name);
            CurrentInputDeviceId = id;
            CurrentInputDeviceName = name;
            AudioSettingsChanged?.Invoke();
        }

        private void OnCurrentOutputDeviceReceived(AudioDevice device)
        {
            ReadCurrentDevice(device, out var id, out var name);
            CurrentOutputDeviceId = id;
            CurrentOutputDeviceName = name;
            AudioSettingsChanged?.Invoke();
        }

        private void CycleDevice(
            List<DiscordAudioDeviceInfo> devices,
            string currentId,
            int direction,
            bool input)
        {
            var client = authManager?.Client;
            if (client == null || devices.Count == 0 || direction == 0)
            {
                return;
            }

            var currentIndex = 0;
            for (var index = 0; index < devices.Count; index++)
            {
                if (devices[index].Id == currentId)
                {
                    currentIndex = index;
                    break;
                }
            }

            var nextIndex = (currentIndex + (direction > 0 ? 1 : -1) + devices.Count) % devices.Count;
            var next = devices[nextIndex];
            try
            {
                if (input)
                {
                    client.SetInputDevice(next.Id, result => OnDeviceChanged(result, next, true));
                }
                else
                {
                    client.SetOutputDevice(next.Id, result => OnDeviceChanged(result, next, false));
                }
            }
            catch (Exception exception)
            {
                ReportAudioSettingsError("Non è stato possibile cambiare dispositivo audio.", exception);
            }
        }

        private void OnDeviceChanged(ClientResult result, DiscordAudioDeviceInfo device, bool input)
        {
            if (!result.Successful())
            {
                ReportAudioSettingsError("Discord ha rifiutato il cambio del dispositivo audio.");
                return;
            }

            if (input)
            {
                CurrentInputDeviceId = device.Id;
                CurrentInputDeviceName = device.Name;
            }
            else
            {
                CurrentOutputDeviceId = device.Id;
                CurrentOutputDeviceName = device.Name;
            }

            AudioSettingsMessage = $"Dispositivo {(input ? "microfono" : "cuffie")} aggiornato.";
            AudioSettingsChanged?.Invoke();
        }

        private static void ReplaceDeviceList(List<DiscordAudioDeviceInfo> target, AudioDevice[] devices)
        {
            target.Clear();
            if (devices == null)
            {
                return;
            }

            foreach (var device in devices)
            {
                if (device == null)
                {
                    continue;
                }

                try
                {
                    target.Add(new DiscordAudioDeviceInfo(device.Id(), device.Name(), device.IsDefault()));
                }
                finally
                {
                    device.Dispose();
                }
            }
        }

        private static void ReadCurrentDevice(AudioDevice device, out string id, out string name)
        {
            id = string.Empty;
            name = "Predefinito di sistema";
            if (device == null)
            {
                return;
            }

            try
            {
                id = device.Id();
                var detectedName = device.Name();
                name = string.IsNullOrWhiteSpace(detectedName) ? name : detectedName;
            }
            finally
            {
                device.Dispose();
            }
        }

        private void ReportAudioSettingsError(string message, Exception exception = null)
        {
            AudioSettingsMessage = message;
            if (exception == null)
            {
                Debug.LogWarning(message);
            }
            else
            {
                Debug.LogWarning($"{message} {exception.Message}");
            }

            AudioSettingsChanged?.Invoke();
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
                var message = $"Errore della chiamata Discord: {error} ({errorDetail}).";
                if (error == Call.Error.Forbidden)
                {
                    Fail(message);
                }
                else
                {
                    ScheduleVoiceRestart(message);
                }

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
                    automaticRestartAttempts = 0;
                    restartVoiceAt = -1f;
                    ErrorMessage = string.Empty;
                    SetState(DiscordVoiceState.Connected);
                    Debug.Log(
                        $"Voce Discord collegata a {Time.realtimeSinceStartup:0.0}s dall'avvio.");
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
                        ScheduleVoiceRestart("La chiamata vocale Discord si è disconnessa.");
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
                IsSelfDeafened = call.GetSelfDeaf();
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

            voiceRequested = false;
            automaticRestartAttempts = 0;
            restartVoiceAt = -1f;
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

        private void RestartVoiceIfDue()
        {
            if (restartVoiceAt < 0f || Time.realtimeSinceStartup < restartVoiceAt)
            {
                return;
            }

            if (!voiceRequested || sessionManager?.State != DiscordSessionState.Joined)
            {
                restartVoiceAt = -1f;
                return;
            }

            if (authManager?.State != DiscordAuthState.Connected)
            {
                return;
            }

            restartVoiceAt = -1f;
            BeginVoiceCall(true);
        }

        private void ScheduleVoiceRestart(string message, Exception exception = null)
        {
            CloseCallBeforeRestart();
            if (!VoiceReconnectPolicy.CanSchedule(
                    voiceRequested,
                    sessionManager?.State ?? DiscordSessionState.WaitingForDiscord,
                    automaticRestartAttempts))
            {
                Fail(
                    automaticRestartAttempts >= VoiceReconnectPolicy.MaximumAttempts
                        ? $"{message} Riconnessione automatica non riuscita dopo " +
                          $"{VoiceReconnectPolicy.MaximumAttempts} tentativi."
                        : message,
                    exception);
                return;
            }

            automaticRestartAttempts++;
            var delay = VoiceReconnectPolicy.GetDelaySeconds(automaticRestartAttempts);
            restartVoiceAt = Time.realtimeSinceStartup + delay;
            ErrorMessage =
                $"Voce in riconnessione: tentativo {automaticRestartAttempts}/" +
                $"{VoiceReconnectPolicy.MaximumAttempts}.";
            if (exception == null)
            {
                Debug.LogWarning(
                    $"{message} {ErrorMessage} Tra {delay:0}s; tempo " +
                    $"{Time.realtimeSinceStartup:0.0}s.");
            }
            else
            {
                Debug.LogWarning(
                    $"{message} {ErrorMessage} Tra {delay:0}s; tempo " +
                    $"{Time.realtimeSinceStartup:0.0}s.\n{exception}");
            }

            SetState(DiscordVoiceState.Reconnecting);
        }

        private void CloseCallBeforeRestart()
        {
            var lobbyToClose = activeLobbyId;
            DisposeCall();
            if (lobbyToClose == 0 || authManager?.Client == null)
            {
                return;
            }

            try
            {
                authManager.Client.EndCall(lobbyToClose, () => { });
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Pulizia della chiamata Discord non riuscita: {exception.Message}");
            }
        }

        private void DisposeCall()
        {
            call?.Dispose();
            call = null;
            activeLobbyId = 0;
            IsSelfMuted = false;
            IsSelfDeafened = false;
            IsPushToTalkPressed = false;
            lock (speakingUsersLock)
            {
                speakingUsers.Clear();
            }

            ClearRemoteAudio();

            VoiceParticipantsChanged?.Invoke();
        }

        private void Fail(string message, Exception exception = null)
        {
            voiceRequested = false;
            automaticRestartAttempts = 0;
            restartVoiceAt = -1f;
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

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && PushToTalkEnabled)
            {
                SetPushToTalkPressed(false);
            }
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
