// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Avalonia.Services;

using global::Avalonia.Platform.Storage;

public interface IPlatformStorageService
{
    IStorageProvider? GetStorageProvider();
}
