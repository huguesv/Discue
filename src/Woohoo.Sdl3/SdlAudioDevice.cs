// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Sdl3;

using Woohoo.Sdl3.Internal.Interop;

public class SdlAudioDevice
{
    public SdlAudioDevice(uint deviceId)
    {
        this.Id = deviceId;
    }

    /// <summary>
    /// Gets or sets the gain of the device.
    /// </summary>
    /// <remarks>
    /// The gain of a device is its volume; a larger gain means a louder
    /// output, with a gain of zero being silence.
    /// Audio devices default to a gain of 1.0f (no change in output).
    /// Physical devices may not have their gain changed, only logical devices,
    /// and this function will always return -1.0f when used on physical devices.
    /// It is safe to call this function from any thread.
    /// </remarks>
    public float Gain
    {
        get
        {
            return SdlAudioNativeMethods.SDL_GetAudioDeviceGain(this.Id);
        }

        set
        {
            if (!SdlAudioNativeMethods.SDL_SetAudioDeviceGain(this.Id, value))
            {
                SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_GetAudioDeviceGain));
            }
        }
    }

    /// <summary>
    /// Gets the audio format of the device.
    /// </summary>
    /// <remarks>
    /// For an opened device, this will report the format the device is currently
    /// using. If the device isn't yet opened, this will report the device's
    /// preferred format (or a reasonable default if this can't be determined).
    /// It is safe to call this function from any thread.
    /// </remarks>
    public SdlAudioFormat Format
    {
        get
        {
            if (!SdlAudioNativeMethods.SDL_GetAudioDeviceFormat(this.Id, out var spec, out var sampleFrames))
            {
                SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_GetAudioDeviceFormat));
            }

            return (SdlAudioFormat)spec.Format;
        }
    }

    /// <summary>
    /// Gets the number of channels of the device.
    /// </summary>
    /// <remarks>
    /// For an opened device, this will report the format the device is currently
    /// using. If the device isn't yet opened, this will report the device's
    /// preferred format (or a reasonable default if this can't be determined).
    /// It is safe to call this function from any thread.
    /// </remarks>
    public int Channels
    {
        get
        {
            if (!SdlAudioNativeMethods.SDL_GetAudioDeviceFormat(this.Id, out var spec, out var sampleFrames))
            {
                SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_GetAudioDeviceFormat));
            }

            return spec.Channels;
        }
    }

    /// <summary>
    /// Gets the frequency of the device.
    /// </summary>
    /// <remarks>
    /// For an opened device, this will report the format the device is currently
    /// using. If the device isn't yet opened, this will report the device's
    /// preferred format (or a reasonable default if this can't be determined).
    /// It is safe to call this function from any thread.
    /// </remarks>
    public int Frequency
    {
        get
        {
            if (!SdlAudioNativeMethods.SDL_GetAudioDeviceFormat(this.Id, out var spec, out var sampleFrames))
            {
                SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_GetAudioDeviceFormat));
            }

            return spec.Freq;
        }
    }

    /// <summary>
    /// Gets the device buffer size, in sample frames.
    /// </summary>
    public int SampleFrames
    {
        get
        {
            if (!SdlAudioNativeMethods.SDL_GetAudioDeviceFormat(this.Id, out var spec, out var sampleFrames))
            {
                SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_GetAudioDeviceFormat));
            }

            return sampleFrames;
        }
    }

    /// <summary>
    /// Gets the human-readable name of the device.
    /// </summary>
    public string Name
    {
        get
        {
            string? name = SdlAudioNativeMethods.SDL_GetAudioDeviceName(this.Id);
            if (name is null)
            {
                SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_GetAudioDeviceName));
            }

            return name!;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the device is physical
    /// (instead of logical).
    /// </summary>
    /// <remarks>
    /// It is safe to call this function from any thread.
    /// </remarks>
    public bool IsPhysical
    {
        get => SdlAudioNativeMethods.SDL_IsAudioDevicePhysical(this.Id);
    }

    /// <summary>
    /// Gets a value indicating whether the device is a playback device
    /// (instead of recording).
    /// </summary>
    /// <remarks>
    /// It is safe to call this function from any thread.
    /// </remarks>
    public bool IsPlayback
    {
        get => SdlAudioNativeMethods.SDL_IsAudioDevicePlayback(this.Id);
    }

    /// <summary>
    /// Gets a value indicating whether the device is paused.
    /// </summary>
    /// <remarks>
    /// Physical devices can not be paused or unpaused, only logical devices
    /// created through SDL_OpenAudioDevice() can be. Physical and invalid
    /// device IDs will report themselves as unpaused here.
    /// It is safe to call this function from any thread.
    /// </remarks>
    public bool IsPaused
    {
        get => SdlAudioNativeMethods.SDL_AudioDevicePaused(this.Id);
    }

    internal uint Id { get; }

    public SdlAudioStream OpenStream(SdlAudioFormat format, int channels, int frequency, SdlAudioStreamCallback audioRequest)
    {
        return SdlAudioStream.Open(this, format, channels, frequency, audioRequest);
    }

    /// <summary>
    /// Pause audio playback on the device.
    /// </summary>
    /// <remarks>
    /// This function pauses audio processing for a given device. Any bound
    /// audio streams will not progress, and no audio will be generated.
    /// Pausing one device does not prevent other unpaused devices from running.
    /// Pausing a device can be useful to halt all audio without unbinding all
    /// the audio streams.This might be useful while a game is paused, or a
    /// level is loading, etc.
    /// Physical devices can not be paused or unpaused, only logical devices
    /// created through SDL_OpenAudioDevice() can be.
    /// It is safe to call this function from any thread.
    /// </remarks>
    public void Pause()
    {
        if (!SdlAudioNativeMethods.SDL_PauseAudioDevice(this.Id))
        {
            SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_PauseAudioDevice));
        }
    }

    /// <summary>
    /// Unpause audio playback on the device.
    /// </summary>
    /// <remarks>
    /// This function unpauses audio processing for a given device that has
    /// previously been paused with SDL_PauseAudioDevice(). Once unpaused, any
    /// bound audio streams will begin to progress again, and audio can be
    /// generated.
    /// Physical devices can not be paused or unpaused, only logical devices
    /// created through SDL_OpenAudioDevice() can be.
    /// It is safe to call this function from any thread.
    /// </remarks>
    public void Resume()
    {
        if (!SdlAudioNativeMethods.SDL_ResumeAudioDevice(this.Id))
        {
            SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_ResumeAudioDevice));
        }
    }

    /// <summary>
    /// Close a previously-opened audio device.
    /// </summary>
    /// <remarks>
    /// The application should close open audio devices once they are no longer
    /// needed.
    /// This function may block briefly while pending audio data is played by
    /// the hardware, so that applications don't drop the last buffer of data
    /// they supplied if terminating immediately afterwards.
    /// It is safe to call this function from any thread.
    /// </remarks>
    public void Close()
    {
        SdlAudioNativeMethods.SDL_CloseAudioDevice(this.Id);
    }
}
