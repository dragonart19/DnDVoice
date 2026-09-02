using System;

namespace DndProximityVoice.Voice
{
    public static class Pcm16Converter
    {
        private const float NormalizationFactor = 1f / 32768f;

        public static int ConvertInterleavedToMono(
            short[] input,
            int inputOffset,
            int samplesPerChannel,
            int channels,
            float[] output,
            int outputOffset)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (samplesPerChannel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(samplesPerChannel));
            }

            if (channels <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(channels));
            }

            var totalInputSamples = checked(samplesPerChannel * channels);
            ValidateRange(input.Length, inputOffset, totalInputSamples, nameof(inputOffset));
            ValidateRange(output.Length, outputOffset, samplesPerChannel, nameof(outputOffset));

            for (var frame = 0; frame < samplesPerChannel; frame++)
            {
                var sum = 0f;
                var frameOffset = inputOffset + (frame * channels);

                for (var channel = 0; channel < channels; channel++)
                {
                    sum += input[frameOffset + channel] * NormalizationFactor;
                }

                output[outputOffset + frame] = sum / channels;
            }

            return samplesPerChannel;
        }

        private static void ValidateRange(int arrayLength, int offset, int length, string parameterName)
        {
            if (offset < 0 || length < 0 || offset > arrayLength - length)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
