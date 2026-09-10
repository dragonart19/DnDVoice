using DndProximityVoice.Voice;
using UnityEngine;

namespace DndProximityVoice.UI
{
    public enum VoiceAudibilityLevel
    {
        Blocked,
        OutOfRange,
        Faint,
        Attenuated,
        Clear
    }

    /// <summary>
    /// Converts the acoustic model into stable, accessible UI data. It intentionally
    /// contains no drawing or Discord calls, so presentation stays separate from voice logic.
    /// </summary>
    public readonly struct VoiceAudibilityPresentation
    {
        public VoiceAudibilityPresentation(
            VoiceAudibilityLevel level,
            float normalizedStrength,
            string label,
            string detail)
        {
            Level = level;
            NormalizedStrength = Mathf.Clamp01(normalizedStrength);
            Label = label;
            Detail = detail;
        }

        public VoiceAudibilityLevel Level { get; }

        public float NormalizedStrength { get; }

        public string Label { get; }

        public string Detail { get; }
    }

    public static class VoiceUiPresentation
    {
        private static readonly AnimationCurve DefaultCurve = VoiceRangeCalculator.CreateDefaultCurve();

        public static VoiceAudibilityPresentation Calculate(
            float distanceMeters,
            VoiceMode mode,
            float wallOcclusion,
            bool privateGroupBlocked)
        {
            if (privateGroupBlocked)
            {
                return new VoiceAudibilityPresentation(
                    VoiceAudibilityLevel.Blocked,
                    0f,
                    "PRIVATA",
                    "Canale privato diverso");
            }

            var rangeGain = VoiceRangeCalculator.Evaluate(
                Mathf.Max(0f, distanceMeters),
                VoiceModeProfile.GetMinimumDistance(mode),
                VoiceModeProfile.GetMaximumDistance(mode),
                DefaultCurve);
            var strength = Mathf.Clamp01(
                rangeGain *
                VoiceModeProfile.GetOutputGain(mode) *
                VoiceAudioSource.CalculateWallGain(wallOcclusion));

            if (strength <= 0.001f)
            {
                return new VoiceAudibilityPresentation(
                    VoiceAudibilityLevel.OutOfRange,
                    0f,
                    "FUORI PORTATA",
                    "La voce non arriva fino a te");
            }

            if (strength < 0.18f)
            {
                return new VoiceAudibilityPresentation(
                    VoiceAudibilityLevel.Faint,
                    strength,
                    "DEBOLE",
                    "Voce molto distante o schermata");
            }

            if (strength < 0.56f)
            {
                return new VoiceAudibilityPresentation(
                    VoiceAudibilityLevel.Attenuated,
                    strength,
                    "ATTENUATA",
                    "Distanza o muri riducono il volume");
            }

            return new VoiceAudibilityPresentation(
                VoiceAudibilityLevel.Clear,
                strength,
                "NITIDA",
                "Voce chiaramente udibile");
        }

        public static int ComparePlayers(
            bool leftLocal,
            bool leftSpeaking,
            bool leftConnected,
            bool leftDm,
            string leftName,
            bool rightLocal,
            bool rightSpeaking,
            bool rightConnected,
            bool rightDm,
            string rightName)
        {
            var result = CompareTrueFirst(leftLocal, rightLocal);
            if (result != 0)
            {
                return result;
            }

            result = CompareTrueFirst(leftSpeaking, rightSpeaking);
            if (result != 0)
            {
                return result;
            }

            result = CompareTrueFirst(leftConnected, rightConnected);
            if (result != 0)
            {
                return result;
            }

            result = CompareTrueFirst(leftDm, rightDm);
            return result != 0
                ? result
                : string.Compare(leftName, rightName, System.StringComparison.CurrentCultureIgnoreCase);
        }

        private static int CompareTrueFirst(bool left, bool right)
        {
            return left == right ? 0 : left ? -1 : 1;
        }
    }
}
