using DndProximityVoice.Voice;
using NUnit.Framework;
using UnityEngine;

namespace DndProximityVoice.Tests
{
    public sealed class VoiceRangeCalculatorTests
    {
        [TestCase(0f, 1f)]
        [TestCase(2f, 1f)]
        [TestCase(4f, 0.8f)]
        [TestCase(6f, 0.55f)]
        [TestCase(8f, 0.3f)]
        [TestCase(10f, 0.1f)]
        [TestCase(12f, 0f)]
        [TestCase(20f, 0f)]
        public void DefaultCurveMatchesTheTwelveMeterVoicePreset(float distance, float expected)
        {
            var volume = VoiceRangeCalculator.Evaluate(
                distance,
                VoiceRangeCalculator.DefaultMinimumDistance,
                VoiceRangeCalculator.DefaultMaximumDistance,
                VoiceRangeCalculator.CreateDefaultCurve());

            Assert.That(volume, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void RelativePositionUsesTokenDistanceAndHorizontalDirection()
        {
            VoiceRangeCalculator.CalculateRelativePosition(
                new Vector2(1f, 2f),
                new Vector2(4f, 6f),
                out var horizontalDirection,
                out var distanceMeters);

            Assert.That(distanceMeters, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(horizontalDirection, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void RelativePositionAtSamePointIsCentered()
        {
            VoiceRangeCalculator.CalculateRelativePosition(
                Vector2.one,
                Vector2.one,
                out var horizontalDirection,
                out var distanceMeters);

            Assert.That(distanceMeters, Is.Zero);
            Assert.That(horizontalDirection, Is.Zero);
        }
    }
}
