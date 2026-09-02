namespace DndProximityVoice.Voice
{
    public static class VoiceModeProfile
    {
        public static bool IsValid(VoiceMode mode)
        {
            return mode == VoiceMode.Whisper ||
                   mode == VoiceMode.Normal ||
                   mode == VoiceMode.Shout;
        }

        public static float GetMinimumDistance(VoiceMode mode)
        {
            switch (mode)
            {
                case VoiceMode.Whisper:
                    return 0.75f;
                case VoiceMode.Shout:
                    return 3f;
                default:
                    return VoiceRangeCalculator.DefaultMinimumDistance;
            }
        }

        public static float GetMaximumDistance(VoiceMode mode)
        {
            switch (mode)
            {
                case VoiceMode.Whisper:
                    return 3f;
                case VoiceMode.Shout:
                    return 24f;
                default:
                    return VoiceRangeCalculator.DefaultMaximumDistance;
            }
        }

        public static float GetOutputGain(VoiceMode mode)
        {
            return mode == VoiceMode.Whisper ? 0.72f : 1f;
        }

        public static string GetDisplayName(VoiceMode mode)
        {
            switch (mode)
            {
                case VoiceMode.Whisper:
                    return "SUSSURRO";
                case VoiceMode.Shout:
                    return "URLO";
                default:
                    return "NORMALE";
            }
        }
    }
}
