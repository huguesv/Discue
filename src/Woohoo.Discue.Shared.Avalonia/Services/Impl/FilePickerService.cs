// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Shared.Avalonia.Services.Impl;

using global::Avalonia.Platform.Storage;

public sealed class FilePickerService : IFilePickerService
{
    public async Task<string[]> GetFilePathsAsync(IStorageProvider storageProvider, string startFolderPath, string title, bool allowMultiple, IReadOnlyList<FilePickerFileType> filters)
    {
        if (storageProvider.CanOpen == true)
        {
            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = allowMultiple,
                FileTypeFilter = filters,
            };

            try
            {
                var files = await storageProvider.OpenFilePickerAsync(options);

                var filePaths = new List<string>();
                foreach (var file in files)
                {
                    string? path = file.TryGetLocalPath();
                    if (!string.IsNullOrEmpty(path))
                    {
                        filePaths.Add(path);
                    }
                }

                return [.. filePaths];
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        return [];
    }

    public async Task<IStorageFile[]> GetFilesAsync(IStorageProvider storageProvider, string startFolderPath, string title, bool allowMultiple, IReadOnlyList<FilePickerFileType> filters)
    {
        if (storageProvider.CanOpen == true)
        {
            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = allowMultiple,
                FileTypeFilter = filters,
            };

            try
            {
                var files = await storageProvider.OpenFilePickerAsync(options);
                return [.. files];
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        return [];
    }
}
