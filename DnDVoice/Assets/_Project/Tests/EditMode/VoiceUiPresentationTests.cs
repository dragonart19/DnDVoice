using DndProximityVoice.UI;
using DndProximityVoice.Voice;
using NUnit.Framework;

namespace DndProximityVoice.Tests.EditMode
{
    public sealed class VoiceUiPresentationTests
    {
        [Test]
        public void PrivateChannelIsReportedAsBlockedWithoutRelyingOnColor()
        {
            var result = VoiceUiPresentation.Calculate(1f, VoiceMode.Normal, 0f, true);

            Assert.That(result.Level, Is.EqualTo(VoiceAudibilityLevel.Blocked));
            Assert.That(result.NormalizedStrength, Is.Zero);
            Assert.That(result.Label, Is.EqualTo("PRIVATA"));
        }

        [Test]
        public void SpeakerOutsideModeRangeIsReportedAsOutOfRange()
        {
            var result = VoiceUiPresentation.Calculate(4f, VoiceMode.Whisper, 0f, false);

            Assert.That(result.Level, Is.EqualTo(VoiceAudibilityLevel.OutOfRange));
            Assert.That(result.NormalizedStrength, Is.Zero);
        }

        [Test]
        public void HeavyWallProducesWeakerPresentationThanClearPath()
        {
            var clear = VoiceUiPresentation.Calculate(2f, VoiceMode.Normal, 0f, false);
            var blocked = VoiceUiPresentation.Calculate(2f, VoiceMode.Normal, 1f, false);

            Assert.That(clear.NormalizedStrength, Is.GreaterThan(blocked.NormalizedStrength));
            Assert.That((int)clear.Level, Is.GreaterThan((int)blocked.Level));
        }

        [Test]
        public void PlayerOrderingPrioritizesLocalThenSpeakingThenConnected()
        {
            Assert.That(VoiceUiPresentation.ComparePlayers(
                true, false, true, false, "Zed",
                false, true, true, true, "Ada"), Is.LessThan(0));
            Assert.That(VoiceUiPresentation.ComparePlayers(
                false, true, true, false, "Zed",
                false, false, true, true, "Ada"), Is.LessThan(0));
            Assert.That(VoiceUiPresentation.ComparePlayers(
                false, false, true, false, "Zed",
                false, false, false, true, "Ada"), Is.LessThan(0));
        }

        [TestCase(1366f, 768f, 1.0666f)]
        [TestCase(1920f, 1080f, 1.5f)]
        [TestCase(2560f, 1440f, 2f)]
        public void TacticalLayoutUsesReadableScaleAtSupportedResolutions(
            float width,
            float height,
            float expectedScale)
        {
            var scale = AppUiTheme.CalculateResponsiveScale(width, height, 1180f, 720f);

            Assert.That(scale, Is.EqualTo(expectedScale).Within(0.001f));
            Assert.That(width / scale, Is.GreaterThanOrEqualTo(1279f));
            Assert.That(height / scale, Is.GreaterThanOrEqualTo(719f));
        }
    }
}
