// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Avalonia.Services.DesignTime;

using System;
using System.Threading.Tasks;
using global::Avalonia.Platform.Storage;
using Woohoo.Discue.Shared.Avalonia.Services;

internal class NullFilePickerService : IFilePickerService
{
    public Task<string[]> GetFilePathsAsync(IStorageProvider storageProvider, string startFolderPath, string title, bool allowMultiple, IReadOnlyList<FilePickerFileType> filters)
    {
        return Task.FromResult(Array.Empty<string>());
    }

    public Task<IStorageFile[]> GetFilesAsync(IStorageProvider storageProvider, string startFolderPath, string title, bool allowMultiple, IReadOnlyList<FilePickerFileType> filters)
    {
        return Task.FromResult(Array.Empty<IStorageFile>());
    }
}
