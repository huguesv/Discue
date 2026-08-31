// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Shared.Avalonia.Helpers;

using System;
using global::Avalonia.Platform.Storage;
using Woohoo.Audio.Core.Storage;

public sealed class AvaloniaStorageFile : IXPlatStorageFile
{
    private readonly IStorageFile storageFile;

    public AvaloniaStorageFile(IStorageFile storageFile)
    {
        this.storageFile = storageFile;
    }

    public string Name => this.storageFile.Name;

    public Uri Path => this.storageFile.Path;

    public async Task<XPlatStorageItemProperties> GetBasicPropertiesAsync()
    {
        var props = await this.storageFile.GetBasicPropertiesAsync();
        return new XPlatStorageItemProperties
        {
            DateCreated = props.DateCreated,
            DateModified = props.DateModified,
            Size = props.Size,
        };
    }

    public async Task<IXPlatStorageFolder?> GetParentAsync()
    {
        var parent = await this.storageFile.GetParentAsync();
        if (parent is null)
        {
            return null;
        }

        return new AvaloniaStorageFolder(parent);
    }

    public Task<Stream> OpenReadAsync()
    {
        return this.storageFile.OpenReadAsync();
    }
}
