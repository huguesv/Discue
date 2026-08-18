// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Playback.Windows;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using global::Windows.Media;
using global::Windows.Media.Core;
using global::Windows.Media.Playback;
using global::Windows.Storage;
using global::Windows.Storage.Streams;
using Woohoo.Audio.Core.Media;
using Woohoo.Audio.Core.Playback;
using Woohoo.Audio.Playback.Windows.Internal.IO;
using Woohoo.Audio.Playback.Windows.Internal.Wasapi;

[SupportedOSPlatform("windows10.0.14393.0")]
public sealed class WindowsAudioPlayer : IAudioPlayer
{
    private readonly MediaPlayer mediaPlayer;
    private readonly MediaPlaybackList mediaList;
    private readonly AudioCapture capture;
    private readonly DeviceTracker tracker;
    private readonly WindowsAudioPlayerVisualization visualization;

    private readonly List<AudioPlayerTrack> playlistTracks;
    private readonly List<AudioPlayerDisc> discs;
    private readonly Dictionary<MediaPlaybackItem, Guid> mediaItemToGuidMap = [];
    private readonly Dictionary<Guid, MediaPlaybackItem> guidToMediaItemMap = [];
    private readonly Lock dataLock;

    private bool trackerStarted;
    private MediaPlaybackItem? currentPlaybackItem;
    private Guid? currentPlaybackTrackId;

    public WindowsAudioPlayer()
    {
        this.mediaPlayer = new MediaPlayer();
        this.mediaList = new MediaPlaybackList();
        this.capture = new AudioCapture();
        this.tracker = new DeviceTracker(this.capture);
        this.visualization = new WindowsAudioPlayerVisualization(this.capture);

        this.playlistTracks = [];
        this.discs = [];
        this.dataLock = new Lock();

        this.mediaPlayer.CurrentStateChanged += this.MediaPlayer_CurrentStateChanged;
        this.mediaPlayer.PlaybackSession.PositionChanged += this.PlaybackSession_PositionChanged;
        this.mediaList.CurrentItemChanged += this.MediaList_CurrentItemChanged;
    }

    public event EventHandler<EventArgs>? ActiveTrackChanged;

    public event EventHandler<EventArgs>? PlaybackStateChanged;

    public event EventHandler<EventArgs>? PlaybackPositionChanged;

    public string AudioEngineDisplayName => "Windows Media Player";

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

    public AudioPlayerStatus PlaybackState => (AudioPlayerStatus)(int)this.mediaPlayer.CurrentState;

    public TimeSpan PlaybackPosition => this.mediaPlayer.PlaybackSession.Position;

    public AudioPlayerTrack? ActiveTrack
    {
        get
        {
            lock (this.dataLock)
            {
                return this.playlistTracks.SingleOrDefault(t => t.Id == this.currentPlaybackTrackId);
            }
        }
    }

    public bool CanAdjustVolume => false;

    public int Volume
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public bool IsNextTrackEnabled
    {
        get
        {
            return this.mediaList.CurrentItemIndex < this.mediaList.Items.Count - 1;
        }
    }

