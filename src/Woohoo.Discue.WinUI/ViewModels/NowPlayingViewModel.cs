// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Woohoo.Audio.Core.Playback;
using Woohoo.Audio.Services;
using Woohoo.Discue.Contracts.Services;

public sealed partial class NowPlayingViewModel : ObservableObject
{
    private readonly IMediaPlayerService mediaPlayerService;
    private readonly IWindowsBitmapCacheService bitmapCacheService;
    private readonly ILogger logger;
    private readonly IDispatcherQueue dispatcherQueue;

    public NowPlayingViewModel(
        IDispatcherQueueService dispatcherQueueService,
        IMediaPlayerService mediaPlayerService,
        IWindowsBitmapCacheService bitmapCacheService,
        ILogger<NowPlayingViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueueService);
        ArgumentNullException.ThrowIfNull(mediaPlayerService);
        ArgumentNullException.ThrowIfNull(bitmapCacheService);
        ArgumentNullException.ThrowIfNull(logger);

        this.mediaPlayerService = mediaPlayerService;
        this.bitmapCacheService = bitmapCacheService;
        this.logger = logger;
        this.dispatcherQueue = dispatcherQueueService.GetDispatcherQueue();

        this.AlbumArt = new CacheableImageViewModel(bitmapCacheService);

        this.mediaPlayerService.ActiveTrackChanged += this.MediaPlayerService_ActiveTrackChanged;
        this.mediaPlayerService.LyricsUpdated += this.MediaPlayerService_LyricsUpdated;
        this.mediaPlayerService.MetadataUpdated += this.MediaPlayerService_MetadataUpdated;
        this.mediaPlayerService.PlaybackPositionChanged += this.MediaPlayerService_PlaybackPositionChanged;

        var activeTrack = this.mediaPlayerService.GetActiveTrack();
        this.Load(activeTrack);
        this.LoadCurrentLyric();
        this.IsNowPlayingEnabled = activeTrack is not null;
    }

    [ObservableProperty]
    public partial bool IsNowPlayingEnabled { get; set; }

    [ObservableProperty]
    public partial CacheableImageViewModel AlbumArt { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullAlbumTitle))]
    public partial string AlbumTitle { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullAlbumTitle))]
    public partial string AlbumPerformer { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TrackTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CurrentLyric { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TimeSpan Position { get; set; } = TimeSpan.Zero;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullAlbumTitle))]
    public partial string AlbumFileName { get; set; } = string.Empty;

    public string FullAlbumTitle
    {
        get
        {
            if (!string.IsNullOrEmpty(this.AlbumPerformer) &&
                !string.IsNullOrEmpty(this.AlbumTitle))
            {
                return string.Format("{0} - {1}", this.AlbumPerformer, this.AlbumTitle);
            }
            else
            {
                return this.AlbumFileName;
            }
        }
    }

    private void MediaPlayerService_ActiveTrackChanged(object? sender, EventArgs e)
    {
        _ = this.dispatcherQueue.TryEnqueue(() =>
        {
            var activeTrack = this.mediaPlayerService.GetActiveTrack();
            if (activeTrack is not null)
            {
                this.Load(activeTrack);
                this.LoadCurrentLyric();
                this.IsNowPlayingEnabled = activeTrack is not null;
            }
        });
    }

    private void MediaPlayerService_LyricsUpdated(object? sender, AudioPlayerTrackEventArgs e)
    {
        _ = this.dispatcherQueue.TryEnqueue(() =>
        {
        });
    }

    private void MediaPlayerService_MetadataUpdated(object? sender, AudioPlayerTrackEventArgs e)
    {
        _ = this.dispatcherQueue.TryEnqueue(() =>
        {
            if (e.Track.Id == this.mediaPlayerService.GetActiveTrack()?.Id)
            {
                this.Load(e.Track);
            }
        });
    }

    private void MediaPlayerService_PlaybackPositionChanged(object? sender, EventArgs e)
    {
        _ = this.dispatcherQueue.TryEnqueue(() =>
        {
            this.Position = this.mediaPlayerService.PlaybackPosition;
            this.LoadCurrentLyric();
        });
    }

    private void Load(AudioPlayerTrack? track)
    {
        if (track is not null)
        {
            var disc = this.mediaPlayerService.FindDisc(track.DiscId);
            if (disc is not null)
            {
                this.AlbumFileName = disc.Name;
            }
            else
            {
                this.AlbumFileName = string.Empty;
            }

            var discMetadata = this.mediaPlayerService.GetDiscMetadata(track.DiscId);
            var trackMetadata = this.mediaPlayerService.GetTrackMetadata(track.Id);

            this.TrackTitle = !string.IsNullOrEmpty(trackMetadata?.TrackTitle)
                ? trackMetadata.TrackTitle
                : $"Track {track.TrackNumber:00}";

            this.AlbumTitle = trackMetadata?.AlbumTitle ?? string.Empty;
            this.AlbumPerformer = trackMetadata?.AlbumPerformer ?? string.Empty;
            this.Position = this.mediaPlayerService.PlaybackPosition;

            this.AlbumArt.ImageUrl = discMetadata?.GetPrimaryArtUrl() ?? string.Empty;
        }
    }

    private void LoadCurrentLyric()
    {
        var activeTrack = this.mediaPlayerService.GetActiveTrack();
        if (activeTrack is not null)
        {
            var lyrics = this.mediaPlayerService.GetTrackLyrics(activeTrack.Id);
            var lyric = lyrics?.GetLineAt(this.Position);
            this.CurrentLyric = lyric ?? string.Empty;
        }
    }
}
