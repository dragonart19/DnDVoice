using System;

namespace DndProximityVoice.Voice
{
    /// <summary>
    /// A bounded PCM buffer shared by a Discord callback thread and Unity's audio thread.
    /// Overflow discards the oldest samples so latency cannot grow without bound.
    /// Underflow always produces deterministic silence.
    /// </summary>
    public sealed class PcmRingBuffer
    {
        private readonly float[] samples;
        private readonly object syncRoot = new object();
        private int readPosition;
        private int writePosition;
        private int count;
        private long droppedSamples;
        private long underflowSamples;

        public PcmRingBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
            }

            samples = new float[capacity];
        }

        public int Capacity => samples.Length;

        public int Count
        {
            get
            {
                lock (syncRoot)
                {
                    return count;
                }
            }
        }

        public long DroppedSamples
        {
            get
            {
                lock (syncRoot)
                {
                    return droppedSamples;
                }
            }
        }

        public long UnderflowSamples
        {
            get
            {
                lock (syncRoot)
                {
                    return underflowSamples;
                }
            }
        }

        public void Write(float[] source, int sourceOffset, int length)
        {
            ValidateRange(source, sourceOffset, length);

            lock (syncRoot)
            {
                for (var index = 0; index < length; index++)
                {
                    if (count == samples.Length)
                    {
                        readPosition = (readPosition + 1) % samples.Length;
                        count--;
                        droppedSamples++;
                    }

                    samples[writePosition] = source[sourceOffset + index];
                    writePosition = (writePosition + 1) % samples.Length;
                    count++;
                }
            }
        }

        public int Read(float[] destination, int destinationOffset, int length)
        {
            ValidateRange(destination, destinationOffset, length);

            lock (syncRoot)
            {
                var readable = Math.Min(length, count);

                for (var index = 0; index < readable; index++)
                {
                    destination[destinationOffset + index] = samples[readPosition];
                    readPosition = (readPosition + 1) % samples.Length;
                }

                count -= readable;

                var missing = length - readable;
                if (missing > 0)
                {
                    Array.Clear(destination, destinationOffset + readable, missing);
                    underflowSamples += missing;
                }

                return readable;
            }
        }

        public int TrimIfAbove(int maximumCount, int targetCount)
        {
            if (maximumCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumCount));
            }

            if (targetCount < 0 || targetCount > maximumCount)
            {
                throw new ArgumentOutOfRangeException(nameof(targetCount));
            }

            lock (syncRoot)
            {
                if (count <= maximumCount)
                {
                    return 0;
                }

                var discarded = count - targetCount;
                readPosition = (readPosition + discarded) % samples.Length;
                count -= discarded;
                droppedSamples += discarded;
                return discarded;
            }
        }

        public void Clear()
        {
            lock (syncRoot)
            {
                Array.Clear(samples, 0, samples.Length);
                readPosition = 0;
                writePosition = 0;
                count = 0;
            }
        }

        private static void ValidateRange(float[] array, int offset, int length)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (offset < 0 || length < 0 || offset > array.Length - length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset and length must describe a valid array range.");
            }
        }
    }
}
