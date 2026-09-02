using DndProximityVoice.Voice;
using UnityEngine;

namespace DndProximityVoice.Players
{
    public enum PrivateVoiceGroup : byte
    {
        None = 0,
        A = 1,
        B = 2,
        C = 3
    }

    public static class PrivateVoiceGroupRules
    {
        public static bool IsValid(PrivateVoiceGroup group)
        {
            return group >= PrivateVoiceGroup.None && group <= PrivateVoiceGroup.C;
        }

        public static bool CanHear(
            bool isolationEnabled,
            PrivateVoiceGroup listenerGroup,
            PrivateVoiceGroup speakerGroup)
        {
            if (!isolationEnabled)
            {
                return true;
            }

            return listenerGroup != PrivateVoiceGroup.None && listenerGroup == speakerGroup;
        }

        public static string GetDisplayName(PrivateVoiceGroup group)
        {
            switch (group)
            {
                case PrivateVoiceGroup.A:
                    return "A";
                case PrivateVoiceGroup.B:
                    return "B";
                case PrivateVoiceGroup.C:
                    return "C";
                default:
                    return "NESSUNO";
            }
        }
    }

    public sealed class PlayerData
    {
        public PlayerData(
            ulong discordUserId,
            string displayName,
            Vector2 position,
            Color color)
        {
            DiscordUserId = discordUserId;
            DisplayName = displayName ?? string.Empty;
            Position = position;
            TargetPosition = position;
            Color = color;
        }

        public ulong DiscordUserId { get; }

        public string DisplayName { get; internal set; }

        public Vector2 Position { get; internal set; }

        public Vector2 TargetPosition { get; internal set; }

        public Color Color { get; }

        public bool IsDM { get; internal set; }

        public bool IsLocal { get; internal set; }

        public bool IsConnected { get; internal set; }

        public VoiceMode VoiceMode { get; internal set; } = VoiceMode.Normal;

        public PrivateVoiceGroup PrivateGroup { get; internal set; } = PrivateVoiceGroup.None;
    }
}
