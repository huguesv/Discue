// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Playback.Windows.Internal.Wasapi;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using global::Windows.Win32.Media.Audio;
using global::Windows.Win32.Media.Audio.Endpoints;
using winmdroot = global::Windows.Win32;

[SupportedOSPlatform("windows6.0.6000")]
internal sealed class AudioCapture
{
    private const int MaxBufferSize = 4096;

    private readonly Lock lockObject = new();
    private readonly Queue<double> sampleBuffer = new();

    private IAudioClient? audioClient;
    private IAudioCaptureClient? captureClient;
    private IAudioEndpointVolume? endpointVolume;
    private WAVEFORMATEX waveFormat;
    private Thread? captureThread;
    private bool isCapturing;
    private bool isFloat;
    private ushort bitsPerSample;

    [Flags]
    internal enum AUDCLNT_STREAMFLAGS : uint
    {
        AUDCLNT_STREAMFLAGS_LOOPBACK = 0x00020000,
    }

    public double[] GetRecentSamples()
    {
        lock (this.lockObject)
        {
            return [.. this.sampleBuffer];
        }
    }

    public void Start()
    {
        if (this.audioClient is null)
        {
            throw new InvalidOperationException("Audio client not initialized");
        }

        if (this.endpointVolume is null)
        {
            throw new InvalidOperationException("Audio endpoint volume not initialized");
        }

        Debug.Assert(this.isCapturing == false, "Should not already be capturing when calling Start.");

        this.audioClient.Start();

        this.isCapturing = true;
        this.captureThread = new Thread(this.CaptureLoop);
        this.captureThread.Start();
    }

    public void Stop()
    {
        this.isCapturing = false;
        this.captureThread?.Join();
        this.captureThread = null;
        this.audioClient?.Stop();
    }

