using DndProximityVoice.Voice;
using NUnit.Framework;

namespace DndProximityVoice.Tests.EditMode
{
    public sealed class VoiceModeProfileTests
    {
        [TestCase(VoiceMode.Whisper, 3f)]
        [TestCase(VoiceMode.Normal, 12f)]
        [TestCase(VoiceMode.Shout, 24f)]
        public void MaximumDistanceMatchesMode(VoiceMode mode, float expectedDistance)
        {
            Assert.That(VoiceModeProfile.GetMaximumDistance(mode), Is.EqualTo(expectedDistance));
        }

        [Test]
        public void WhisperIsQuieterThanNormalAtCloseRange()
        {
            Assert.That(
                VoiceModeProfile.GetOutputGain(VoiceMode.Whisper),
                Is.LessThan(VoiceModeProfile.GetOutputGain(VoiceMode.Normal)));
        }

        [TestCase(VoiceMode.Whisper)]
        [TestCase(VoiceMode.Normal)]
        [TestCase(VoiceMode.Shout)]
        public void VoiceStopsAtMaximumRange(VoiceMode mode)
        {
            var volume = VoiceRangeCalculator.Evaluate(
                VoiceModeProfile.GetMaximumDistance(mode),
                VoiceModeProfile.GetMinimumDistance(mode),
                VoiceModeProfile.GetMaximumDistance(mode),
                VoiceRangeCalculator.CreateDefaultCurve());

            Assert.That(volume, Is.Zero);
        }
    }
}
