// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Shared.Avalonia.Helpers;

using System;
using System.Collections.Generic;
using global::Avalonia.Platform.Storage;
using Woohoo.Audio.Core.Storage;

internal sealed class AvaloniaStorageFolder : IXPlatStorageFolder
{
    private readonly IStorageFolder storageFolder;

    public AvaloniaStorageFolder(IStorageFolder storageFolder)
    {
        this.storageFolder = storageFolder;
    }

    public string Name => this.storageFolder.Name;

    public Uri Path => this.storageFolder.Path;

    public async Task<XPlatStorageItemProperties> GetBasicPropertiesAsync()
    {
        var props = await this.storageFolder.GetBasicPropertiesAsync();
        return new XPlatStorageItemProperties
        {
            DateCreated = props.DateCreated,
            DateModified = props.DateModified,
            Size = props.Size,
        };
    }

    public async Task<IXPlatStorageFolder?> GetParentAsync()
    {
        var parent = await this.storageFolder.GetParentAsync();
        if (parent is null)
        {
            return null;
        }

        return new AvaloniaStorageFolder(parent);
    }

    public async Task<IXPlatStorageFile?> GetFileAsync(string name)
    {
        var file = await this.storageFolder.GetFileAsync(name);
        if (file is null)
        {
            return null;
        }

        return new AvaloniaStorageFile(file);
    }

    public async Task<IXPlatStorageFolder?> GetFolderAsync(string name)
    {
        var folder = await this.storageFolder.GetFolderAsync(name);
        if (folder is null)
        {
            return null;
        }

        return new AvaloniaStorageFolder(folder);
    }

    public async IAsyncEnumerable<IXPlatStorageItem> GetItemsAsync()
    {
        await foreach (var item in this.storageFolder.GetItemsAsync())
        {
            if (item is IStorageFile file)
            {
                yield return new AvaloniaStorageFile(file);
            }
            else if (item is IStorageFolder folder)
            {
                yield return new AvaloniaStorageFolder(folder);
            }
        }
    }
}