    internal unsafe void Initialize(IMMDevice device)
    {
        try
        {
            Guid iidIAudioClient = typeof(IAudioClient).GUID;
            device.Activate(&iidIAudioClient, winmdroot.System.Com.CLSCTX.CLSCTX_ALL, null, out object audioClientObj);
            this.audioClient = (IAudioClient)audioClientObj;

            if (this.audioClient == null)
            {
                throw new Exception("Failed to activate audio client");
            }

            Guid iidIAudioEndPointVolume = typeof(IAudioEndpointVolume).GUID;
            device.Activate(&iidIAudioEndPointVolume, winmdroot.System.Com.CLSCTX.CLSCTX_ALL, null, out object audioVolumeObj);
            this.endpointVolume = (IAudioEndpointVolume)audioVolumeObj;

            if (this.endpointVolume == null)
            {
                throw new Exception("Failed to activate audio endpoint volume");
            }

            WAVEFORMATEX* formatPtr = null;
            this.audioClient.GetMixFormat(&formatPtr);
            if (formatPtr == null)
            {
                throw new Exception("Failed to get mix format");
            }

            try
            {
                this.waveFormat.cbSize = (*formatPtr).cbSize;
                this.waveFormat.nAvgBytesPerSec = (*formatPtr).nAvgBytesPerSec;
                this.waveFormat.nBlockAlign = (*formatPtr).nBlockAlign;
                this.waveFormat.nChannels = (*formatPtr).nChannels;
                this.waveFormat.nSamplesPerSec = (*formatPtr).nSamplesPerSec;
                this.waveFormat.wBitsPerSample = (*formatPtr).wBitsPerSample;
                this.waveFormat.wFormatTag = (*formatPtr).wFormatTag;

                this.isFloat = false;
                this.bitsPerSample = this.waveFormat.wBitsPerSample;
                if (this.waveFormat.wFormatTag == 0xFFFE)
                {
                    WAVEFORMATEXTENSIBLE extFormat = (WAVEFORMATEXTENSIBLE)Marshal.PtrToStructure((nint)formatPtr, typeof(WAVEFORMATEXTENSIBLE))!;
                    if (extFormat.SubFormat == new Guid("00000003-0000-0010-8000-00aa00389b71"))
                    {
                        this.isFloat = true;
                    }
                    else if (extFormat.SubFormat == new Guid("00000001-0000-0010-8000-00aa00389b71"))
                    {
                        this.isFloat = false;
                    }
                    else
                    {
                        throw new Exception("Unsupported subformat in WAVEFORMATEXTENSIBLE");
                    }

                    this.bitsPerSample = extFormat.Format.wBitsPerSample;
                }
                else if (this.waveFormat.wFormatTag == 3)
                {
                    this.isFloat = true;
                }
                else if (this.waveFormat.wFormatTag == 1)
                {
                    this.isFloat = false;
                }
                else
                {
                    throw new Exception("Unsupported format tag");
                }

                if ((this.isFloat && this.bitsPerSample != 32 && this.bitsPerSample != 64) ||
                    (!this.isFloat && this.bitsPerSample != 16 && this.bitsPerSample != 32))
                {
                    throw new Exception("Unsupported bit depth");
                }

                long bufferDuration = 10000000L / 10;
                Guid emptyGuid = Guid.Empty;
                this.audioClient.Initialize(
                    winmdroot.Media.Audio.AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED,
                    (uint)AUDCLNT_STREAMFLAGS.AUDCLNT_STREAMFLAGS_LOOPBACK,
                    bufferDuration,
                    0,
                    formatPtr,
                    &emptyGuid);

                Guid iidIAudioCaptureClient = typeof(IAudioCaptureClient).GUID;
                this.audioClient.GetService(&iidIAudioCaptureClient, out void* ppv);

                try
                {
                    this.captureClient = (IAudioCaptureClient)Marshal.GetObjectForIUnknown((IntPtr)ppv);
                    if (this.captureClient == null)
                    {
                        throw new Exception("Failed to get capture client");
                    }
                }
                finally
                {
                    if (ppv != null)
                    {
                        Marshal.Release((IntPtr)ppv);
                    }
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem((IntPtr)formatPtr);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error initializing capture: " + ex.Message);
        }
    }

    private unsafe void CaptureLoop()
    {
        while (this.isCapturing)
        {
            Thread.Sleep(10);

            if (this.captureClient == null || this.endpointVolume == null)
            {
                continue;
            }

            try
            {
                ulong pos = 0;
                ulong qpc = 0;
                byte* bufferPtr = null;
                this.captureClient.GetBuffer(&bufferPtr, out uint numFrames, out uint flags, &pos, &qpc);

                try
                {
                    if (numFrames > 0)
                    {
                        this.endpointVolume.GetMasterVolumeLevelScalar(out float volumeLevel);

                        int byteLength = (int)(numFrames * this.waveFormat.nBlockAlign);

                        var buffer = ArrayPool<byte>.Shared.Rent(byteLength);
                        var avgs = ArrayPool<double>.Shared.Rent((int)numFrames);

                        try
                        {
                            Marshal.Copy((nint)bufferPtr, buffer, 0, byteLength);

                            int bytesPerSample = this.bitsPerSample / 8;

                            for (uint f = 0; f < numFrames; f++)
                            {
                                double frameSum = 0.0;
                                for (ushort ch = 0; ch < this.waveFormat.nChannels; ch++)
                                {
                                    int offset = (int)((f * this.waveFormat.nBlockAlign) + (ch * bytesPerSample));
                                    double sample;
                                    if (this.isFloat)
                                    {
                                        if (this.bitsPerSample == 32)
                                        {
                                            sample = BitConverter.ToSingle(buffer, offset);
                                        }
                                        else
                                        {
                                            sample = BitConverter.ToDouble(buffer, offset);
                                        }
                                    }
                                    else
                                    {
                                        if (this.bitsPerSample == 16)
                                        {
                                            sample = BitConverter.ToInt16(buffer, offset) / 32768.0;
                                        }
                                        else
                                        {
                                            sample = BitConverter.ToInt32(buffer, offset) / 2147483648.0;
                                        }
                                    }

                                    frameSum += sample;
                                }

                                double frameAvg = frameSum / this.waveFormat.nChannels;
                                avgs[f] = frameAvg;
                            }

                            lock (this.lockObject)
                            {
                                for (uint f = 0; f < numFrames; f++)
                                {
                                    if (volumeLevel > 0)
                                    {
                                        this.sampleBuffer.Enqueue(avgs[f] / volumeLevel);
                                    }
                                    else
                                    {
                                        this.sampleBuffer.Enqueue(avgs[f]);
                                    }

                                    if (this.sampleBuffer.Count > MaxBufferSize)
                                    {
                                        this.sampleBuffer.Dequeue();
                                    }
                                }
                            }
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                            ArrayPool<double>.Shared.Return(avgs);
                        }
                    }
                }
                finally
                {
                    this.captureClient.ReleaseBuffer(numFrames);
                }
            }
            catch (COMException ex)
            {
                Trace.WriteLine($"AudioCapture.CaptureLoop Exception: {ex.ErrorCode:X}\n{ex.Message}");
                continue;
            }
        }
    }
}
