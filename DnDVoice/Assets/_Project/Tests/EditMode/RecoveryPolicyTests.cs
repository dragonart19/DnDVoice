using DndProximityVoice.Discord;
using DndProximityVoice.Session;
using DndProximityVoice.Voice;
using NUnit.Framework;

namespace DndProximityVoice.Tests.EditMode
{
    public sealed class RecoveryPolicyTests
    {
        [Test]
        public void TemporaryDiscordReconnectPreservesAnActiveLobby()
        {
            Assert.That(
                SessionRecoveryPolicy.ShouldPreserveJoinedSession(
                    DiscordAuthState.Connecting,
                    DiscordSessionState.Joined,
                    42),
                Is.True);
        }

        [TestCase(DiscordAuthState.Failed, DiscordSessionState.Joined, 42UL)]
        [TestCase(DiscordAuthState.Connecting, DiscordSessionState.Joined, 0UL)]
        [TestCase(DiscordAuthState.Connecting, DiscordSessionState.Ready, 42UL)]
        public void SessionIsNotPreservedOutsideATemporaryReconnect(
            DiscordAuthState authState,
            DiscordSessionState sessionState,
            ulong lobbyId)
        {
            Assert.That(
                SessionRecoveryPolicy.ShouldPreserveJoinedSession(
                    authState,
                    sessionState,
                    lobbyId),
                Is.False);
        }

        [Test]
        public void VoiceRetryStopsAfterThreeAttempts()
        {
            Assert.That(
                VoiceReconnectPolicy.CanSchedule(true, DiscordSessionState.Joined, 0),
                Is.True);
            Assert.That(
                VoiceReconnectPolicy.CanSchedule(true, DiscordSessionState.Joined, 2),
                Is.True);
            Assert.That(
                VoiceReconnectPolicy.CanSchedule(true, DiscordSessionState.Joined, 3),
                Is.False);
            Assert.That(
                VoiceReconnectPolicy.CanSchedule(false, DiscordSessionState.Joined, 0),
                Is.False);
        }

        [Test]
        public void VoiceRetryUsesShortBoundedBackoff()
        {
            Assert.That(VoiceReconnectPolicy.GetDelaySeconds(1), Is.EqualTo(2f));
            Assert.That(VoiceReconnectPolicy.GetDelaySeconds(2), Is.EqualTo(4f));
            Assert.That(VoiceReconnectPolicy.GetDelaySeconds(3), Is.EqualTo(6f));
            Assert.That(VoiceReconnectPolicy.GetDelaySeconds(10), Is.EqualTo(6f));
        }
    }
}
