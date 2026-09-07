using DndProximityVoice.Realtime;
using NUnit.Framework;

namespace DndProximityVoice.Tests.EditMode
{
    public sealed class RelayPeerAuthorizationTests
    {
        [Test]
        public void ValidChallengeBindsDiscordUserToRelayClient()
        {
            var authorization = new RelayPeerAuthorization();
            var proof = authorization.Challenge(42);

            var accepted = authorization.TryConfirm(9001, proof, out var clientId);

            Assert.That(accepted, Is.True);
            Assert.That(clientId, Is.EqualTo(42));
            Assert.That(authorization.IsAuthenticated(42), Is.True);
            Assert.That(authorization.IsUser(42, 9001), Is.True);
            Assert.That(authorization.ClientFor(9001), Is.EqualTo(42));
        }

        [Test]
        public void InvalidChallengeDoesNotAuthenticateClient()
        {
            var authorization = new RelayPeerAuthorization();
            authorization.Challenge(42);

            var accepted = authorization.TryConfirm(9001, "proof-non-valida", out var clientId);

            Assert.That(accepted, Is.False);
            Assert.That(clientId, Is.Zero);
            Assert.That(authorization.IsAuthenticated(42), Is.False);
        }

        [Test]
        public void DiscordUserCannotBeBoundToTwoRelayClients()
        {
            var authorization = new RelayPeerAuthorization();
            var firstProof = authorization.Challenge(42);
            var secondProof = authorization.Challenge(84);
            Assert.That(authorization.TryConfirm(9001, firstProof, out _), Is.True);

            var accepted = authorization.TryConfirm(9001, secondProof, out var clientId);

            Assert.That(accepted, Is.False);
            Assert.That(clientId, Is.Zero);
            Assert.That(authorization.IsAuthenticated(84), Is.False);
        }

        [Test]
        public void NewChallengeInvalidatesPreviousClientChallenge()
        {
            var authorization = new RelayPeerAuthorization();
            var expiredProof = authorization.Challenge(42);
            var currentProof = authorization.Challenge(42);

            Assert.That(authorization.TryConfirm(9001, expiredProof, out _), Is.False);
            Assert.That(authorization.TryConfirm(9001, currentProof, out var clientId), Is.True);
            Assert.That(clientId, Is.EqualTo(42));
        }

        [Test]
        public void RemoveAndClearDiscardEstablishedBindings()
        {
            var authorization = new RelayPeerAuthorization();
            Assert.That(authorization.TryConfirm(9001, authorization.Challenge(42), out _), Is.True);
            Assert.That(authorization.TryConfirm(9002, authorization.Challenge(84), out _), Is.True);

            authorization.Remove(42);
            Assert.That(authorization.IsAuthenticated(42), Is.False);
            Assert.That(authorization.IsAuthenticated(84), Is.True);

            authorization.Clear();
            Assert.That(authorization.IsAuthenticated(84), Is.False);
            Assert.That(authorization.ClientFor(9002), Is.Zero);
        }
    }
}
