namespace DndProximityVoice.Session
{
    public enum DiscordSessionState
    {
        WaitingForDiscord = 0,
        Ready = 1,
        Joining = 2,
        Joined = 3,
        Leaving = 4,
        Failed = 5
    }
}
