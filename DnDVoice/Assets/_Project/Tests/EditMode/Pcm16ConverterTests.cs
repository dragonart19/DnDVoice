using DndProximityVoice.Voice;
using NUnit.Framework;

namespace DndProximityVoice.Tests
{
    public sealed class Pcm16ConverterTests
    {
        [Test]
        public void ConvertsMonoExtremesToNormalizedFloats()
        {
            var input = new short[] { short.MinValue, short.MaxValue };
            var output = new float[2];

            var converted = Pcm16Converter.ConvertInterleavedToMono(input, 0, 2, 1, output, 0);

            Assert.That(converted, Is.EqualTo(2));
            Assert.That(output[0], Is.EqualTo(-1f));
            Assert.That(output[1], Is.EqualTo(short.MaxValue / 32768f));
        }

        [Test]
        public void MixesInterleavedStereoToMono()
        {
            var input = new short[] { 16384, -16384, 8192, 8192 };
            var output = new float[2];

            Pcm16Converter.ConvertInterleavedToMono(input, 0, 2, 2, output, 0);

            Assert.That(output[0], Is.EqualTo(0f).Within(0.00001f));
            Assert.That(output[1], Is.EqualTo(0.25f).Within(0.00001f));
        }
    }
}