    public bool IsPreviousTrackEnabled
    {
        get
        {
            return this.mediaList.CurrentItemIndex > 0;
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

    public void PreviousTrack()
    {
        if (this.mediaList.CurrentItemIndex > 0)
        {
            this.mediaList.MovePrevious();
        }
    }

    public void PlayPause()
    {
        if (this.mediaPlayer.CurrentState == MediaPlayerState.Playing)
        {
            this.mediaPlayer.Pause();
        }
        else if (this.mediaPlayer.CurrentState == MediaPlayerState.Paused)
        {
            this.mediaPlayer.Play();
        }
    }

    public void NextTrack()
    {
        if (this.mediaList.CurrentItemIndex < this.mediaList.Items.Count - 1)
        {
            this.mediaList.MoveNext();
        }
    }

    public void SeekForward(TimeSpan span)
    {
        this.mediaPlayer.PlaybackSession.Position += span;
    }

    public void SeekBackward(TimeSpan span)
    {
        this.mediaPlayer.PlaybackSession.Position -= span;
    }

    public void SeekTo(TimeSpan span)
    {
        this.mediaPlayer.PlaybackSession.Position = span;
    }

    public void Play(Guid trackId)
    {
        var mediaItem = this.guidToMediaItemMap[trackId];
        var mediaIndex = this.mediaList.Items.IndexOf(mediaItem);
        if (mediaIndex >= 0)
        {
            this.mediaList.MoveTo((uint)mediaIndex);
            this.mediaPlayer.Play();
        }
    }

    public Task ClearAsync(CancellationToken cancellationToken)
    {
        this.mediaPlayer.Pause();
        this.mediaList.Items.Clear();

        lock (this.dataLock)
        {
            this.playlistTracks.Clear();
            this.guidToMediaItemMap.Clear();
            this.mediaItemToGuidMap.Clear();
            this.discs.Clear();
            this.currentPlaybackTrackId = null;
        }

        return Task.CompletedTask;
    }

    public async Task LoadAsync(
        AudioPlayerDisc disc,
        ImmutableArray<(AudioPlayerTrack PlayerTrack, AudioPlayerTrackMetadata TrackMetadata, IAlbumTrack AlbumTrack)> tracks,
        CancellationToken cancellationToken)
    {
        this.mediaList.Items.Clear();

        lock (this.dataLock)
        {
            this.playlistTracks.Clear();
            this.guidToMediaItemMap.Clear();
            this.mediaItemToGuidMap.Clear();
            this.discs.Clear();
            this.discs.Add(disc);
        }

        foreach (var track in tracks)
        {
            var streamRef = new WinRTRandomAccessStreamReference(track.AlbumTrack, "audio/wav");
            var mediaSource = MediaSource.CreateFromStreamReference(streamRef, "audio/wav");

            var mediaItem = new MediaPlaybackItem(mediaSource);
            var props = mediaItem.GetDisplayProperties();
            props.Type = MediaPlaybackType.Music;
            props.MusicProperties.Artist = track.TrackMetadata.TrackPerformer;
            props.MusicProperties.Title = track.TrackMetadata.TrackTitle;
            props.MusicProperties.AlbumTitle = track.TrackMetadata.AlbumTitle;
            props.MusicProperties.AlbumArtist = track.TrackMetadata.AlbumPerformer;
            props.MusicProperties.AlbumTrackCount = (uint)tracks.Length;
            props.MusicProperties.TrackNumber = (uint)track.PlayerTrack.TrackNumber;
            props.Thumbnail = null;
            mediaItem.ApplyDisplayProperties(props);

            this.mediaList.Items.Add(mediaItem);

            lock (this.dataLock)
            {
                this.playlistTracks.Add(track.PlayerTrack);
                this.guidToMediaItemMap[track.PlayerTrack.Id] = mediaItem;
                this.mediaItemToGuidMap[mediaItem] = track.PlayerTrack.Id;
            }
        }

        lock (this.dataLock)
        {
            this.currentPlaybackTrackId = this.playlistTracks.FirstOrDefault()?.Id;
        }

        this.mediaPlayer.Source = this.mediaList;
        this.mediaPlayer.Play();
    }

    public Task UpdateDiscMetadataAsync(Guid discId, AudioPlayerDiscMetadata metadata, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task UpdateTrackMetadataAsync(
        Guid trackId,
        AudioPlayerTrackMetadata trackMetadata,
        Uri? originalAlbumArtUri,
        Uri? localAlbumArtUri,
        CancellationToken cancellationToken)
    {
        MediaPlaybackItem? mediaItem;

        lock (this.dataLock)
        {
            this.guidToMediaItemMap.TryGetValue(trackId, out mediaItem);
        }

        if (mediaItem is not null)
        {
            var props = mediaItem.GetDisplayProperties();
            props.MusicProperties.Artist = trackMetadata.TrackPerformer;
            props.MusicProperties.Title = trackMetadata.TrackTitle;
            props.MusicProperties.AlbumTitle = trackMetadata.AlbumTitle;
            props.MusicProperties.AlbumArtist = trackMetadata.AlbumPerformer;

            if (localAlbumArtUri is not null)
            {
                StorageFile storageFile = await StorageFile.GetFileFromPathAsync(localAlbumArtUri.LocalPath);
                props.Thumbnail = RandomAccessStreamReference.CreateFromFile(storageFile);
            }
            else if (originalAlbumArtUri is not null)
            {
                props.Thumbnail = RandomAccessStreamReference.CreateFromUri(originalAlbumArtUri);
            }

            mediaItem.ApplyDisplayProperties(props);
        }
    }

    public void Shutdown()
    {
        if (this.trackerStarted)
        {
            this.tracker.Stop();
        }
    }

    private void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
    {
        if (this.PlaybackState == AudioPlayerStatus.Playing)
        {
            if (!this.trackerStarted)
            {
                this.trackerStarted = true;
                this.tracker.Start();
            }
        }

        this.PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
    {
        this.PlaybackPositionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void MediaList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
    {
        this.currentPlaybackItem = this.mediaList.CurrentItem;

        lock (this.dataLock)
        {
            this.currentPlaybackTrackId = this.currentPlaybackItem is not null
                ? this.mediaItemToGuidMap[this.currentPlaybackItem]
                : null;
        }

        this.ActiveTrackChanged?.Invoke(this, EventArgs.Empty);
    }
}
