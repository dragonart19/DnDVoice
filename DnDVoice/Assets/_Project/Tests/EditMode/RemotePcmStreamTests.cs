using System;
using System.Runtime.InteropServices;
using DndProximityVoice.Voice;
using NUnit.Framework;

namespace DndProximityVoice.Tests
{
    public sealed class RemotePcmStreamTests
    {
        [Test]
        public void PushCopiesAndConvertsCallbackMemoryBeforeReturning()
        {
            var input = new short[] { 16384, -16384, 8192, 8192 };
            var pointer = Marshal.AllocHGlobal(input.Length * sizeof(short));

            try
            {
                Marshal.Copy(input, 0, pointer, input.Length);
                var stream = new RemotePcmStream(42, 32);

                var accepted = stream.Push(pointer, 2, 48000, 2);
                var output = new float[2];
                var read = stream.Read(output, 0, output.Length);

                Assert.That(accepted, Is.True);
                Assert.That(read, Is.EqualTo(2));
                Assert.That(stream.SampleRate, Is.EqualTo(48000));
                Assert.That(stream.SourceChannels, Is.EqualTo(2));
                Assert.That(output[0], Is.EqualTo(0f).Within(0.00001f));
                Assert.That(output[1], Is.EqualTo(0.25f).Within(0.00001f));
            }
            finally
            {
                Marshal.FreeHGlobal(pointer);
            }
        }
    }
}
