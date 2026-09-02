using DndProximityVoice.Voice;
using NUnit.Framework;

namespace DndProximityVoice.Tests
{
    public sealed class PcmRingBufferTests
    {
        [Test]
        public void ReadPreservesFifoOrder()
        {
            var buffer = new PcmRingBuffer(8);
            buffer.Write(new[] { 0.1f, 0.2f, 0.3f }, 0, 3);

            var destination = new float[3];
            var read = buffer.Read(destination, 0, destination.Length);

            Assert.That(read, Is.EqualTo(3));
            Assert.That(destination, Is.EqualTo(new[] { 0.1f, 0.2f, 0.3f }));
            Assert.That(buffer.Count, Is.Zero);
        }

        [Test]
        public void UnderflowFillsRemainingSamplesWithSilence()
        {
            var buffer = new PcmRingBuffer(4);
            buffer.Write(new[] { 0.5f, -0.5f }, 0, 2);

            var destination = new[] { 1f, 1f, 1f, 1f };
            var read = buffer.Read(destination, 0, destination.Length);

            Assert.That(read, Is.EqualTo(2));
            Assert.That(destination, Is.EqualTo(new[] { 0.5f, -0.5f, 0f, 0f }));
            Assert.That(buffer.UnderflowSamples, Is.EqualTo(2));
        }

        [Test]
        public void OverflowRetainsNewestSamplesToBoundLatency()
        {
            var buffer = new PcmRingBuffer(3);
            buffer.Write(new[] { 1f, 2f, 3f, 4f, 5f }, 0, 5);

            var destination = new float[3];
            buffer.Read(destination, 0, destination.Length);

            Assert.That(destination, Is.EqualTo(new[] { 3f, 4f, 5f }));
            Assert.That(buffer.DroppedSamples, Is.EqualTo(2));
        }

        [Test]
        public void LatencyTrimDiscardsOldAudioAndRetainsNewestTargetWindow()
        {
            var buffer = new PcmRingBuffer(12);
            buffer.Write(new[] { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f }, 0, 8);

            var trimmed = buffer.TrimIfAbove(6, 3);
            var destination = new float[3];
            buffer.Read(destination, 0, destination.Length);

            Assert.That(trimmed, Is.EqualTo(5));
            Assert.That(destination, Is.EqualTo(new[] { 6f, 7f, 8f }));
            Assert.That(buffer.DroppedSamples, Is.EqualTo(5));
        }

        [Test]
        public void LatencyTrimLeavesHealthyBufferUntouched()
        {
            var buffer = new PcmRingBuffer(8);
            buffer.Write(new[] { 1f, 2f, 3f }, 0, 3);

            var trimmed = buffer.TrimIfAbove(4, 2);

            Assert.That(trimmed, Is.Zero);
            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(buffer.DroppedSamples, Is.Zero);
        }
    }
}
