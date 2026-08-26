// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Core.Storage;

using System;

public sealed class LocalStorageFile : IXPlatStorageFile
{
    private readonly string absolutePath;

    public LocalStorageFile(string absolutePath)
    {
        this.absolutePath = absolutePath;
    }

    public string Name => System.IO.Path.GetFileName(this.absolutePath);

    public Uri Path => throw new NotImplementedException();

    public Task<XPlatStorageItemProperties> GetBasicPropertiesAsync()
    {
        var info = new FileInfo(this.absolutePath);

        var result = new XPlatStorageItemProperties
        {
            DateCreated = info.CreationTimeUtc,
            DateModified = info.LastWriteTimeUtc,
            Size = (ulong)info.Length,
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

    public Task<Stream> OpenReadAsync()
    {
        return Task.FromResult<Stream>(File.OpenRead(this.absolutePath));
    }
}
