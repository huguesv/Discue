// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Core.Storage;

using System;

public interface IXPlatStorageItem
{
    /// <summary>
    /// Gets the name of the item including the file name extension if there is one.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the file-system path of the item.
    /// </summary>
    Uri Path { get; }

    Task<XPlatStorageItemProperties> GetBasicPropertiesAsync();

    Task<IXPlatStorageFolder?> GetParentAsync();
}
