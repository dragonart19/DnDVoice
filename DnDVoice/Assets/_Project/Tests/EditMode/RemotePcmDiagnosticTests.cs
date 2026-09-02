using DndProximityVoice.Voice;
using NUnit.Framework;

namespace DndProximityVoice.Tests
{
    public sealed class RemotePcmDiagnosticTests
    {
        [Test]
        public void DiagnosticMonoUsesTheSameBoundedPlaybackBuffer()
        {
            var stream = new RemotePcmStream(99, 8);
            var input = new[] { 0.1f, 0.2f, 0.3f, 0.4f };

            stream.PushDiagnosticMono(input, 0, input.Length, 48000);
            var output = new float[4];
            var read = stream.Read(output, 0, output.Length);

            Assert.That(read, Is.EqualTo(4));
            Assert.That(output, Is.EqualTo(input));
            Assert.That(stream.SampleRate, Is.EqualTo(48000));
            Assert.That(stream.SourceChannels, Is.EqualTo(1));
        }
    }
}
