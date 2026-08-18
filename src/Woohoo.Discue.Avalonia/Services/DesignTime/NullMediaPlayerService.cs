// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Avalonia.Services.DesignTime;

using System;
using System.Collections.Immutable;
using Woohoo.Audio.Core.Lyrics;
using Woohoo.Audio.Core.Playback;
using Woohoo.Audio.Services;

internal class NullMediaPlayerService : IMediaPlayerService
{
#pragma warning disable CS0067 // The event is never used
    public event EventHandler<EventArgs>? DiscLoading;

    public event EventHandler<EventArgs>? DiscLoaded;

    public event EventHandler<EventArgs>? ActiveTrackChanged;

    public event EventHandler<AudioPlayerTrackEventArgs>? LyricsUpdated;

    public event EventHandler<AudioPlayerTrackEventArgs>? MetadataUpdated;

    public event EventHandler<EventArgs>? PlaybackPositionChanged;

    public event EventHandler<EventArgs>? PlaybackStateChanged;

    public event EventHandler<EventArgs>? PlaylistUpdated;
#pragma warning restore CS0067

    public string AudioEngineDisplayName => "Null Media Player";

    public ImmutableArray<AudioPlayerDisc> Discs => [];

    public bool IsNextTrackEnabled => true;

    public bool IsPreviousTrackEnabled => true;

    public TimeSpan PlaybackPosition => TimeSpan.Zero;

    public AudioPlayerStatus PlaybackState => AudioPlayerStatus.Paused;

    public ImmutableArray<AudioPlayerTrack> PlaylistTracks => [];

    public IAudioPlayerVisualization Visualization => throw new NotImplementedException();

    public bool CanAdjustVolume => true;

    public int Volume { get; set; }

    public AudioPlayerDisc? FindDisc(Guid id)
    {
        throw new NotImplementedException();
    }

    public AudioPlayerTrack? FindTrack(Guid id)
    {
        throw new NotImplementedException();
    }

    public AudioPlayerTrack? GetActiveTrack()
    {
        return null;
    }

    public AudioPlayerDiscMetadata? GetDiscMetadata(Guid id)
    {
        throw new NotImplementedException();
    }

    public LyricsTrack? GetTrackLyrics(Guid id)
    {
        throw new NotImplementedException();
    }

    public AudioPlayerTrackMetadata? GetTrackMetadata(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task LoadFromFileAsync(string albumFilePath, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public void NextTrack()
    {
        throw new NotImplementedException();
    }

    public void Play(Guid trackId)
    {
        throw new NotImplementedException();
    }

    public void PlayPause()
    {
        throw new NotImplementedException();
    }

    public void PreviousTrack()
    {
        throw new NotImplementedException();
    }

    public void SeekBackward(TimeSpan span)
    {
        throw new NotImplementedException();
    }

    public void SeekTo(TimeSpan span)
    {
        throw new NotImplementedException();
    }

    public void SeekForward(TimeSpan span)
    {
        throw new NotImplementedException();
    }

    public void Shutdown()
    {
        throw new NotImplementedException();
    }
}
