// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Avalonia.Views;

using CommunityToolkit.Mvvm.Messaging;
using global::Avalonia.Controls;
using Woohoo.Discue.Avalonia.ViewModels;

public partial class MobileView : TabbedPage
{
    public MobileView()
    {
        this.InitializeComponent();

        WeakReferenceMessenger.Default.Register<BrowseAlbumMessage>(this, (r, m) =>
        {
            _ = this.BrowseAsync(CancellationToken.None);
        });
    }

    private async Task BrowseAsync(CancellationToken cancellationToken)
    {
        var mainViewModel = this.DataContext as MainViewModel ?? throw new NotSupportedException();
        var topLevel = TopLevel.GetTopLevel(this) ?? throw new NotSupportedException();

        await mainViewModel.BrowseAsync(topLevel.StorageProvider, cancellationToken);
    }
}
