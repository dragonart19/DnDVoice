namespace DndProximityVoice.Discord
{
    public sealed class DiscordUserSnapshot
    {
        public DiscordUserSnapshot(ulong id, string username, string displayName, string avatarUrl)
        {
            Id = id;
            Username = username ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            AvatarUrl = avatarUrl ?? string.Empty;
        }

        public ulong Id { get; }

        public string Username { get; }

        public string DisplayName { get; }

        public string AvatarUrl { get; }
    }
}
