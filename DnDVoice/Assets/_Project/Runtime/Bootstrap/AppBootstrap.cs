using DndProximityVoice.Core;
using DndProximityVoice.Discord;
using DndProximityVoice.Map;
using DndProximityVoice.Players;
using DndProximityVoice.Realtime;
using DndProximityVoice.Session;
using DndProximityVoice.UI;
using DndProximityVoice.Voice;
using UnityEngine;

namespace DndProximityVoice.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class AppBootstrap : MonoBehaviour
    {
        private const int LowLatencyDspBufferSamples = 256;
        private const int TargetFrameRate = 60;
        private static AppBootstrap instance;
        private DiscordAuthManager discordAuthManager;
        private ProductModeManager productModeManager;
        private DiscordSessionManager discordSessionManager;
        private DiscordVoiceManager discordVoiceManager;
        private PlayerManager playerManager;
        private PositionSyncManager positionSyncManager;
        private TacticalMapManager tacticalMapManager;

        public AppStatus Status { get; private set; } = AppStatus.Booting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateApplicationRoot()
        {
            if (instance != null)
            {
                return;
            }

            var root = new GameObject(BuildInfo.ProductName);
            DontDestroyOnLoad(root);
            instance = root.AddComponent<AppBootstrap>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
            ConfigureLowLatencyAudio();

            if (!DiscordSdkAvailability.IsIntegrated)
            {
                Status = AppStatus.WaitingForDiscordSdk;
                Debug.LogWarning(
                    "D&D Proximity Voice started without the Discord Social SDK. " +
                    "Import the official package and enable DND_DISCORD_SDK to continue Gate 0.");
                return;
            }

            discordAuthManager = gameObject.AddComponent<DiscordAuthManager>();
            gameObject.AddComponent<DiscordLoginOverlay>();
            productModeManager = gameObject.AddComponent<ProductModeManager>();
            gameObject.AddComponent<ProductModeOverlay>();
            positionSyncManager = gameObject.AddComponent<PositionSyncManager>();
            discordSessionManager = gameObject.AddComponent<DiscordSessionManager>();
            discordSessionManager.Initialize(discordAuthManager, positionSyncManager, productModeManager);
            tacticalMapManager = gameObject.AddComponent<TacticalMapManager>();
            tacticalMapManager.Initialize(discordSessionManager);
            playerManager = gameObject.AddComponent<PlayerManager>();
            playerManager.Initialize(discordSessionManager, tacticalMapManager);
            positionSyncManager.Initialize(discordSessionManager, playerManager, tacticalMapManager);
            discordVoiceManager = gameObject.AddComponent<DiscordVoiceManager>();
            discordVoiceManager.Initialize(
                discordAuthManager,
                discordSessionManager,
                playerManager,
                tacticalMapManager);
            gameObject.AddComponent<DiscordSessionOverlay>();
            gameObject.AddComponent<ProximityMapOverlay>();
            discordAuthManager.StateChanged += OnDiscordStateChanged;
            OnDiscordStateChanged(discordAuthManager.State);
            Debug.Log(
                $"{BuildInfo.ProductName} {BuildInfo.ReleaseLabel} bootstrap ready " +
                $"with Discord Social SDK {DiscordSdkAvailability.IntegratedVersion}.");
        }

        private static void ConfigureLowLatencyAudio()
        {
            var configuration = AudioSettings.GetConfiguration();
            if (configuration.dspBufferSize > LowLatencyDspBufferSamples || configuration.dspBufferSize <= 0)
            {
                configuration.dspBufferSize = LowLatencyDspBufferSamples;
                if (!AudioSettings.Reset(configuration))
                {
                    Debug.LogWarning("Non è stato possibile applicare il buffer audio a bassa latenza.");
                }
            }

            AudioSettings.GetDSPBufferSize(out var bufferLength, out var bufferCount);
            Debug.Log(
                $"Audio low-latency: {AudioSettings.outputSampleRate} Hz, " +
                $"DSP {bufferLength} samples × {bufferCount} buffers.");
        }

        private void OnDiscordStateChanged(DiscordAuthState state)
        {
            switch (state)
            {
                case DiscordAuthState.ReadyToLogin:
                    Status = AppStatus.WaitingForDiscordLogin;
                    break;
                case DiscordAuthState.Authorizing:
                case DiscordAuthState.ExchangingToken:
                case DiscordAuthState.Connecting:
                    Status = AppStatus.AuthenticatingDiscord;
                    break;
                case DiscordAuthState.Connected:
                    Status = AppStatus.Ready;
                    break;
                case DiscordAuthState.Failed:
                    Status = AppStatus.FatalError;
                    break;
            }
        }

        private void OnDestroy()
        {
            if (discordAuthManager != null)
            {
                discordAuthManager.StateChanged -= OnDiscordStateChanged;
            }

            discordSessionManager = null;
            discordVoiceManager = null;
            productModeManager = null;
            playerManager = null;
            positionSyncManager = null;
            tacticalMapManager = null;
        }
    }
}
