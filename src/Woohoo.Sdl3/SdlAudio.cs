// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Sdl3;

using SDL;
using Woohoo.Sdl3.Internal.Interop;

public static class SdlAudio
{
    public static void Initialize()
    {
        SDL3.SDL_SetMainReady();

        if (!SDL.SDL3.SDL_Init(SDL.SDL_InitFlags.SDL_INIT_AUDIO))
        {
            string? errorMessage = SDL3.SDL_GetError();
            Console.WriteLine($"SDL_Init failed: {errorMessage}");

            SdlException.Throw(nameof(SDL.SDL3.SDL_Init));
        }
    }

    public static SdlAudioDevice[] GetPlaybackDevices()
    {
        return GetPlaybackDeviceIds().Select(SdlAudio.CreateDevice).ToArray();
    }

    /// <summary>
    /// Get a list of currently-connected audio playback devices.
    /// </summary>
    /// <returns>Device ids.</returns>
    public static unsafe uint[] GetPlaybackDeviceIds()
    {
        nint ids = SdlAudioNativeMethods.SDL_GetAudioPlaybackDevices(out int count);
        if (ids == 0)
        {
            SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_GetAudioPlaybackDevices));
        }

        uint[] devices = PointerToUintArray(ids, count);

        SdlAudioNativeMethods.SDL_free(ids);

        return devices;
    }

    public static SdlAudioDevice[] GetRecordingDevices()
    {
        return GetRecordingDeviceIds().Select(SdlAudio.CreateDevice).ToArray();
    }

    /// <summary>
    /// Get a list of currently-connected audio recording devices.
    /// </summary>
    /// <returns>Device ids.</returns>
    public static unsafe uint[] GetRecordingDeviceIds()
    {
        nint ids = SdlAudioNativeMethods.SDL_GetAudioRecordingDevices(out int count);
        if (ids == 0)
        {
            SdlException.Throw(nameof(SdlAudioNativeMethods.SDL_GetAudioRecordingDevices));
        }

        uint[] devices = PointerToUintArray(ids, count);

        SdlAudioNativeMethods.SDL_free(ids);

        return devices;
    }

    internal static SdlAudioDevice CreateDevice(uint deviceId)
    {
        return new SdlAudioDevice(deviceId);
    }

    private static uint[] PointerToUintArray(nint pointer, int count)
    {
        uint[] result = new uint[count];
        unsafe
        {
            uint* ptr = (uint*)new IntPtr(pointer).ToPointer();
            for (int i = 0; i < count; i++)
            {
                result[i] = ptr[i];
            }
        }

        return result;
    }

    public static class DefaultDevices
    {
        public static SdlAudioDevice Playback { get; } = CreateDevice(DefaultDeviceIds.Playback);

        public static SdlAudioDevice Recording { get; } = CreateDevice(DefaultDeviceIds.Recording);
    }

    internal static class DefaultDeviceIds
    {
        public const uint Playback = 4294967295u;

        public const uint Recording = 4294967294u;
    }
}
