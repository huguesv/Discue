// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Sdl3;

using System;

using Woohoo.Sdl3.Internal.Interop;

public class SdlAudioStream : IDisposable
{
    private readonly nint stream;
    private readonly StreamCallback callback;
    private bool disposedValue;

    private SdlAudioStream(nint stream, StreamCallback callback)
    {
        if (stream == nint.Zero)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        ArgumentNullException.ThrowIfNull(callback);

        this.stream = stream;
        this.callback = callback;
    }

    ~SdlAudioStream()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: false);
    }

    /// <summary>
    /// Gets or sets the frequency ratio of the audio stream.
    /// </summary>
    /// <remarks>
    /// 1.0 is normal speed. Must be between 0.01 and 100.
    /// The frequency ratio is used to adjust the rate at which input data is
    /// consumed. Changing this effectively modifies the speed and pitch of the
    /// audio. A value greater than 1.0 will play the audio faster, and at a
    /// higher pitch. A value less than 1.0 will play the audio slower, and at
    /// a lower pitch.
    /// It is safe to call this function from any thread, as it holds a
    /// stream-specific mutex while running.
    /// </remarks>
    public float FrequencyRatio
    {
        get
        {
            ObjectDisposedException.ThrowIf(this.disposedValue, this);

            return SdlAudioNativeMethods.SDL_GetAudioStreamFrequencyRatio(this.stream);
        }

        set
        {
            ObjectDisposedException.ThrowIf(this.disposedValue, this);

            if (!SdlAudioNativeMethods.SDL_SetAudioStreamFrequencyRatio(this.stream, value))
            {
                SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_SetAudioStreamFrequencyRatio));
            }
        }
    }

    /// <summary>
    /// Gets or sets the gain of the audio stream.
    /// </summary>
    /// <remarks>
    /// The gain of a stream is its volume; a larger gain means a louder
    /// output, with a gain of zero being silence.
    /// Audio streams default to a gain of 1.0f (no change in output).
    /// It is safe to call this function from any thread, as it holds a
    /// stream-specific mutex while running.
    /// </remarks>
    public float Gain
    {
        get
        {
            ObjectDisposedException.ThrowIf(this.disposedValue, this);

            return SdlAudioNativeMethods.SDL_GetAudioStreamGain(this.stream);
        }

        set
        {
            ObjectDisposedException.ThrowIf(this.disposedValue, this);

            if (!SdlAudioNativeMethods.SDL_SetAudioStreamGain(this.stream, value))
            {
                SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_SetAudioStreamGain));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether an audio device associated with a stream is paused.
    /// </summary>
    public bool IsPaused
    {
        get
        {
            ObjectDisposedException.ThrowIf(this.disposedValue, this);

            return SdlAudioNativeMethods.SDL_AudioStreamDevicePaused(this.stream);
        }
    }

    /// <summary>
    /// Pause audio playback on the audio device associated with an audio stream.
    /// </summary>
    /// <remarks>
    /// This pauses audio processing for a given device. Any bound audio
    /// streams will not progress, and no audio will be generated. Pausing one
    /// device does not prevent other unpaused devices from running.
    /// Pausing a device can be useful to halt all audio without unbinding all
    /// the audio streams.This might be useful while a game is paused, or a
    /// level is loading, etc.
    /// It is safe to call this function from any thread.
    /// </remarks>
    public void Pause()
    {
        ObjectDisposedException.ThrowIf(this.disposedValue, this);

        if (!SdlAudioNativeMethods.SDL_PauseAudioStreamDevice(this.stream))
        {
            SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_PauseAudioStreamDevice));
        }
    }

    /// <summary>
    /// Unpause audio playback on the audio device associated with an audio stream.
    /// </summary>
    /// <remarks>
    /// This unpauses audio processing for a given device that has previously been
    /// paused. Once unpaused, any bound audio streams will begin to progress
    /// again, and audio can be generated.
    /// It is safe to call this function from any thread.
    /// </remarks>
    public void Resume()
    {
        ObjectDisposedException.ThrowIf(this.disposedValue, this);

        if (!SdlAudioNativeMethods.SDL_ResumeAudioStreamDevice(this.stream))
        {
            SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_ResumeAudioStreamDevice));
        }
    }

    /// <summary>
    /// Clear any pending data in the stream.
    /// </summary>
    /// <remarks>
    /// This drops any queued data, so there will be nothing to read from the
    /// stream until more is added.
    /// It is safe to call this function from any thread.
    /// </remarks>
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(this.disposedValue, this);

        if (!SdlAudioNativeMethods.SDL_ClearAudioStream(this.stream))
        {
            SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_ClearAudioStream));
        }
    }

    /// <summary>
    /// Tell the stream that you're done sending data, and anything being
    /// buffered should be converted/resampled and made available immediately.
    /// </summary>
    /// <remarks>
    /// It is legal to add more data to a stream after flushing, but there may
    /// be audio gaps in the output. Generally this is intended to signal the
    /// end of input, so the complete output becomes available.
    /// It is safe to call this function from any thread.
    /// </remarks>
    public void Flush()
    {
        ObjectDisposedException.ThrowIf(this.disposedValue, this);

        if (!SdlAudioNativeMethods.SDL_FlushAudioStream(this.stream))
        {
            SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_FlushAudioStream));
        }
    }

    /// <summary>
    /// Lock an audio stream for serialized access.
    /// </summary>
    /// <remarks>
    /// Each audio stream has an internal mutex it uses to protect its data
    /// structures from threading conflicts.This function allows an app to
    /// lock that mutex, which could be useful if registering callbacks on
    /// this stream. One does not need to lock a stream to use in it most
    /// cases, as the stream manages this lock internally.However, this lock
    /// is held during callbacks, which may run from arbitrary threads at any
    /// time, so if an app needs to protect shared data during those
    /// callbacks, locking the stream guarantees that the callback is not
    /// running while the lock is held. As this is just a wrapper over
    /// SDL_LockMutex for an internal lock; it has all the same attributes
    /// (recursive locks are allowed, etc).
    /// It is safe to call this function from any thread.
    /// </remarks>
    public void Lock()
    {
        ObjectDisposedException.ThrowIf(this.disposedValue, this);

        if (!SdlAudioNativeMethods.SDL_LockAudioStream(this.stream))
        {
            SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_LockAudioStream));
        }
    }

    /// <summary>
    /// Unlock an audio stream for serialized access.
    /// </summary>
    /// <remarks>
    /// You should only call this from the same thread that previously called
    /// <see cref="Lock"/>.
    /// </remarks>
    public void Unlock()
    {
        ObjectDisposedException.ThrowIf(this.disposedValue, this);

        if (!SdlAudioNativeMethods.SDL_UnlockAudioStream(this.stream))
        {
            SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_UnlockAudioStream));
        }
    }

    public unsafe void PutStreamData(byte[] data, int length)
    {
        ObjectDisposedException.ThrowIf(this.disposedValue, this);

        fixed (byte* ptr = data)
        {
            if (!SdlAudioNativeMethods.SDL_PutAudioStreamData(this.stream, (nint)ptr, length))
            {
                SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_PutAudioStreamData));
            }
        }
    }

    public unsafe void PutStreamData(short[] data, int length)
    {
        ObjectDisposedException.ThrowIf(this.disposedValue, this);

        fixed (short* ptr = data)
        {
            if (!SdlAudioNativeMethods.SDL_PutAudioStreamData(this.stream, (nint)ptr, length * sizeof(short)))
            {
                SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_PutAudioStreamData));
            }
        }
    }

    public unsafe void PutStreamData(int[] data, int length)
    {
        ObjectDisposedException.ThrowIf(this.disposedValue, this);

        fixed (int* ptr = data)
        {
            if (!SdlAudioNativeMethods.SDL_PutAudioStreamData(this.stream, (nint)ptr, length * sizeof(int)))
            {
                SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_PutAudioStreamData));
            }
        }
    }

    public unsafe void PutStreamData(float[] data, int length)
    {
        ObjectDisposedException.ThrowIf(this.disposedValue, this);

        fixed (float* ptr = data)
        {
            if (!SdlAudioNativeMethods.SDL_PutAudioStreamData(this.stream, (nint)ptr, length * sizeof(float)))
            {
                SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_PutAudioStreamData));
            }
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    internal static SdlAudioStream Open(SdlAudioDevice device, SdlAudioFormat format, int channels, int frequency, SdlAudioStreamCallback audioRequest)
    {
        var spec = new SdlAudioNativeMethods.SDL_AudioSpec()
        {
            Freq = frequency,
            Format = (SdlAudioNativeMethods.SDL_AudioFormat)format,
            Channels = channels,
        };

        var callback = new StreamCallback(audioRequest);

        var stream = SdlAudioNativeMethods.SDL_OpenAudioDeviceStream(device.Id, ref spec, callback.SdlAudioCallback, 0);
        if (stream == nint.Zero)
        {
            SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_OpenAudioDeviceStream));
        }

        var deviceStream = new SdlAudioStream(stream, callback);
        callback.SetParentStreamDevice(deviceStream);

        return deviceStream;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects)
            }

            SdlAudioNativeMethods.SDL_DestroyAudioStream(this.stream);
            this.disposedValue = true;
        }
    }

    internal class StreamCallback
    {
        private readonly SdlAudioStreamCallback audioRequestedCallback;
        private SdlAudioStream? parentStreamDevice;

        public StreamCallback(SdlAudioStreamCallback audioRequestedCallback)
        {
            ArgumentNullException.ThrowIfNull(audioRequestedCallback);
            this.audioRequestedCallback = audioRequestedCallback;

            // Store the delegate that is passed to unmanaged code in a field so it remains alive
            this.SdlAudioCallback = this.SdlAudioCallbackFunction;
        }

        internal SdlAudioNativeMethods.SDL_AudioStreamCallback SdlAudioCallback { get; }

        public void SetParentStreamDevice(SdlAudioStream device)
        {
            ArgumentNullException.ThrowIfNull(device);
            this.parentStreamDevice = device;
        }

        private void SdlAudioCallbackFunction(nint userdata, nint stream, int additionalAmount, int totalAmount)
        {
            if (this.parentStreamDevice is null)
            {
                throw new InvalidOperationException("Parent stream device not set.");
            }

            this.audioRequestedCallback(this.parentStreamDevice, additionalAmount, totalAmount);
        }
    }
}
