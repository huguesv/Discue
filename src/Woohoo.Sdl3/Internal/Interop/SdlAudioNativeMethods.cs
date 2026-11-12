// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Sdl3.Internal.Interop;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

/// <summary>
/// Replacement audio api definition that is more C# friendly, especially for the
/// stream function callback (compared to the definition from ppy.SDL3-CS).
/// This one allows you to use an instance method for the callback.
/// </summary>
internal static partial class SdlAudioNativeMethods
{
    private const string NativeLibName = "SDL3";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SDL_AudioStreamCallback(nint userdata, nint stream, int additional_amount, int total_amount);

    public enum SDL_AudioFormat
    {
        SDL_AUDIO_UNKNOWN = 0,
        SDL_AUDIO_U8 = 8,
        SDL_AUDIO_S8 = 32776,
        SDL_AUDIO_S16LE = 32784,
        SDL_AUDIO_S16BE = 36880,
        SDL_AUDIO_S32LE = 32800,
        SDL_AUDIO_S32BE = 36896,
        SDL_AUDIO_F32LE = 33056,
        SDL_AUDIO_F32BE = 37152,
#pragma warning disable CA1069 // Enums values should not be duplicated
        SDL_AUDIO_S16 = 32784,
        SDL_AUDIO_S32 = 32800,
        SDL_AUDIO_F32 = 33056,
#pragma warning restore CA1069 // Enums values should not be duplicated
    }

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nint SDL_malloc(nuint size);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_free(nint mem);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial IntPtr SDL_GetAudioPlaybackDevices(out int count);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial IntPtr SDL_GetAudioRecordingDevices(out int count);

    [LibraryImport(NativeLibName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalUsing(typeof(SDLOwnedStringMarshaller))]
    public static partial string SDL_GetAudioDeviceName(uint devid);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_GetAudioDeviceFormat(uint devid, out SDL_AudioSpec spec, out int sample_frames);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial float SDL_GetAudioDeviceGain(uint devid);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_SetAudioDeviceGain(uint devid, float gain);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_IsAudioDevicePhysical(uint devid);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_IsAudioDevicePlayback(uint devid);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_PauseAudioDevice(uint dev);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_ResumeAudioDevice(uint dev);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_AudioDevicePaused(uint dev);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_CloseAudioDevice(uint devid);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_PutAudioStreamData(nint stream, nint buf, int len);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int SDL_GetAudioStreamData(nint stream, nint buf, int len);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int SDL_GetAudioStreamAvailable(nint stream);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int SDL_GetAudioStreamQueued(nint stream);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_FlushAudioStream(nint stream);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_ClearAudioStream(nint stream);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial float SDL_GetAudioStreamFrequencyRatio(nint stream);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_SetAudioStreamFrequencyRatio(nint stream, float ratio);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial float SDL_GetAudioStreamGain(nint stream);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_SetAudioStreamGain(nint stream, float gain);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_PauseAudioStreamDevice(nint stream);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_ResumeAudioStreamDevice(nint stream);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_AudioStreamDevicePaused(nint stream);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_LockAudioStream(nint stream);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDLBool SDL_UnlockAudioStream(nint stream);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_DestroyAudioStream(nint stream);

    [LibraryImport(NativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial IntPtr SDL_OpenAudioDeviceStream(uint devid, ref SDL_AudioSpec spec, SDL_AudioStreamCallback callback, IntPtr userdata);

    public readonly record struct SDLBool
    {
        internal const byte FalseValue = 0;
        internal const byte TrueValue = 1;

        private readonly byte value;

        internal SDLBool(byte value)
        {
            this.value = value;
        }

        public static implicit operator bool(SDLBool b)
        {
            return b.value != FalseValue;
        }

        public static implicit operator SDLBool(bool b)
        {
            return new SDLBool(b ? TrueValue : FalseValue);
        }

        public bool Equals(SDLBool other)
        {
            return other.value == this.value;
        }

        public override int GetHashCode()
        {
            return this.value.GetHashCode();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_AudioSpec
    {
        public SDL_AudioFormat Format;
        public int Channels;
        public int Freq;
    }

    [CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedOut, typeof(SDLOwnedStringMarshaller))]
    public static unsafe class SDLOwnedStringMarshaller
    {
        /// <summary>
        /// Converts an unmanaged string to a managed version.
        /// </summary>
        /// <returns>A managed string.</returns>
        public static string? ConvertToManaged(byte* unmanaged)
            => Marshal.PtrToStringUTF8((nint)unmanaged);
    }
}
