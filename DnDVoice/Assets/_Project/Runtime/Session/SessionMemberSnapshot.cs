namespace DndProximityVoice.Session
{
    public sealed class SessionMemberSnapshot
    {
        public SessionMemberSnapshot(ulong id, string displayName, bool connected, bool isHost, bool isLocal)
        {
            Id = id;
            DisplayName = displayName ?? string.Empty;
            Connected = connected;
            IsHost = isHost;
            IsLocal = isLocal;
        }

        public ulong Id { get; }

        public string DisplayName { get; }

        public bool Connected { get; }

        public bool IsHost { get; }

        public bool IsLocal { get; }
    }
}
