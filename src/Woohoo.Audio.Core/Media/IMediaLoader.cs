// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Core.Media;

using Woohoo.Audio.Core.Storage;

public interface IMediaLoader
{
    Task<IAlbumMedia> LoadFromAsync(string filePath, CancellationToken cancellationToken);

    Task<IAlbumMedia> LoadFromAsync(IXPlatStorageFile storageFile, CancellationToken cancellationToken);
}
