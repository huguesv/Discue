// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Woohoo.Audio.Core.Media;
using Woohoo.Audio.Core.Playback;
using Woohoo.Audio.Services;
using Woohoo.Discue.Contracts.Services;

public sealed partial class PlaybackViewModel : ObservableRecipient
{
    private const string PlayIconBold = "\uF5B0";
    private const string PlayIcon = "\uE768";
    private const string PauseIconBold = "\uF8AE";
    private const string PauseIcon = "\uE769";

    private readonly IMediaPlayerService mediaPlayerService;
    private readonly IWindowsBitmapCacheService bitmapCacheService;
    private readonly ILogger logger;
    private readonly IDispatcherQueue dispatcherQueue;

    private bool ignorePositionChanges = false;

    public PlaybackViewModel(
        IDispatcherQueueService dispatcherQueueService,
        IMediaPlayerService mediaPlayerService,
        IWindowsBitmapCacheService bitmapCacheService,
        ILogger<PlaybackViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueueService);
        ArgumentNullException.ThrowIfNull(mediaPlayerService);
        ArgumentNullException.ThrowIfNull(bitmapCacheService);
        ArgumentNullException.ThrowIfNull(logger);

        WeakReferenceMessenger.Default.Register<LoadAlbumMessage>(this, (r, m) =>
        {
            _ = this.LoadAlbumAsync(m, CancellationToken.None);
        });

        WeakReferenceMessenger.Default.Register<PlayTrackMessage>(this, (r, m) =>
        {
            this.PlayTrack(m);
        });

        WeakReferenceMessenger.Default.Register<MediaErrorMessage>(this, (r, m) =>
        {
            this.ShowError(m);
        });

        this.mediaPlayerService = mediaPlayerService;
        this.bitmapCacheService = bitmapCacheService;
        this.logger = logger;
        this.dispatcherQueue = dispatcherQueueService.GetDispatcherQueue();

        this.AlbumArt = new CacheableImageViewModel(bitmapCacheService);

