// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Playback.Android;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using global::Android.Media;
using Woohoo.Audio.Core;
using Woohoo.Audio.Core.Media;
using Woohoo.Audio.Core.Playback;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "TODO")]
public sealed class AndroidAudioPlayer : IAudioPlayer
{
    public const int Channels = 2;
    public const int Frequency = 44100;
    public const int FormatSizeInBytes = 2;

    private readonly List<AudioPlayerTrack> playlistTracks;
    private readonly List<AudioPlayerDisc> discs;
    private readonly Dictionary<Guid, IAlbumTrack> guidToAlbumTrackMap = [];
    private readonly AndroidAudioPlayerVisualizationData visualization;
    private readonly Lock dataLock;

    private readonly Lock streamLock = new();

    private int activeTrackIndex;

    private AudioTrack? audioTrack;

    private bool initialized;
    private System.IO.Stream? fileStream;
    private byte[] fileData = [];
    private int fileDataIndex;
    private int fileDataLength;

    private CancellationTokenSource? playbackTaskCancellationTokenSource;
    private Task? playbackTask;

    public AndroidAudioPlayer()
    {
        this.playlistTracks = [];
        this.discs = [];
        this.visualization = new AndroidAudioPlayerVisualizationData();
        this.dataLock = new Lock();
        this.activeTrackIndex = -1;
    }

    public event EventHandler<EventArgs>? ActiveTrackChanged;

    public event EventHandler<EventArgs>? PlaybackPositionChanged;

    public event EventHandler<EventArgs>? PlaybackStateChanged;

    public string AudioEngineDisplayName => "Android";

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

    public bool CanAdjustVolume => false;

    public int Volume
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public IAudioPlayerVisualization Visualization => this.visualization;

    public void Initialize()
    {
        if (this.initialized)
        {
            return;
        }

        int minBufferSize = AudioTrack.GetMinBufferSize(Frequency, ChannelOut.Stereo, Encoding.Pcm16bit);

        var attributes = new AudioAttributes.Builder()
            .SetUsage(AudioUsageKind.Media)!
            .SetContentType(AudioContentType.Music)!
            .Build();
        if (attributes is null)
        {
            throw new InvalidOperationException("Could not initialize android audio.");
        }

        var format = new AudioFormat.Builder()!
            .SetSampleRate(Frequency)!
            .SetEncoding(Encoding.Pcm16bit)!
            .SetChannelMask(ChannelOut.Stereo)!
            .Build();
        if (format is null)
        {
            throw new InvalidOperationException("Could not initialize android audio.");
        }

        this.audioTrack = new AudioTrack.Builder()!
            .SetAudioAttributes(attributes)!
            .SetAudioFormat(format)!
            .SetBufferSizeInBytes(minBufferSize * 2)
            .SetTransferMode(AudioTrackMode.Stream)!
            .Build();
        if (this.audioTrack is null)
        {
            throw new InvalidOperationException("Could not initialize android audio.");
        }

        this.initialized = true;

        this.playbackTaskCancellationTokenSource = new CancellationTokenSource();
        this.playbackTask = Task.Run(() => this.StreamAudioLoop(minBufferSize, this.playbackTaskCancellationTokenSource.Token), this.playbackTaskCancellationTokenSource.Token);
    }

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

    public Task LoadAsync(AudioPlayerDisc disc, ImmutableArray<(AudioPlayerTrack PlayerTrack, AudioPlayerTrackMetadata TrackMetadata, IAlbumTrack AlbumTrack)> tracks, CancellationToken cancellationToken)
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

        lock (this.streamLock)
        {
            int offset = (int)(span.TotalSeconds * Frequency * Channels * FormatSizeInBytes);
            this.fileDataIndex = AdjustDataIndex(Math.Max(0, this.fileDataIndex - offset));
            this.fileStream?.Seek(this.fileDataIndex, SeekOrigin.Begin);
        }
    }

    public void SeekForward(TimeSpan span)
    {
        this.VerifyDeviceNotNull();

        lock (this.streamLock)
        {
            int offset = (int)(span.TotalSeconds * Frequency * Channels * FormatSizeInBytes);
            this.fileDataIndex = AdjustDataIndex(Math.Min(this.fileDataLength, this.fileDataIndex + offset));
            this.fileStream?.Seek(this.fileDataIndex, SeekOrigin.Begin);
        }
    }

    public void SeekTo(TimeSpan span)
    {
        this.VerifyDeviceNotNull();

        lock (this.streamLock)
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

    public Task UpdateTrackMetadataAsync(Guid trackId, AudioPlayerTrackMetadata trackMetadata, Uri? originalAlbumArtUri, Uri? localAlbumArtUri, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Shutdown()
    {
        lock (this.streamLock)
        {
            this.audioTrack?.Pause();
            this.audioTrack?.Dispose();
            this.audioTrack = null;
        }

        this.IsPlaying = false;
    }

    private static int AdjustDataIndex(int index)
    {
        // Ensure proper index alignment for audio data
        return index / (Channels * FormatSizeInBytes) * (Channels * FormatSizeInBytes);
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

    private void Play(System.IO.Stream fileStream, int length)
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

        this.audioTrack.Pause();

        this.IsPlaying = false;
        this.PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Resume()
    {
        this.VerifyDeviceNotNull();

        this.audioTrack.Play();

        this.IsPlaying = true;
        this.PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void StreamAudioLoop(int bufferSize, CancellationToken token)
    {
        byte[] buffer = new byte[bufferSize];

        try
        {
            while (!token.IsCancellationRequested)
            {
                // Fire callback to pull next slice of PCM data from consumer
                int bytesProvided = this.AudioRequested(buffer, buffer.Length);

                if (bytesProvided <= 0)
                {
                    // End of track
                    Thread.Sleep(50);
                    continue;
                }

                lock (this.streamLock)
                {
                    if (this.audioTrack == null || this.audioTrack.PlayState != PlayState.Playing)
                    {
                        break;
                    }

                    // WriteMode.Blocking pauses this background thread until
                    // Android's internal buffer has space for the new data.
                    this.audioTrack.Write(buffer, 0, bytesProvided, WriteMode.Blocking);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal exit on cancellation
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Audio streaming error: {ex.Message}");
        }
        finally
        {
            lock (this.streamLock)
            {
            }
        }
    }

    private int AudioRequested(byte[] buffer, int amount)
    {
        lock (this.streamLock)
        {
            if (this.audioTrack is null)
            {
                return 0;
            }

            int total = Math.Min(amount, this.fileDataLength - this.fileDataIndex);
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

                return 0;
            }

            if (this.fileStream is not null)
            {
                this.fileStream.ReadExactly(buffer, 0, total);
            }
            else
            {
                Array.Copy(this.fileData, this.fileDataIndex, buffer, 0, total);
            }

            this.fileDataIndex += total;

            this.visualization.AnalyzeBuffer(buffer, total);
            this.PlaybackPositionChanged?.Invoke(this, EventArgs.Empty);

            return total;
        }
    }

    [MemberNotNull(nameof(audioTrack))]
    private void VerifyDeviceNotNull()
    {
        if (this.audioTrack is null)
        {
            throw new InvalidOperationException("Stream device not set.");
        }
    }
}
