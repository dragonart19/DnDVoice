using System;
using System.Security.Cryptography;
using System.Text;

namespace DndProximityVoice.Session
{
    public static class SessionCode
    {
        public const int Length = 6;

        private const string GeneratedAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const string AcceptedAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string SecretNamespace = "dnd-proximity-voice/v1/";

        public static string Generate()
        {
            var randomBytes = new byte[Length];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(randomBytes);
            }

            var characters = new char[Length];
            for (var index = 0; index < Length; index++)
            {
                characters[index] = GeneratedAlphabet[randomBytes[index] & 31];
            }

            return new string(characters);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var normalized = new StringBuilder(Length);
            foreach (var character in value.ToUpperInvariant())
            {
                if (AcceptedAlphabet.IndexOf(character) >= 0 && normalized.Length < Length)
                {
                    normalized.Append(character);
                }
            }

            return normalized.ToString();
        }

        public static bool IsValid(string value)
        {
            return Normalize(value).Length == Length;
        }

        public static string DeriveLobbySecret(string value)
        {
            var normalized = Normalize(value);
            if (!IsValid(normalized))
            {
                throw new ArgumentException($"Il codice sessione deve contenere {Length} caratteri.", nameof(value));
            }

            byte[] hash;
            using (var sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(SecretNamespace + normalized));
            }

            return Convert.ToBase64String(hash)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
