// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Consolonia.Views;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using global::Consolonia.Controls;
using Woohoo.Discue.Consolonia.ViewModels;

public partial class MainControl : UserControl
{
    public MainControl()
    {
        this.InitializeComponent();

        WeakReferenceMessenger.Default.Register<CurrentLyricChangeMessage>(this, (r, m) =>
        {
            this.ScrollToLyricLine(m);
        });

        WeakReferenceMessenger.Default.Register<MediaErrorMessage>(this, (r, m) =>
        {
            this.ShowError(m);
        });

        WeakReferenceMessenger.Default.Register<BrowseAlbumMessage>(this, (r, m) =>
        {
            _ = this.BrowseAsync(CancellationToken.None);
        });
    }

    private void OnExit(object sender, RoutedEventArgs e)
    {
        var lifetime = Application.Current!.ApplicationLifetime as IControlledApplicationLifetime;
        lifetime!.Shutdown();
    }

    private void PlaylistListBox_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: PlaylistItemViewModel playlistItem })
        {
            playlistItem.PlayCommand.Execute(null);
        }
    }

    private void PlaylistListBox_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        if (sender is ListBox { SelectedItem: PlaylistItemViewModel playlistItem })
        {
            playlistItem.PlayCommand.Execute(null);
        }
    }

    private void RecentDiscsListBox_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: HomeRecentDiscViewModel recentDisc })
        {
            if (File.Exists(recentDisc.AlbumFilePath))
            {
                _ = (this.DataContext as MainViewModel)?.OpenFileAsync(recentDisc.AlbumFilePath, CancellationToken.None);
            }
        }
    }

    private void RecentDiscsListBox_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        if (sender is ListBox { SelectedItem: HomeRecentDiscViewModel recentDisc })
        {
            if (File.Exists(recentDisc.AlbumFilePath))
            {
                _ = (this.DataContext as MainViewModel)?.OpenFileAsync(recentDisc.AlbumFilePath, CancellationToken.None);
            }
        }
    }

    private void ScrollToLyricLine(CurrentLyricChangeMessage m)
    {
        if (m.AutoScroll)
        {
            this.LyricsItemsRepeater
                .GetOrCreateElement(Math.Min(m.Index + 1, m.LineCount - 1))?
                .BringIntoView();
        }
    }

    private void ShowError(MediaErrorMessage m)
    {
        var mb = new MessageBox
        {
            MessageBoxStyle = MessageBoxStyle.Ok,
            Title = "Error",
            Message = m.Text,
        };

        mb.ShowDialog();
    }

    private async Task BrowseAsync(CancellationToken cancellationToken)
    {
        var mainViewModel = this.DataContext as MainViewModel ?? throw new NotSupportedException();
        var topLevel = TopLevel.GetTopLevel(this) ?? throw new NotSupportedException();

        await mainViewModel.BrowseAsync(topLevel.StorageProvider, cancellationToken);
    }
}
