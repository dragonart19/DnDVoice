using DndProximityVoice.Discord;

namespace DndProximityVoice.Session
{
    public static class SessionRecoveryPolicy
    {
        public static bool ShouldPreserveJoinedSession(
            DiscordAuthState authState,
            DiscordSessionState sessionState,
            ulong lobbyId)
        {
            return authState == DiscordAuthState.Connecting &&
                   sessionState == DiscordSessionState.Joined &&
                   lobbyId != 0;
        }
    }
}
