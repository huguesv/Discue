// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Core.Storage;

public interface IXPlatStorageFile : IXPlatStorageItem
{
    Task<Stream> OpenReadAsync();
}
