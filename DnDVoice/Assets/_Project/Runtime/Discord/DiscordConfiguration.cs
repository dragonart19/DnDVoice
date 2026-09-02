using Discord.Sdk;

namespace DndProximityVoice.Discord
{
    public static class DiscordConfiguration
    {
        public const ulong ApplicationId = 1541099026571722772UL;
        public const string RedirectUri = "http://127.0.0.1/callback";

        public static string RequiredScopes => Client.GetDefaultCommunicationScopes();
    }
}
