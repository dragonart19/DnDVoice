namespace DndProximityVoice.Discord
{
    public static class DiscordSdkAvailability
    {
        public const string IntegratedVersion = "1.10.18687";

        public static bool IsIntegrated
        {
            get
            {
#if DND_DISCORD_SDK
                return true;
#else
                return false;
#endif
            }
        }
    }
}
