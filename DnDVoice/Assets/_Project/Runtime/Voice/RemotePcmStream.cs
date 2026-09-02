using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DndProximityVoice.Voice
{
    public sealed class RemotePcmStream
    {
        private readonly object conversionLock = new object();
        private readonly PcmRingBuffer buffer;
        private short[] interleavedScratch = Array.Empty<short>();
        private float[] monoScratch = Array.Empty<float>();
        private int sampleRate;
        private int sourceChannels;
        private int playbackReady;
        private long frameCount;

        public RemotePcmStream(ulong userId, int capacityInMonoSamples)
        {
            UserId = userId;
            buffer = new PcmRingBuffer(capacityInMonoSamples);
        }

        public ulong UserId { get; }

        public int SampleRate => Volatile.Read(ref sampleRate);

        public int SourceChannels => Volatile.Read(ref sourceChannels);

        public int BufferedSamples => buffer.Count;

        public long DroppedSamples => buffer.DroppedSamples;

        public long UnderflowSamples => buffer.UnderflowSamples;

        public long FrameCount => Interlocked.Read(ref frameCount);

        public bool PlaybackReady
        {
            get => Volatile.Read(ref playbackReady) == 1;
            set => Volatile.Write(ref playbackReady, value ? 1 : 0);
        }

        public bool Push(
            IntPtr data,
            ulong samplesPerChannel,
            int inputSampleRate,
            ulong inputChannels)
        {
            if (data == IntPtr.Zero || samplesPerChannel == 0 || inputSampleRate <= 0 ||
                inputChannels == 0 || samplesPerChannel > int.MaxValue || inputChannels > int.MaxValue)
            {
                return false;
            }

            var monoSampleCount = (int)samplesPerChannel;
            var channelCount = (int)inputChannels;
            int interleavedSampleCount;
            try
            {
                interleavedSampleCount = checked(monoSampleCount * channelCount);
            }
            catch (OverflowException)
            {
                return false;
            }

            lock (conversionLock)
            {
                EnsureScratchCapacity(interleavedSampleCount, monoSampleCount);
                Marshal.Copy(data, interleavedScratch, 0, interleavedSampleCount);
                Pcm16Converter.ConvertInterleavedToMono(
                    interleavedScratch,
                    0,
                    monoSampleCount,
                    channelCount,
                    monoScratch,
                    0);
                buffer.Write(monoScratch, 0, monoSampleCount);
                Volatile.Write(ref sampleRate, inputSampleRate);
                Volatile.Write(ref sourceChannels, channelCount);
                Interlocked.Increment(ref frameCount);
            }

            return true;
        }

        public int Read(float[] destination, int destinationOffset, int length)
        {
            return buffer.Read(destination, destinationOffset, length);
        }

        public int TrimLatencyIfNeeded(int maximumBufferedSamples, int targetBufferedSamples)
        {
            return buffer.TrimIfAbove(maximumBufferedSamples, targetBufferedSamples);
        }

        public void PushDiagnosticMono(float[] samples, int offset, int length, int inputSampleRate)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (offset < 0 || length < 0 || offset > samples.Length - length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (inputSampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inputSampleRate));
            }

            buffer.Write(samples, offset, length);
            Volatile.Write(ref sampleRate, inputSampleRate);
            Volatile.Write(ref sourceChannels, 1);
            Interlocked.Increment(ref frameCount);
        }

        public void Clear()
        {
            PlaybackReady = false;
            buffer.Clear();
        }

        private void EnsureScratchCapacity(int interleavedSamples, int monoSamples)
        {
            if (interleavedScratch.Length < interleavedSamples)
            {
                interleavedScratch = new short[interleavedSamples];
            }

            if (monoScratch.Length < monoSamples)
            {
                monoScratch = new float[monoSamples];
            }
        }
    }
}
