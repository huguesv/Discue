// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Avalonia.Views;

using CommunityToolkit.Mvvm.Messaging;
using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Interactivity;
using global::Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Woohoo.Discue.Avalonia.Helpers;
using Woohoo.Discue.Avalonia.ViewModels;

public partial class MainWindow : Window
{
    private bool isNavigatingProgrammatically;
    private bool isKeyboardSelecting;

    public MainWindow()
    {
        this.InitializeComponent();

        this.AddHandler(DragDrop.DropEvent, this.OnDrop);

        this.NavPage.Popped += (_, _) => this.SyncSidebarSelection();

        // Attach KeyDown with RoutingStrategies.Tunnel and handledEventsToo = true
        this.TopNavList.AddHandler(KeyDownEvent, this.OnNavKeyDown, RoutingStrategies.Tunnel, true);
        this.BottomNavList.AddHandler(KeyDownEvent, this.OnNavKeyDown, RoutingStrategies.Tunnel, true);

        // Set Root Page
        this.TopNavList.SelectedItem = this.HomeNavItem;
        this.NavPage.Content = new HomePage();

        WindowStateHelper.TrackWindow(
            this,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Woohoo.Discue.Avalonia", "WindowSettings.json"));

        WeakReferenceMessenger.Default.Register<BrowseAlbumMessage>(this, (r, m) =>
        {
            _ = this.BrowseAsync(CancellationToken.None);
        });
    }

    public void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            var files = e.DataTransfer.Items ?? Array.Empty<IDataTransferItem>();
            var filePaths = new List<string>();

            foreach (var item in files)
            {
                var raw = item.TryGetRaw(DataFormat.File);
                if (raw is IStorageFile file)
                {
                    string? path = file.TryGetLocalPath();
                    if (!string.IsNullOrEmpty(path))
                    {
                        filePaths.Add(path);
                    }
                }
                else if (raw is IStorageFolder folder)
                {
                    string? path = folder.TryGetLocalPath();
                    if (!string.IsNullOrEmpty(path))
                    {
                        filePaths.AddRange(Directory.GetFiles(path, "*", SearchOption.AllDirectories));
                    }
                }
            }

            if (filePaths.Count > 0)
            {
                _ = (this.DataContext as MainViewModel)?.OpenFileAsync(filePaths[0], CancellationToken.None);
            }
        }
    }

    private void Window_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.MediaPlayPause)
        {
            if (this.DataContext is MainViewModel vm)
            {
                vm.Playback.PlayPauseCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.MediaPreviousTrack)
        {
            if (this.DataContext is MainViewModel vm)
            {
                vm.Playback.PreviousTrackCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.MediaNextTrack)
        {
            if (this.DataContext is MainViewModel vm)
            {
                vm.Playback.NextTrackCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.F11)
        {
            if (this.WindowState == WindowState.FullScreen)
            {
                this.WindowState = WindowState.Normal;
                e.Handled = true;
            }
            else
            {
                this.WindowState = WindowState.FullScreen;
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            if (this.WindowState == WindowState.FullScreen)
            {
                this.WindowState = WindowState.Normal;
                e.Handled = true;
            }
        }
    }

    private void OnPaneToggleClicked(object? sender, RoutedEventArgs e)
    {
        this.NavSplitView.IsPaneOpen = !this.NavSplitView.IsPaneOpen;
    }

    private void OnNavKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not ListBox sourceList)
        {
            return;
        }

        if (e.Key == Key.Down)
        {
            this.isKeyboardSelecting = true;

            if (sourceList == this.TopNavList && sourceList.SelectedIndex == sourceList.ItemCount - 1)
            {
                e.Handled = true;
                this.BottomNavList.Focus();
                this.BottomNavList.SelectedIndex = 0;
            }
        }
        else if (e.Key == Key.Up)
        {
            this.isKeyboardSelecting = true;

            if (sourceList == this.BottomNavList && sourceList.SelectedIndex == 0)
            {
                e.Handled = true;
                this.TopNavList.Focus();
                this.TopNavList.SelectedIndex = this.TopNavList.ItemCount - 1;
            }
        }
        else if (e.Key == Key.Enter)
        {
            this.isKeyboardSelecting = false;
            this.UpdateMutualExclusion(sourceList);
            this.NavigateToSelectedPage(sourceList);
            e.Handled = true;
        }
    }

    private void OnNavSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox sourceList || this.isNavigatingProgrammatically)
        {
            return;
        }

        this.UpdateMutualExclusion(sourceList);

        if (!this.isKeyboardSelecting)
        {
            this.NavigateToSelectedPage(sourceList);
        }
    }

    private void UpdateMutualExclusion(ListBox activeList)
    {
        if (this.isNavigatingProgrammatically)
        {
            return;
        }

        this.isNavigatingProgrammatically = true;

        if (activeList == this.TopNavList)
        {
            this.BottomNavList.SelectedItem = null;
        }
        else if (activeList == this.BottomNavList)
        {
            this.TopNavList.SelectedItem = null;
        }

        this.isNavigatingProgrammatically = false;
    }

    private async void NavigateToSelectedPage(ListBox sourceList)
    {
        if (this.NavPage is null || sourceList.SelectedItem is null)
        {
            return;
        }

        if (sourceList.SelectedItem == this.HomeNavItem && this.NavPage.CurrentPage is not HomePage)
        {
            await this.NavPage.PushAsync(new HomePage());
        }
        else if (sourceList.SelectedItem == this.NowPlayingNavItem && this.NavPage.CurrentPage is not NowPlayingPage)
        {
            await this.NavPage.PushAsync(new NowPlayingPage());
        }
        else if (sourceList.SelectedItem == this.VisualizationNavItem && this.NavPage.CurrentPage is not VisualizationPage)
        {
            await this.NavPage.PushAsync(new VisualizationPage());
        }
        else if (sourceList.SelectedItem == this.LyricsNavItem && this.NavPage.CurrentPage is not LyricsPage)
        {
            await this.NavPage.PushAsync(new LyricsPage());
        }
        else if (sourceList.SelectedItem == this.PlaylistNavItem && this.NavPage.CurrentPage is not PlaylistPage)
        {
            await this.NavPage.PushAsync(new PlaylistPage());
        }
        else if (sourceList.SelectedItem == this.SettingsNavItem && this.NavPage.CurrentPage is not SettingsPage)
        {
            await this.NavPage.PushAsync(new SettingsPage());
        }
    }

    private void SyncSidebarSelection()
    {
        this.isNavigatingProgrammatically = true;

        if (this.NavPage.CurrentPage is HomePage)
        {
            this.TopNavList.SelectedItem = this.HomeNavItem;
            this.BottomNavList.SelectedItem = null;
        }
        else if (this.NavPage.CurrentPage is NowPlayingPage)
        {
            this.TopNavList.SelectedItem = this.NowPlayingNavItem;
            this.BottomNavList.SelectedItem = null;
        }
        else if (this.NavPage.CurrentPage is VisualizationPage)
        {
            this.TopNavList.SelectedItem = this.VisualizationNavItem;
            this.BottomNavList.SelectedItem = null;
        }
        else if (this.NavPage.CurrentPage is LyricsPage)
        {
            this.TopNavList.SelectedItem = this.LyricsNavItem;
            this.BottomNavList.SelectedItem = null;
        }
        else if (this.NavPage.CurrentPage is PlaylistPage)
        {
            this.TopNavList.SelectedItem = this.PlaylistNavItem;
            this.BottomNavList.SelectedItem = null;
        }
        else if (this.NavPage.CurrentPage is SettingsPage)
        {
            this.BottomNavList.SelectedItem = this.SettingsNavItem;
            this.TopNavList.SelectedItem = null;
        }

        this.isNavigatingProgrammatically = false;
    }

    private async Task BrowseAsync(CancellationToken cancellationToken)
    {
        var mainViewModel = this.DataContext as MainViewModel ?? throw new NotSupportedException();
        var topLevel = TopLevel.GetTopLevel(this) ?? throw new NotSupportedException();

        await mainViewModel.BrowseAsync(topLevel.StorageProvider, cancellationToken);
    }
}
