// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Core.Storage;

using System.Collections.Generic;

public interface IXPlatStorageFolder : IXPlatStorageItem
{
    IAsyncEnumerable<IXPlatStorageItem> GetItemsAsync();

    Task<IXPlatStorageFolder?> GetFolderAsync(string name);

    Task<IXPlatStorageFile?> GetFileAsync(string name);
}
