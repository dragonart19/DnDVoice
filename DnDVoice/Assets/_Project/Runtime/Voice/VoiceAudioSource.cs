using System.Threading;
using UnityEngine;

namespace DndProximityVoice.Voice
{
    [DisallowMultipleComponent]
    public sealed class VoiceAudioSource : MonoBehaviour
    {
        // Una coda di 40-50 ms era troppo corta per assorbire il normale jitter di rete e
        // produceva interruzioni. Questi valori mantengono la latenza ben sotto il secondo,
        // lasciando però abbastanza margine per una conversazione continua.
        private const float PrebufferSeconds = 0.12f;
        private const float MaximumBufferedSeconds = 0.45f;
        private const float TargetBufferedSeconds = 0.18f;
        private const float MinimumPlaybackPitch = 0.985f;
        private const float MaximumPlaybackPitch = 1.015f;
        private const float PlaybackPitchCorrection = 0.015f;
        private const float PlaybackPitchSmoothing = 0.08f;
        private const int StreamingClipSeconds = 2;
        private const float PositionSmoothingSpeed = 8f;
        private const float WallGainChangePerSecond = 6f;
        private const float PrivacyGainChangePerSecond = 12f;
        private const float VolumeAttackPerSecond = 18f;
        private const float VolumeReleasePerSecond = 8f;
        private const float ClearCutoffFrequency = 22000f;
        private const float HeavyWallCutoffFrequency = 900f;
        private const float HeavyWallGain = 0.12f;
        private const float FilterSmoothingSpeed = 7f;

        private RemotePcmStream stream;
        private AudioSource audioSource;
        private AudioClip streamingClip;
        private AudioLowPassFilter lowPassFilter;
        private int requiredPrebufferSamples;
        private int maximumBufferedSamples;
        private int targetBufferedSamples;
        private AudioListener listener;
        private AnimationCurve attenuationCurve;
        private float minimumDistance = VoiceRangeCalculator.DefaultMinimumDistance;
        private float maximumDistance = VoiceRangeCalculator.DefaultMaximumDistance;
        private float outputGain = 1f;
        private float currentWallGain = 1f;
        private float targetWallGain = 1f;
        private float currentPrivacyGain = 1f;
        private float targetPrivacyGain = 1f;
        private float targetCutoffFrequency = ClearCutoffFrequency;
        private int rebufferRequested;
        private Vector3 currentRelativePosition = new Vector3(0f, 0f, 1f);
        private Vector3 targetRelativePosition = new Vector3(0f, 0f, 1f);

        public ulong UserId => stream?.UserId ?? 0;

        public bool PlaybackStarted { get; private set; }

        public int BufferedSamples => stream?.BufferedSamples ?? 0;

        public int BufferedMilliseconds
        {
            get
            {
                var sampleRate = stream?.SampleRate ?? 0;
                return sampleRate <= 0
                    ? 0
                    : Mathf.RoundToInt(BufferedSamples * 1000f / sampleRate);
            }
        }

        public long DroppedSamples => stream?.DroppedSamples ?? 0;

        public long UnderflowSamples => stream?.UnderflowSamples ?? 0;

        public float VirtualDistanceMeters => currentRelativePosition.magnitude;

        public float CalculatedVolume { get; private set; } = 1f;

        public VoiceMode VoiceMode { get; private set; } = VoiceMode.Normal;

        public void Initialize(RemotePcmStream remoteStream, string displayName)
        {
            stream = remoteStream;
            var frequency = Mathf.Max(8000, stream.SampleRate);
            requiredPrebufferSamples = Mathf.Max(1, Mathf.RoundToInt(frequency * PrebufferSeconds));
            maximumBufferedSamples = Mathf.Max(
                requiredPrebufferSamples,
                Mathf.RoundToInt(frequency * MaximumBufferedSeconds));
            targetBufferedSamples = Mathf.Max(
                requiredPrebufferSamples,
                Mathf.RoundToInt(frequency * TargetBufferedSeconds));

            gameObject.name = $"Remote Voice · {displayName} · {stream.UserId}";
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 1f;
            audioSource.dopplerLevel = 0f;
            audioSource.spread = 0f;
            audioSource.volume = 0f;
            audioSource.priority = 16;
            audioSource.ignoreListenerPause = true;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 500f;
            audioSource.SetCustomCurve(
                AudioSourceCurveType.CustomRolloff,
                AnimationCurve.Linear(0f, 1f, 1f, 1f));
            lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
            lowPassFilter.cutoffFrequency = ClearCutoffFrequency;
            lowPassFilter.lowpassResonanceQ = 1f;

            streamingClip = AudioClip.Create(
                $"Discord PCM · {stream.UserId}",
                frequency * StreamingClipSeconds,
                1,
                frequency,
                true,
                OnPcmRead);
            audioSource.clip = streamingClip;
        }

        public void ConfigureSpatial(
            AudioListener audioListener,
            AnimationCurve curve,
            float minDistance,
            float maxDistance)
        {
            listener = audioListener;
            attenuationCurve = curve;
            minimumDistance = Mathf.Max(0f, minDistance);
            maximumDistance = Mathf.Max(minimumDistance + 0.01f, maxDistance);
        }

        public void SetVirtualPosition(float horizontalDirection, float distanceMeters)
        {
            var clampedHorizontal = Mathf.Clamp(horizontalDirection, -1f, 1f);
            var clampedDistance = Mathf.Max(0f, distanceMeters);
            var angleRadians = clampedHorizontal * Mathf.PI * 0.5f;
            targetRelativePosition = new Vector3(
                Mathf.Sin(angleRadians) * clampedDistance,
                0f,
                Mathf.Cos(angleRadians) * clampedDistance);
        }