        this.mediaPlayerService.ActiveTrackChanged += this.MediaPlayerService_ActiveTrackChanged;
        this.mediaPlayerService.MetadataUpdated += this.MediaPlayerService_MetadataUpdated;
        this.mediaPlayerService.PlaybackPositionChanged += this.MediaPlayerService_PlaybackPositionChanged;
        this.mediaPlayerService.PlaybackStateChanged += this.MediaPlayerService_PlaybackStateChanged;
        this.mediaPlayerService.PlaylistUpdated += this.MediaPlayerService_PlaylistUpdated;
        this.mediaPlayerService.DiscLoading += this.MediaPlayerService_DiscLoading;
        this.mediaPlayerService.DiscLoaded += this.MediaPlayerService_DiscLoaded;
    }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsNowPlayingEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsSeekEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsPlayPauseEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsNextEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsPreviousEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsSeekForwardEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsSeekBackwardEnabled { get; set; }

    [ObservableProperty]
    public partial string PlayPauseGlyph { get; set; } = PlayIconBold;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullAlbumTitle))]
    public partial string AlbumTitle { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullAlbumTitle))]
    public partial string AlbumPerformer { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullAlbumTitle))]
    public partial string AlbumFileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial CacheableImageViewModel AlbumArt { get; set; }

    [ObservableProperty]
    public partial string CurrentTrackTitle { get; set; } = string.Empty;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentTrackDurationAsSeconds))]
    [NotifyPropertyChangedFor(nameof(CurrentTrackDurationAsMs))]
    [NotifyPropertyChangedFor(nameof(CurrentTrackDurationAsText))]
    [NotifyPropertyChangedFor(nameof(CurrentTrackPositionAsDouble))]
    public partial TimeSpan CurrentTrackDuration { get; set; } = TimeSpan.Zero;

    public int CurrentTrackDurationAsSeconds => (int)this.CurrentTrackDuration.TotalSeconds;

    public int CurrentTrackDurationAsMs => (int)this.CurrentTrackDuration.TotalMilliseconds;

    public string CurrentTrackDurationAsText => this.CurrentTrackDuration.ToString("mm\\:ss");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentTrackPositionAsSeconds))]
    [NotifyPropertyChangedFor(nameof(CurrentTrackPositionAsMs))]
    [NotifyPropertyChangedFor(nameof(CurrentTrackPositionAsText))]
    [NotifyPropertyChangedFor(nameof(CurrentTrackPositionAsDouble))]
    public partial TimeSpan CurrentTrackPosition { get; set; } = TimeSpan.Zero;

    public int CurrentTrackPositionAsSeconds => (int)this.CurrentTrackPosition.TotalSeconds;

    public int CurrentTrackPositionAsMs => (int)this.CurrentTrackPosition.TotalMilliseconds;

    public string CurrentTrackPositionAsText => this.CurrentTrackPosition.ToString("mm\\:ss");

    public double CurrentTrackPositionAsDouble
    {
        get
        {
            return this.CurrentTrackDuration.TotalMilliseconds > 0
                ? this.CurrentTrackPosition.TotalMilliseconds / this.CurrentTrackDuration.TotalMilliseconds
                : 0;
        }

        set
        {
            if (this.ignorePositionChanges)
            {
                return;
            }

            var timeSpan = TimeSpan.FromMilliseconds(value * this.CurrentTrackDuration.TotalMilliseconds);
            this.SeekTo(timeSpan);
        }
    }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsErrorVisible { get; set; }

    public void PreviousTrack()
    {
        this.mediaPlayerService.PreviousTrack();
    }

    public void PlayPause()
    {
        this.mediaPlayerService.PlayPause();
    }

    public void NextTrack()
    {
        this.mediaPlayerService.NextTrack();
    }

    public void SeekBackward()
    {
        this.mediaPlayerService.SeekBackward(TimeSpan.FromSeconds(5));
    }

    public void SeekForward()
    {
        this.mediaPlayerService.SeekForward(TimeSpan.FromSeconds(5));
    }

    public void SeekTo(TimeSpan value)
    {
        this.mediaPlayerService.SeekTo(value);
    }

    private async Task LoadAlbumAsync(LoadAlbumMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(async () => await this.mediaPlayerService.LoadFromFileAsync(message.AlbumFilePath, cancellationToken));
            WeakReferenceMessenger.Default.Send(new ViewChangeMessage { ViewName = typeof(NowPlayingViewModel).FullName! });
        }
        catch (MediaLoadException ex)
        {
            WeakReferenceMessenger.Default.Send(new MediaErrorMessage { Text = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            WeakReferenceMessenger.Default.Send(new MediaErrorMessage { Text = ex.Message });
        }
    }

    private void PlayTrack(PlayTrackMessage message)
    {
        this.mediaPlayerService.Play(message.TrackId);
    }

    private void ShowError(MediaErrorMessage m)
    {
        _ = this.dispatcherQueue.TryEnqueue(() =>
        {
            this.ErrorMessage = m.Text;
            this.IsErrorVisible = true;
            this.IsLoading = false;
        });
    }

    private void MediaPlayerService_DiscLoading(object? sender, EventArgs e)
    {
        _ = this.dispatcherQueue.TryEnqueue(() =>
        {
            this.IsLoading = true;
        });
    }

    private void MediaPlayerService_DiscLoaded(object? sender, EventArgs e)
    {
        _ = this.dispatcherQueue.TryEnqueue(() =>
        {
            this.IsLoading = false;
        });
    }

    private void MediaPlayerService_ActiveTrackChanged(object? sender, EventArgs e)
    {
        _ = this.dispatcherQueue.TryEnqueue(() =>
        {
            var track = this.mediaPlayerService.GetActiveTrack();
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

                this.AlbumTitle = trackMetadata?.AlbumTitle ?? string.Empty;
                this.AlbumPerformer = trackMetadata?.AlbumPerformer ?? string.Empty;

                this.CurrentTrackTitle = !string.IsNullOrEmpty(trackMetadata?.TrackTitle)
                    ? trackMetadata.TrackTitle
                    : $"Track {track.TrackNumber:00}";

                this.AlbumArt.ImageUrl = discMetadata?.GetPrimaryArtUrl() ?? string.Empty;

                this.UpdateTrackPositionAndDuration(this.mediaPlayerService.PlaybackPosition, track.Duration);
                this.UpdatePlaybackButtonStates();
            }
            else
            {
                this.AlbumFileName = string.Empty;
                this.AlbumTitle = string.Empty;
                this.AlbumPerformer = string.Empty;
                this.CurrentTrackTitle = string.Empty;
                this.AlbumArt.ImageUrl = string.Empty;
                this.UpdateTrackPositionAndDuration(TimeSpan.Zero, TimeSpan.Zero);
                this.UpdatePlaybackButtonStates();
            }

            this.IsNowPlayingEnabled = track is not null;
            this.IsPlayPauseEnabled = track is not null;
            this.IsSeekBackwardEnabled = track is not null;
            this.IsSeekForwardEnabled = track is not null;

            this.ErrorMessage = string.Empty;
            this.IsErrorVisible = false;
        });
    }

    private void UpdateTrackPositionAndDuration(TimeSpan position, TimeSpan duration)
    {
        this.ignorePositionChanges = true;
        try
        {
            this.CurrentTrackDuration = duration;
            this.CurrentTrackPosition = position;
        }
        finally
        {
            this.ignorePositionChanges = false;
        }
    }

    private void UpdateTrackPosition(TimeSpan position)
    {
        this.ignorePositionChanges = true;
        try
        {
            this.CurrentTrackPosition = position;
        }
        finally
        {
            this.ignorePositionChanges = false;
        }
    }

    private void MediaPlayerService_MetadataUpdated(object? sender, AudioPlayerTrackEventArgs e)
    {
        _ = this.dispatcherQueue.TryEnqueue(() =>
        {
            if (e.Track.Id == this.mediaPlayerService.GetActiveTrack()?.Id)
            {
                var track = e.Track;

                var discMetadata = this.mediaPlayerService.GetDiscMetadata(track.DiscId);
                var trackMetadata = this.mediaPlayerService.GetTrackMetadata(track.Id);

                this.AlbumTitle = trackMetadata?.AlbumTitle ?? string.Empty;
                this.AlbumPerformer = trackMetadata?.AlbumPerformer ?? string.Empty;
                this.CurrentTrackTitle = !string.IsNullOrEmpty(trackMetadata?.TrackTitle)
                    ? trackMetadata.TrackTitle
                    : $"Track {track.TrackNumber:00}";

                var disc = this.mediaPlayerService.FindDisc(e.Track.DiscId);
                this.AlbumArt.ImageUrl = discMetadata?.GetPrimaryArtUrl() ?? string.Empty;
            }
        });
    }

    private void MediaPlayerService_PlaybackPositionChanged(object? sender, EventArgs e)
    {
        _ = this.dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                // Sometimes we get an exception here while quitting the app
                // while music is playing.
                this.UpdateTrackPosition(this.mediaPlayerService.PlaybackPosition);
            }
            catch
            {
            }
        });
    }

    private void MediaPlayerService_PlaybackStateChanged(object? sender, EventArgs e)
    {
        _ = this.dispatcherQueue.TryEnqueue(() =>
        {
            this.UpdatePlaybackButtonStates();
        });
    }

    private void MediaPlayerService_PlaylistUpdated(object? sender, EventArgs e)
    {
        _ = this.dispatcherQueue.TryEnqueue(() =>
        {
            this.UpdatePlaybackButtonStates();
        });
    }

    private void UpdatePlaybackButtonStates()
    {
        this.IsNextEnabled = this.mediaPlayerService.IsNextTrackEnabled;
        this.IsPreviousEnabled = this.mediaPlayerService.IsPreviousTrackEnabled;
        this.PlayPauseGlyph = this.mediaPlayerService.PlaybackState == AudioPlayerStatus.Playing
            ? PauseIconBold
            : PlayIconBold;
    }

    partial void OnIsNowPlayingEnabledChanged(bool value)
    {
        this.IsSeekEnabled = value;
    }
}
