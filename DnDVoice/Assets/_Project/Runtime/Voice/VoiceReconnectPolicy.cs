using DndProximityVoice.Session;

namespace DndProximityVoice.Voice
{
    public static class VoiceReconnectPolicy
    {
        public const int MaximumAttempts = 3;
        private const float BaseDelaySeconds = 2f;

        public static bool CanSchedule(
            bool voiceRequested,
            DiscordSessionState sessionState,
            int completedAttempts)
        {
            return voiceRequested &&
                   sessionState == DiscordSessionState.Joined &&
                   completedAttempts < MaximumAttempts;
        }

        public static float GetDelaySeconds(int attemptNumber)
        {
            var clampedAttempt = attemptNumber < 1
                ? 1
                : attemptNumber > MaximumAttempts
                    ? MaximumAttempts
                    : attemptNumber;
            return BaseDelaySeconds * clampedAttempt;
        }
    }
}
