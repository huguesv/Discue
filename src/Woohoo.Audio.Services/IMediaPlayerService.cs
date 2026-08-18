// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Services;

using System.Collections.Immutable;
using Woohoo.Audio.Core.Lyrics;
using Woohoo.Audio.Core.Playback;

public interface IMediaPlayerService
{
    event EventHandler<EventArgs>? DiscLoading;

    event EventHandler<EventArgs>? DiscLoaded;

    event EventHandler<EventArgs>? ActiveTrackChanged;

    event EventHandler<AudioPlayerTrackEventArgs>? LyricsUpdated;

    event EventHandler<AudioPlayerTrackEventArgs>? MetadataUpdated;

    event EventHandler<EventArgs>? PlaybackPositionChanged;

    event EventHandler<EventArgs>? PlaybackStateChanged;

    event EventHandler<EventArgs>? PlaylistUpdated;

    string AudioEngineDisplayName { get; }

    ImmutableArray<AudioPlayerDisc> Discs { get; }

    bool CanAdjustVolume { get; }

    int Volume { get; set; }

    bool IsNextTrackEnabled { get; }

    bool IsPreviousTrackEnabled { get; }

    TimeSpan PlaybackPosition { get; }

    AudioPlayerStatus PlaybackState { get; }

    ImmutableArray<AudioPlayerTrack> PlaylistTracks { get; }

    IAudioPlayerVisualization Visualization { get; }

    AudioPlayerTrack? GetActiveTrack();

    AudioPlayerDisc? FindDisc(Guid id);

    AudioPlayerTrack? FindTrack(Guid id);

    AudioPlayerDiscMetadata? GetDiscMetadata(Guid id);

    AudioPlayerTrackMetadata? GetTrackMetadata(Guid id);

    LyricsTrack? GetTrackLyrics(Guid id);

    Task LoadFromFileAsync(string albumFilePath, CancellationToken cancellationToken);

    void NextTrack();

    void Play(Guid trackId);

    void PlayPause();

    void PreviousTrack();

    void SeekForward(TimeSpan span);

    void SeekBackward(TimeSpan span);

    void SeekTo(TimeSpan span);

    void Shutdown();
}
