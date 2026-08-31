// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Consolonia.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Tmds.DBus.Protocol;
using Woohoo.Audio.Core.Media;
using Woohoo.Audio.Core.Playback;
using Woohoo.Audio.Services;

public sealed partial class PlaybackViewModel : ObservableRecipient
{
    private const string PlayCaptionPlay = " ► ";
    private const string PlayCaptionPause = " ■ ";

    private readonly IMediaPlayerService mediaPlayerService;
    private readonly ILogger logger;
    private readonly IDispatcherQueue dispatcherQueue;

    public PlaybackViewModel(
        IDispatcherQueueService dispatcherQueueService,
        IMediaPlayerService mediaPlayerService,
        ILogger<PlaybackViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueueService);
        ArgumentNullException.ThrowIfNull(mediaPlayerService);
        ArgumentNullException.ThrowIfNull(logger);

        WeakReferenceMessenger.Default.Register<LoadAlbumMessage>(this, (r, m) =>
        {
            _ = this.LoadAlbumAsync(m, CancellationToken.None);
        });

        WeakReferenceMessenger.Default.Register<PlayTrackMessage>(this, (r, m) =>
        {
            this.PlayTrack(m);
        });

        this.mediaPlayerService = mediaPlayerService;
        this.logger = logger;
        this.dispatcherQueue = dispatcherQueueService.GetDispatcherQueue();

        this.mediaPlayerService.ActiveTrackChanged += this.MediaPlayerService_ActiveTrackChanged;
        this.mediaPlayerService.MetadataUpdated += this.MediaPlayerService_MetadataUpdated;
        this.mediaPlayerService.PlaybackPositionChanged += this.MediaPlayerService_PlaybackPositionChanged;
        this.mediaPlayerService.PlaybackStateChanged += this.MediaPlayerService_PlaybackStateChanged;
        this.mediaPlayerService.PlaylistUpdated += this.MediaPlayerService_PlaylistUpdated;
    }

    [ObservableProperty]
    public partial bool IsNowPlayingEnabled { get; set; }

    [ObservableProperty]
    public partial string PlayPauseCaption { get; set; } = PlayCaptionPlay;

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

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
    [NotifyPropertyChangedFor(nameof(FullAlbumTitle))]
    public partial string AlbumTitle { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullAlbumTitle))]
    public partial string AlbumPerformer { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullAlbumTitle))]
    public partial string AlbumFileName { get; set; } = string.Empty;

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

    public double CurrentTrackPositionAsDouble =>
        this.CurrentTrackDuration.TotalMilliseconds > 0
            ? this.CurrentTrackPosition.TotalMilliseconds / this.CurrentTrackDuration.TotalMilliseconds
            : 0;

    public bool CanAdjustVolume => this.mediaPlayerService.CanAdjustVolume;

    [ObservableProperty]
    public partial bool IsMuted { get; set; }

    [ObservableProperty]
    public partial int Volume { get; set; } = 100;

    [RelayCommand]
    public void Mute()
    {
        this.IsMuted = !this.IsMuted;
        this.mediaPlayerService.Volume = this.IsMuted ? 0 : this.Volume;
    }

    [RelayCommand]
    public void VolumeUp()
    {
        this.Volume = Math.Min(100, this.Volume + 5);
    }

    [RelayCommand]
    public void VolumeDown()
    {
        this.Volume = Math.Max(0, this.Volume - 5);
    }

    [RelayCommand]
    public void PreviousTrack()
    {
        this.mediaPlayerService.PreviousTrack();
    }

    [RelayCommand]
    public void PlayPause()
    {
        this.mediaPlayerService.PlayPause();
    }

    [RelayCommand]
    public void NextTrack()
    {
        this.mediaPlayerService.NextTrack();
    }

    [RelayCommand]
    public void SeekBackward()
    {
        this.mediaPlayerService.SeekBackward(TimeSpan.FromSeconds(5));
    }

    [RelayCommand]
    public void SeekForward()
    {
        this.mediaPlayerService.SeekForward(TimeSpan.FromSeconds(5));
    }

    private async Task LoadAlbumAsync(LoadAlbumMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(async () => await this.mediaPlayerService.LoadFromFileAsync(message.AlbumFilePath, cancellationToken));
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

                this.CurrentTrackDuration = track.Duration;
                this.CurrentTrackPosition = this.mediaPlayerService.PlaybackPosition;

                this.UpdatePlaybackButtonStates();
            }

            this.IsNowPlayingEnabled = track is not null;
            this.IsPlayPauseEnabled = track is not null;
            this.IsSeekBackwardEnabled = track is not null;
            this.IsSeekForwardEnabled = track is not null;
        });
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
                this.CurrentTrackPosition = this.mediaPlayerService.PlaybackPosition;
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
        this.IsPlaying = this.mediaPlayerService.PlaybackState == AudioPlayerStatus.Playing;
        this.IsNextEnabled = this.mediaPlayerService.IsNextTrackEnabled;
        this.IsPreviousEnabled = this.mediaPlayerService.IsPreviousTrackEnabled;
        this.PlayPauseCaption = this.IsPlaying ? PlayCaptionPause : PlayCaptionPlay;
    }

    partial void OnVolumeChanged(int value)
    {
        this.mediaPlayerService.Volume = value;
        this.IsMuted = false;
    }
}
