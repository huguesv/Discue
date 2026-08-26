// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Avalonia.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using global::Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using Woohoo.Audio.Core.Media;
using Woohoo.Audio.Services;
using Woohoo.Discue.Avalonia.Services;
using Woohoo.Discue.Shared.Avalonia.Services;

public partial class MainViewModel : ObservableObject
{
    private readonly IDispatcherQueue dispatcherQueue;
    private readonly IFilePickerService filePickerService;
    private readonly IMediaPlayerService mediaPlayerService;
    private readonly ILocalSettingsService localSettingsService;
    private readonly ILogger logger;

    private string lastBrowseFolder = string.Empty;

    public MainViewModel(
        IDispatcherQueueService dispatcherQueueService,
        IFilePickerService filePickerService,
        IPowerManagementService powerManagementService,
        IMediaPlayerService mediaPlayerService,
        ILocalSettingsService localSettingsService,
        HomeViewModel homeViewModel,
        PlaybackViewModel playbackViewModel,
        NowPlayingViewModel nowPlayingViewModel,
        PlaylistViewModel playlistViewModel,
        LyricsViewModel lyricsViewModel,
        SettingsViewModel settingsViewModel,
        ILogger<MainViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueueService);
        ArgumentNullException.ThrowIfNull(filePickerService);
        ArgumentNullException.ThrowIfNull(powerManagementService);
        ArgumentNullException.ThrowIfNull(mediaPlayerService);
        ArgumentNullException.ThrowIfNull(localSettingsService);
        ArgumentNullException.ThrowIfNull(homeViewModel);
        ArgumentNullException.ThrowIfNull(playbackViewModel);
        ArgumentNullException.ThrowIfNull(nowPlayingViewModel);
        ArgumentNullException.ThrowIfNull(playlistViewModel);
        ArgumentNullException.ThrowIfNull(lyricsViewModel);
        ArgumentNullException.ThrowIfNull(settingsViewModel);
        ArgumentNullException.ThrowIfNull(logger);

        this.filePickerService = filePickerService;
        this.mediaPlayerService = mediaPlayerService;
        this.localSettingsService = localSettingsService;
        this.Home = homeViewModel;
        this.Playback = playbackViewModel;
        this.NowPlaying = nowPlayingViewModel;
        this.Playlist = playlistViewModel;
        this.Lyrics = lyricsViewModel;
        this.Settings = settingsViewModel;
        this.logger = logger;
        this.dispatcherQueue = dispatcherQueueService.GetDispatcherQueue();

        this.View = ViewType.Home;

        this.mediaPlayerService.ActiveTrackChanged += this.MediaPlayerService_ActiveTrackChanged;
    }

    public HomeViewModel Home { get; }

    public PlaybackViewModel Playback { get; }

    public NowPlayingViewModel NowPlaying { get; }

    public PlaylistViewModel Playlist { get; }

    public LyricsViewModel Lyrics { get; }

    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    public partial bool IsAlbumLoaded { get; set; }

    [ObservableProperty]
    public partial ViewType View { get; set; }

    public async Task BrowseAsync(IStorageProvider storageProvider, CancellationToken cancellationToken)
    {
        _ = this.dispatcherQueue.TryEnqueue(async () =>
        {
            var filePaths = await this.filePickerService.GetFilePathsAsync(
                storageProvider,
                this.lastBrowseFolder,
                Localized.BrowseDialogTitle,
                allowMultiple: false,
                [
                    new("Disc Image Files") { Patterns = ["*.cue", "*.zip", "*.chd"] },
                new("All Files") { Patterns = ["*.*"] },
                ]);

            if (filePaths.Length > 0)
            {
                var filePath = filePaths[0];

                this.lastBrowseFolder = Path.GetDirectoryName(filePath) ?? string.Empty;
                this.localSettingsService.SaveSetting(KnownSettingKeys.LastBrowseFolder, this.lastBrowseFolder);

                await this.OpenFileAsync(filePath, cancellationToken);
            }
        });
    }

    public async Task OpenFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(async () => await this.mediaPlayerService.LoadFromFileAsync(filePath, cancellationToken));
            this.View = ViewType.NowPlaying;
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

    [RelayCommand]
    public void ChangeView(ViewType view)
    {
        this.View = view;
    }

    private void MediaPlayerService_ActiveTrackChanged(object? sender, EventArgs e)
    {
        _ = this.dispatcherQueue.TryEnqueue(() =>
        {
            var activeTrack = this.mediaPlayerService.GetActiveTrack();

            this.IsAlbumLoaded |= activeTrack is not null;
        });
    }
}
