using System;
using System.Collections.Generic;

namespace DndProximityVoice.Realtime
{
    /// <summary>Binds a Relay connection to the author authenticated by Discord, never a claimed user ID.</summary>
    public sealed class RelayPeerAuthorization
    {
        private readonly Dictionary<ulong, string> challenges = new Dictionary<ulong, string>();
        private readonly Dictionary<ulong, ulong> users = new Dictionary<ulong, ulong>();

        public string Challenge(ulong clientId)
        {
            Remove(clientId);
            return challenges[clientId] = Guid.NewGuid().ToString("N");
        }

        public bool TryConfirm(ulong authorId, string proof, out ulong clientId)
        {
            clientId = 0;
            if (authorId == 0 || string.IsNullOrEmpty(proof) || users.ContainsValue(authorId)) return false;
            foreach (var pair in challenges)
            {
                if (!string.Equals(pair.Value, proof, StringComparison.Ordinal)) continue;
                clientId = pair.Key;
                break;
            }
            if (clientId == 0) return false;
            challenges.Remove(clientId);
            users.Add(clientId, authorId);
            return true;
        }

        public bool IsAuthenticated(ulong clientId) => users.ContainsKey(clientId);
        public bool IsUser(ulong clientId, ulong userId) => users.TryGetValue(clientId, out var actual) && actual == userId;
        public ulong ClientFor(ulong userId)
        {
            foreach (var pair in users) if (pair.Value == userId) return pair.Key;
            return 0;
        }
        public void Remove(ulong clientId) { challenges.Remove(clientId); users.Remove(clientId); }
        public void Clear() { challenges.Clear(); users.Clear(); }
    }
}
