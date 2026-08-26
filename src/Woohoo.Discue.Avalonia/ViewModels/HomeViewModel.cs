// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Avalonia.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Woohoo.Audio.Services;
using Woohoo.Discue.Shared.Avalonia.Services;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly IMruService mruService;
    private readonly IMediaPlayerService mediaPlayerService;
    private readonly IAvaloniaBitmapCacheService bitmapCacheService;
    private readonly ILogger logger;
    private readonly IDispatcherQueue dispatcherQueue;

    public HomeViewModel(
        IDispatcherQueueService dispatcherQueueService,
        IMruService mruService,
        IMediaPlayerService mediaPlayerService,
        IAvaloniaBitmapCacheService bitmapCacheService,
        ILogger<HomeViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueueService);
        ArgumentNullException.ThrowIfNull(mruService);
        ArgumentNullException.ThrowIfNull(mediaPlayerService);
        ArgumentNullException.ThrowIfNull(bitmapCacheService);
        ArgumentNullException.ThrowIfNull(logger);

        this.RecentDiscs = [];

        this.mruService = mruService;
        this.mediaPlayerService = mediaPlayerService;
        this.bitmapCacheService = bitmapCacheService;
        this.logger = logger;
        this.dispatcherQueue = dispatcherQueueService.GetDispatcherQueue();

        this.mruService.ItemsChanged += this.OnMruItemsChanged;

        this.LoadRecentDiscs();
    }

    public ObservableCollection<HomeRecentDiscViewModel> RecentDiscs { get; }

    [ObservableProperty]
    public partial bool HasRecentDiscs { get; set; }

    [RelayCommand]
    public async Task BrowseAsync(CancellationToken cancellationToken)
    {
        WeakReferenceMessenger.Default.Send(new BrowseAlbumMessage());
    }

    public void LoadAlbum(string albumFilePath)
    {
        WeakReferenceMessenger.Default.Send(new LoadAlbumMessage { AlbumFilePath = albumFilePath });
    }

    private void LoadRecentDiscs()
    {
        this.RecentDiscs.Clear();

        foreach (var item in this.mruService.GetItems().OrderByDescending(item => item.LastUpdated))
        {
            var itemViewModel = new HomeRecentDiscViewModel(this.mruService, this.bitmapCacheService, this.logger)
            {
                AlbumFilePath = item.FilePath,
                FullAlbumTitle = item.FullAlbumTitle,
            };

            itemViewModel.AlbumArt.ImageUrl = item.AlbumArtUrl ?? string.Empty;

            this.RecentDiscs.Add(itemViewModel);
        }

        this.HasRecentDiscs = this.RecentDiscs.Count > 0;
    }

    private void OnMruItemsChanged(object? sender, EventArgs e)
    {
        _ = this.dispatcherQueue.TryEnqueue(() =>
        {
            this.LoadRecentDiscs();
        });
    }
}
