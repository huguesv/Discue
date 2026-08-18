// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Playback.Sdl3;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Woohoo.Audio.Core;
using Woohoo.Audio.Core.Media;
using Woohoo.Audio.Core.Playback;
using Woohoo.Sdl3;

public sealed class Sdl3AudioPlayer : IAudioPlayer
{
    public const int Channels = 2;
    public const int Frequency = 44100;
    public const int FormatSizeInBytes = 2;

    private readonly List<AudioPlayerTrack> playlistTracks;
    private readonly List<AudioPlayerDisc> discs;
    private readonly Dictionary<Guid, IAlbumTrack> guidToAlbumTrackMap = [];
    private readonly Sdl3AudioPlayerVisualizationData visualization;
    private readonly Lock dataLock;

    private readonly Lock bufferLock = new();
    private readonly byte[] buffer;

    private int activeTrackIndex;

    private SdlAudioStream? audioDeviceStream;

    private bool initialized;
    private Stream? fileStream;
    private byte[] fileData = [];
    private int fileDataIndex;
    private int fileDataLength;
    private int pendingVolume;

    public Sdl3AudioPlayer()
    {
        this.playlistTracks = [];
        this.discs = [];
        this.visualization = new Sdl3AudioPlayerVisualizationData();
        this.dataLock = new Lock();
        this.activeTrackIndex = -1;

        this.buffer = new byte[4096];
        this.pendingVolume = 100;
    }

    public event EventHandler<EventArgs>? ActiveTrackChanged;

    public event EventHandler<EventArgs>? PlaybackPositionChanged;

    public event EventHandler<EventArgs>? PlaybackStateChanged;

    public string AudioEngineDisplayName => "SDL3";

    public ImmutableArray<AudioPlayerTrack> Tracks
    {
        get
        {
            lock (this.dataLock)
            {
                return [.. this.playlistTracks];
            }
        }
    }

    public ImmutableArray<AudioPlayerDisc> Discs
    {
        get
        {
            lock (this.dataLock)
            {
                return [.. this.discs];
            }
        }
    }

    public AudioPlayerTrack? ActiveTrack
        => this.activeTrackIndex >= 0 && this.activeTrackIndex < this.playlistTracks.Count
        ? this.playlistTracks[this.activeTrackIndex]
        : null;

    public bool IsNextTrackEnabled => this.activeTrackIndex >= 0 && this.activeTrackIndex < this.playlistTracks.Count - 1;

    public bool IsPreviousTrackEnabled => this.activeTrackIndex > 0;

    public TimeSpan PlaybackPosition => TimeConversion.FromPosition(this.fileDataIndex);

    public AudioPlayerStatus PlaybackState => this.IsPlaying ? AudioPlayerStatus.Playing : AudioPlayerStatus.Paused;

    public bool IsPlaying { get; private set; }

    public bool CanAdjustVolume => true;

    public int Volume
    {
        get
        {
            if (this.initialized)
            {
                this.VerifyDeviceNotNull();
                return Math.Max(0, Math.Min(100, (int)((this.audioDeviceStream?.Gain ?? 1) * 100)));
            }
            else
            {
                return this.pendingVolume;
            }
        }

        set
        {
            if (this.initialized)
            {
                this.VerifyDeviceNotNull();
                this.audioDeviceStream.Gain = value / 100.0f;
            }
            else
            {
                this.pendingVolume = value;
            }
        }
    }

    public IAudioPlayerVisualization Visualization => this.visualization;

    public AudioPlayerDisc? FindDisc(Guid id)
    {
        lock (this.dataLock)
        {
            return this.discs.FirstOrDefault(d => d.Id == id);
        }
    }

    public AudioPlayerTrack? FindTrack(Guid id)
    {
        lock (this.dataLock)
        {
            return this.playlistTracks.SingleOrDefault(t => t.Id == id);
        }
    }

    public Task ClearAsync(CancellationToken cancellationToken)
    {
        if (this.IsPlaying)
        {
            this.Pause();
        }

        lock (this.dataLock)
        {
            this.playlistTracks.Clear();
            this.discs.Clear();
            this.guidToAlbumTrackMap.Clear();
        }

        this.SetActiveTrack(-1);

        return Task.CompletedTask;
    }

    public Task LoadAsync(
        AudioPlayerDisc disc,
        ImmutableArray<(AudioPlayerTrack PlayerTrack, AudioPlayerTrackMetadata TrackMetadata, IAlbumTrack AlbumTrack)> tracks,
        CancellationToken cancellationToken)
    {
        lock (this.dataLock)
        {
            this.playlistTracks.Clear();
            this.discs.Clear();
            this.discs.Add(disc);

            foreach (var track in tracks)
            {
                this.playlistTracks.Add(track.PlayerTrack);
                this.guidToAlbumTrackMap[track.PlayerTrack.Id] = track.AlbumTrack;
            }

            this.activeTrackIndex = 0;
        }

        if (this.playlistTracks.Count > 0)
        {
            this.Play(this.playlistTracks.First().Id);
        }
        else
        {
            this.SetActiveTrack(-1);
        }

        return Task.CompletedTask;
    }

    public void NextTrack()
    {
        if (this.activeTrackIndex < this.playlistTracks.Count - 1)
        {
            this.Play(this.activeTrackIndex + 1);
        }
    }

    public void PreviousTrack()
    {
        if (this.activeTrackIndex > 0)
        {
            this.Play(this.activeTrackIndex - 1);
        }
    }

