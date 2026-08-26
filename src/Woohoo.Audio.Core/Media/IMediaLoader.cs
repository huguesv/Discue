// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Core.Media;

public interface IMediaLoader
{
    Task<IAlbumMedia> LoadFromAsync(string filePath, CancellationToken cancellationToken);
}