        public void ConfigureVoiceMode(VoiceMode mode)
        {
            VoiceMode = VoiceModeProfile.IsValid(mode) ? mode : VoiceMode.Normal;
            minimumDistance = VoiceModeProfile.GetMinimumDistance(VoiceMode);
            maximumDistance = VoiceModeProfile.GetMaximumDistance(VoiceMode);
            outputGain = VoiceModeProfile.GetOutputGain(VoiceMode);
        }

        public void SetWallOcclusion(float strength)
        {
            var clampedStrength = Mathf.Clamp01(strength);
            targetWallGain = CalculateWallGain(clampedStrength);
            targetCutoffFrequency = Mathf.Lerp(
                ClearCutoffFrequency,
                HeavyWallCutoffFrequency,
                Mathf.Sqrt(clampedStrength));
        }

        public void SetPrivateGroupBlocked(bool blocked)
        {
            targetPrivacyGain = blocked ? 0f : 1f;
            if (blocked)
            {
                currentPrivacyGain = 0f;
                if (audioSource != null)
                {
                    audioSource.volume = 0f;
                }
            }
        }

        public static float CalculateWallGain(float strength)
        {
            return Mathf.Lerp(1f, HeavyWallGain, Mathf.Clamp01(strength));
        }

        private void Update()
        {
            UpdateSpatialPositionAndVolume();

            if (PlaybackStarted &&
                (Interlocked.Exchange(ref rebufferRequested, 0) == 1 || !audioSource.isPlaying))
            {
                audioSource.Stop();
                PlaybackStarted = false;
                stream.PlaybackReady = false;
            }

            UpdateAdaptivePlaybackRate();

            if (PlaybackStarted || stream == null || audioSource == null ||
                stream.BufferedSamples < requiredPrebufferSamples)
            {
                return;
            }

            audioSource.pitch = 1f;
            audioSource.Play();
            PlaybackStarted = true;
            stream.PlaybackReady = true;
        }

        private void UpdateAdaptivePlaybackRate()
        {
            if (audioSource == null || stream == null || !PlaybackStarted || targetBufferedSamples <= 0)
            {
                return;
            }

            var bufferError = Mathf.Clamp(
                (stream.BufferedSamples - targetBufferedSamples) / (float)targetBufferedSamples,
                -1f,
                1f);
            var targetPitch = Mathf.Clamp(
                1f + bufferError * PlaybackPitchCorrection,
                MinimumPlaybackPitch,
                MaximumPlaybackPitch);
            audioSource.pitch = Mathf.MoveTowards(
                audioSource.pitch,
                targetPitch,
                PlaybackPitchSmoothing * Time.unscaledDeltaTime);
        }

        private void UpdateSpatialPositionAndVolume()
        {
            if (audioSource == null)
            {
                return;
            }

            if (listener == null)
            {
                listener = FindFirstObjectByType<AudioListener>();
            }

            var interpolation = 1f - Mathf.Exp(-PositionSmoothingSpeed * Time.unscaledDeltaTime);
            currentRelativePosition = Vector3.Lerp(
                currentRelativePosition,
                targetRelativePosition,
                interpolation);

            transform.position = listener != null
                ? listener.transform.TransformPoint(currentRelativePosition)
                : currentRelativePosition;

            currentWallGain = Mathf.MoveTowards(
                currentWallGain,
                targetWallGain,
                WallGainChangePerSecond * Time.unscaledDeltaTime);
            currentPrivacyGain = Mathf.MoveTowards(
                currentPrivacyGain,
                targetPrivacyGain,
                PrivacyGainChangePerSecond * Time.unscaledDeltaTime);
            if (lowPassFilter != null)
            {
                var filterInterpolation = 1f - Mathf.Exp(-FilterSmoothingSpeed * Time.unscaledDeltaTime);
                lowPassFilter.cutoffFrequency = Mathf.Lerp(
                    lowPassFilter.cutoffFrequency,
                    targetCutoffFrequency,
                    filterInterpolation);
            }

            CalculatedVolume = Mathf.Clamp01(
                VoiceRangeCalculator.Evaluate(
                    currentRelativePosition.magnitude,
                    minimumDistance,
                    maximumDistance,
                    attenuationCurve) * outputGain * currentWallGain * currentPrivacyGain);
            var volumeSpeed = CalculatedVolume >= audioSource.volume
                ? VolumeAttackPerSecond
                : VolumeReleasePerSecond;
            audioSource.volume = Mathf.MoveTowards(
                audioSource.volume,
                CalculatedVolume,
                volumeSpeed * Time.unscaledDeltaTime);
        }

        private void OnPcmRead(float[] data)
        {
            if (stream == null)
            {
                System.Array.Clear(data, 0, data.Length);
                return;
            }

            stream.TrimLatencyIfNeeded(maximumBufferedSamples, targetBufferedSamples);
            var samplesRead = stream.Read(data, 0, data.Length);
            if (samplesRead < data.Length && stream.PlaybackReady)
            {
                stream.PlaybackReady = false;
                Interlocked.Exchange(ref rebufferRequested, 1);
            }
        }

        private void OnDestroy()
        {
            if (stream != null)
            {
                stream.PlaybackReady = false;
            }

            Interlocked.Exchange(ref rebufferRequested, 0);

            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.clip = null;
            }

            if (streamingClip != null)
            {
                Destroy(streamingClip);
            }

            stream = null;
            audioSource = null;
            streamingClip = null;
            lowPassFilter = null;
        }
    }
}