    public void Play(Guid trackId)
    {
        this.Play(this.playlistTracks.FindIndex(t => t.Id == trackId));
    }

    public void PlayPause()
    {
        if (this.IsPlaying)
        {
            this.Pause();
        }
        else
        {
            this.Resume();
        }
    }

    public void SeekBackward(TimeSpan span)
    {
        this.VerifyDeviceNotNull();

        lock (this.bufferLock)
        {
            int offset = (int)(span.TotalSeconds * Frequency * Channels * FormatSizeInBytes);
            this.fileDataIndex = AdjustDataIndex(Math.Max(0, this.fileDataIndex - offset));
            this.fileStream?.Seek(this.fileDataIndex, SeekOrigin.Begin);
        }
    }

    public void SeekForward(TimeSpan span)
    {
        this.VerifyDeviceNotNull();

        lock (this.bufferLock)
        {
            int offset = (int)(span.TotalSeconds * Frequency * Channels * FormatSizeInBytes);
            this.fileDataIndex = AdjustDataIndex(Math.Min(this.fileDataLength, this.fileDataIndex + offset));
            this.fileStream?.Seek(this.fileDataIndex, SeekOrigin.Begin);
        }
    }

    public void SeekTo(TimeSpan span)
    {
        this.VerifyDeviceNotNull();

        lock (this.bufferLock)
        {
            int offset = (int)(span.TotalSeconds * Frequency * Channels * FormatSizeInBytes);
            this.fileDataIndex = AdjustDataIndex(offset);
            this.fileStream?.Seek(this.fileDataIndex, SeekOrigin.Begin);
        }
    }

    public Task UpdateDiscMetadataAsync(Guid discId, AudioPlayerDiscMetadata metadata, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task UpdateTrackMetadataAsync(
        Guid trackId,
        AudioPlayerTrackMetadata trackMetadata,
        Uri? originalAlbumArtUri,
        Uri? localAlbumArtUri,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Shutdown()
    {
        lock (this.bufferLock)
        {
            this.audioDeviceStream?.Pause();
            this.audioDeviceStream?.Dispose();
            this.audioDeviceStream = null;
        }

        this.IsPlaying = false;
    }

    private static int AdjustDataIndex(int index)
    {
        // Ensure proper index alignment for audio data
        return index / (Channels * FormatSizeInBytes) * (Channels * FormatSizeInBytes);
    }

    private void Initialize()
    {
        if (this.initialized)
        {
            return;
        }

        SdlAudio.Initialize();

        this.audioDeviceStream = SdlAudio.DefaultDevices.Playback.OpenStream(SdlAudioFormat.SDL_AUDIO_S16LE, Channels, Frequency, this.AudioRequested);
        this.initialized = true;
        this.Volume = this.pendingVolume;
    }

    private void SetActiveTrack(int trackIndex)
    {
        this.activeTrackIndex = trackIndex;

        this.ActiveTrackChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Play(int trackIndex)
    {
        this.SetActiveTrack(trackIndex);

        IAlbumTrack? albumTrack = null;

        lock (this.dataLock)
        {
            if (this.activeTrackIndex < 0)
            {
                return;
            }

            var activeTrack = this.playlistTracks[this.activeTrackIndex];
            albumTrack = this.guidToAlbumTrackMap[activeTrack.Id];
        }

        if (albumTrack is not null)
        {
            using var stream = albumTrack.OpenStream();

            var memoryStream = new MemoryStream(capacity: (int)stream.Length);
            stream.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            this.Play(memoryStream, albumTrack.TrackSize);
        }
    }

    private void Play(Stream fileStream, int length)
    {
        this.IsPlaying = false;

        this.Initialize();

        this.fileStream = fileStream;
        this.fileData = [];
        this.fileDataIndex = 0;
        this.fileDataLength = length;

        this.Resume();
    }

    private void Pause()
    {
        this.VerifyDeviceNotNull();

        this.audioDeviceStream.Pause();

        this.IsPlaying = false;
        this.PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Resume()
    {
        this.VerifyDeviceNotNull();

        this.audioDeviceStream.Resume();

        this.IsPlaying = true;
        this.PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AudioRequested(SdlAudioStream device, int additionalAmount, int totalAmount)
    {
        lock (this.bufferLock)
        {
            if (this.audioDeviceStream is null)
            {
                return;
            }

            int total = Math.Min(additionalAmount, this.fileDataLength - this.fileDataIndex);
            total = (total / 2) * 2;
            if (total == 0)
            {
                // End of track
                this.PlaybackPositionChanged?.Invoke(this, EventArgs.Empty);

                if (this.IsNextTrackEnabled)
                {
                    this.NextTrack();
                }
                else
                {
                    this.Pause();
                }

                return;
            }

            if (this.fileStream is not null)
            {
                this.fileStream.ReadExactly(this.buffer, 0, total);
            }
            else
            {
                Array.Copy(this.fileData, this.fileDataIndex, this.buffer, 0, total);
            }

            this.fileDataIndex += total;

            this.visualization.AnalyzeBuffer(this.buffer, total);
            this.PlaybackPositionChanged?.Invoke(this, EventArgs.Empty);
            device.PutStreamData(this.buffer, total);
        }
    }

    [MemberNotNull(nameof(audioDeviceStream))]
    private void VerifyDeviceNotNull()
    {
        if (this.audioDeviceStream is null)
        {
            throw new InvalidOperationException("Stream device not set.");
        }
    }
}
