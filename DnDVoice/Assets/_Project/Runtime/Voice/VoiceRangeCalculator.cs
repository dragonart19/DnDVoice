using UnityEngine;

namespace DndProximityVoice.Voice
{
    public static class VoiceRangeCalculator
    {
        public const float DefaultMinimumDistance = 2f;
        public const float DefaultMaximumDistance = 12f;

        public static float Evaluate(
            float distanceMeters,
            float minimumDistance,
            float maximumDistance,
            AnimationCurve attenuationCurve)
        {
            if (distanceMeters <= minimumDistance)
            {
                return 1f;
            }

            if (distanceMeters >= maximumDistance || maximumDistance <= minimumDistance)
            {
                return 0f;
            }

            var normalizedDistance = Mathf.InverseLerp(
                minimumDistance,
                maximumDistance,
                distanceMeters);
            var value = attenuationCurve == null
                ? 1f - normalizedDistance
                : attenuationCurve.Evaluate(normalizedDistance);
            return Mathf.Clamp01(value);
        }

        public static void CalculateRelativePosition(
            Vector2 listenerPosition,
            Vector2 speakerPosition,
            out float horizontalDirection,
            out float distanceMeters)
        {
            var offset = speakerPosition - listenerPosition;
            distanceMeters = offset.magnitude;
            horizontalDirection = distanceMeters > 0.0001f
                ? Mathf.Clamp(offset.x / distanceMeters, -1f, 1f)
                : 0f;
        }

        public static AnimationCurve CreateDefaultCurve()
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.2f, 0.8f),
                new Keyframe(0.4f, 0.55f),
                new Keyframe(0.6f, 0.3f),
                new Keyframe(0.8f, 0.1f),
                new Keyframe(1f, 0f));
            curve.preWrapMode = WrapMode.ClampForever;
            curve.postWrapMode = WrapMode.ClampForever;
            return curve;
        }
    }
}
