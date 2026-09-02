namespace DndProximityVoice.Discord
{
    public enum DiscordAuthState
    {
        Initializing = 0,
        ReadyToLogin = 1,
        Authorizing = 2,
        ExchangingToken = 3,
        Connecting = 4,
        Connected = 5,
        Failed = 6
    }
}
