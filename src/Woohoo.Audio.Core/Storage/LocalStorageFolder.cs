// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Core.Storage;

using System;
using System.Collections.Generic;

public sealed class LocalStorageFolder : IXPlatStorageFolder
{
    private readonly string absolutePath;

    public LocalStorageFolder(string absolutePath)
    {
        this.absolutePath = absolutePath;
    }

    public string Name => System.IO.Path.GetFileName(this.absolutePath);

    public Uri Path => throw new NotImplementedException();

    public Task<XPlatStorageItemProperties> GetBasicPropertiesAsync()
    {
        var info = new DirectoryInfo(this.absolutePath);

        var result = new XPlatStorageItemProperties
        {
            DateCreated = info.CreationTimeUtc,
            DateModified = info.LastWriteTimeUtc,
            Size = null,
        };

        return Task.FromResult(result);
    }

    public Task<IXPlatStorageFolder?> GetParentAsync()
    {
        var parentPath = System.IO.Path.GetDirectoryName(this.absolutePath);
        if (parentPath == null)
        {
            return Task.FromResult<IXPlatStorageFolder?>(null);
        }

        return Task.FromResult<IXPlatStorageFolder?>(new LocalStorageFolder(parentPath));
    }

    public Task<IXPlatStorageFile?> GetFileAsync(string name)
    {
        var filePath = System.IO.Path.Combine(this.absolutePath, name);
        if (!File.Exists(filePath))
        {
           return Task.FromResult<IXPlatStorageFile?>(null);
        }

        return Task.FromResult<IXPlatStorageFile?>(new LocalStorageFile(filePath));
    }

    public Task<IXPlatStorageFolder?> GetFolderAsync(string name)
    {
        var folderPath = System.IO.Path.Combine(this.absolutePath, name);
        if (!Directory.Exists(folderPath))
        {
            return Task.FromResult<IXPlatStorageFolder?>(null);
        }

        return Task.FromResult<IXPlatStorageFolder?>(new LocalStorageFolder(folderPath));
    }

    public async IAsyncEnumerable<IXPlatStorageItem> GetItemsAsync()
    {
        var entries = Directory.EnumerateFileSystemEntries(this.absolutePath);
        foreach (var entry in entries)
        {
            if (File.Exists(entry))
            {
                yield return new LocalStorageFile(entry);
            }
            else if (Directory.Exists(entry))
            {
                yield return new LocalStorageFolder(entry);
            }
        }
    }
}
