// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Playback.Android;

using System;

internal static class FormatConverter
{
    public static void ConvertFromStereoShortToMonoDouble(byte[] buffer, int bufferLength, double[] destination)
    {
        const int channels = 2;
        const int bytesPerSamplePerChannel = 2;
        const int bytesPerSample = bytesPerSamplePerChannel * channels;
        int bufferSampleCount = Math.Min(bufferLength / bytesPerSample, destination.Length);

        for (int i = 0; i < bufferSampleCount; i++)
        {
            short sample = BitConverter.ToInt16(buffer, i * bytesPerSample);
            destination[i] = sample / 32768.0;
        }
    }
}
