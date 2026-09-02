namespace DndProximityVoice.Core
{
    public enum AppStatus
    {
        Booting = 0,
        WaitingForDiscordSdk = 1,
        WaitingForDiscordLogin = 2,
        AuthenticatingDiscord = 3,
        Ready = 4,
        FatalError = 5
    }
}
