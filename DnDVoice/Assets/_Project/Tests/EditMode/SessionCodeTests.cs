using DndProximityVoice.Session;
using NUnit.Framework;

namespace DndProximityVoice.Tests.EditMode
{
    public sealed class SessionCodeTests
    {
        [Test]
        public void Generate_ReturnsAValidCode()
        {
            var code = SessionCode.Generate();

            Assert.That(SessionCode.IsValid(code), Is.True);
            Assert.That(code.Length, Is.EqualTo(SessionCode.Length));
        }

        [Test]
        public void Normalize_RemovesSeparatorsAndKeepsRelayCharacters()
        {
            var normalized = SessionCode.Normalize("ab-cd i2ef");

            Assert.That(normalized, Is.EqualTo("ABCDI2"));
        }

        [Test]
        public void DeriveLobbySecret_IsStableWithoutExposingTheCode()
        {
            var first = SessionCode.DeriveLobbySecret("ABCD2E");
            var second = SessionCode.DeriveLobbySecret("abcd-2e");

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Does.Not.Contain("ABCD2E"));
        }
    }
}
