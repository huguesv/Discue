// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Core.Playback;

using System.Collections.Immutable;
using Woohoo.Audio.Core.Media;

public interface IAudioPlayer
{
    event EventHandler<EventArgs>? ActiveTrackChanged;

    event EventHandler<EventArgs>? PlaybackPositionChanged;

    event EventHandler<EventArgs>? PlaybackStateChanged;

    string AudioEngineDisplayName { get; }

    AudioPlayerTrack? ActiveTrack { get; }

    ImmutableArray<AudioPlayerDisc> Discs { get; }

    bool CanAdjustVolume { get; }

    int Volume { get; set; }

    bool IsNextTrackEnabled { get; }

    bool IsPreviousTrackEnabled { get; }

    TimeSpan PlaybackPosition { get; }

    AudioPlayerStatus PlaybackState { get; }

    ImmutableArray<AudioPlayerTrack> Tracks { get; }

    IAudioPlayerVisualization Visualization { get; }

    void Initialize();

    AudioPlayerTrack? FindTrack(Guid id);

    AudioPlayerDisc? FindDisc(Guid id);

    Task ClearAsync(CancellationToken cancellationToken);

    Task LoadAsync(
        AudioPlayerDisc disc,
        ImmutableArray<(AudioPlayerTrack PlayerTrack, AudioPlayerTrackMetadata TrackMetadata, IAlbumTrack AlbumTrack)> tracks,
        CancellationToken cancellationToken);

    void NextTrack();

    void Play(Guid trackId);

    void PlayPause();

    void PreviousTrack();

    void SeekBackward(TimeSpan span);

    void SeekForward(TimeSpan span);

    void SeekTo(TimeSpan span);

    void Shutdown();

    Task UpdateDiscMetadataAsync(Guid discId, AudioPlayerDiscMetadata metadata, CancellationToken cancellationToken);

    Task UpdateTrackMetadataAsync(
        Guid trackId,
        AudioPlayerTrackMetadata trackMetadata,
        Uri? originalAlbumArtUri,
        Uri? localAlbumArtUri,
        CancellationToken cancellationToken);
}
