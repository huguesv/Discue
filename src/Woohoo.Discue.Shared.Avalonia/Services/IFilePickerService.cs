// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Shared.Avalonia.Services;

using global::Avalonia.Platform.Storage;

public interface IFilePickerService
{
    Task<string[]> GetFilePathsAsync(IStorageProvider storageProvider, string startFolderPath, string title, bool allowMultiple, IReadOnlyList<FilePickerFileType> filters);
}
