// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Playback.Android;

using System;
using System.Numerics;
using Woohoo.Audio.Core.Playback;

public sealed class AndroidAudioPlayerVisualizationData : IAudioPlayerVisualization
{
    private readonly Lock dataLock = new();

    public AndroidAudioPlayerVisualizationData()
    {
        this.PowerSpectrumDensity = new double[257];
        this.PowerSpectrumDensityBands = new double[8];
        this.Waveform = new double[441];
    }

    public double[] PowerSpectrumDensity { get; }

    public double[] PowerSpectrumDensityBands { get; }

    public double[] Waveform { get; }

    public void CopyTo(double[] plotPsd, double[] plotBands, double[] plotWave)
    {
        lock (this.dataLock)
        {
            Array.Copy(this.PowerSpectrumDensity, plotPsd, Math.Min(this.PowerSpectrumDensity.Length, plotPsd.Length));
            Array.Copy(this.PowerSpectrumDensityBands, plotBands, Math.Min(this.PowerSpectrumDensityBands.Length, plotBands.Length));
            Array.Copy(this.Waveform, plotWave, Math.Min(this.Waveform.Length, plotWave.Length));
        }
    }

    public void AnalyzeBuffer(byte[] buffer, int bufferLength)
    {
        lock (this.dataLock)
        {
            Array.Clear(this.Waveform);
            FormatConverter.ConvertFromStereoShortToMonoDouble(buffer, bufferLength, this.Waveform);

            double[] paddedAudio = FftSharp.Pad.ZeroPad(this.Waveform);

            Complex[] spectrum = FftSharp.FFT.Forward(paddedAudio);
            double[] psd = FftSharp.FFT.Power(spectrum);
            Array.Copy(psd, this.PowerSpectrumDensity, psd.Length);

            int index = 0;
            for (int i = 0; i < 8; i++)
            {
                double total = 0;
                int sampleCount = (int)Math.Pow(2, i);
                for (int j = 0; j < sampleCount; j++)
                {
                    total += psd[index];
                    index++;
                }

                this.PowerSpectrumDensityBands[i] = total / sampleCount;
            }
        }
    }
}
