// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Core.Internal.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

internal class ZipFolder : IFolder
{
    private static readonly bool ExtractFullTrackInMemory = false;

    private readonly ZipArchive archive;

    public ZipFolder(string containerPath)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(containerPath);

        this.ContainerPath = containerPath;
        this.archive = new ZipArchive(new FileStream(containerPath, FileMode.Open, FileAccess.Read));
    }

    public string ContainerPath { get; }

    public bool FileExists(string fileName)
    {
        return !string.IsNullOrEmpty(fileName) && this.archive.GetEntry(fileName) is not null;
    }

    public IEnumerable<string> EnumerateFilesByExtension(string extension)
    {
        ArgumentNullException.ThrowIfNull(extension);

        foreach (var entry in this.archive.Entries)
        {
            if (entry.FullName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                yield return entry.FullName;
            }
        }
    }

    public long GetFileSize(string fileName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(fileName);

        var entry = this.archive.GetEntry(fileName);
        if (entry is null)
        {
            throw EntryNotFound(fileName);
        }

        return entry.Length;
    }

    public byte[] ReadFileBytes(string fileName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(fileName);

        var entry = this.archive.GetEntry(fileName);
        if (entry is null)
        {
            throw EntryNotFound(fileName);
        }

        using var stream = entry.Open();
        byte[] bytes = new byte[entry.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    public byte[] ReadFileBytes(string fileName, long offset, long count)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(fileName);

        var entry = this.archive.GetEntry(fileName);
        if (entry is null)
        {
            throw EntryNotFound(fileName);
        }

        using var stream = entry.Open();
        stream.Seek(offset, SeekOrigin.Begin);
        byte[] buffer = new byte[count];
        int bytesRead = stream.Read(buffer);
        if (bytesRead < count)
        {
            Array.Resize(ref buffer, bytesRead);
        }

        return buffer;
    }

    public string ReadFileText(string fileName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(fileName);

        var entry = this.archive.GetEntry(fileName);
        if (entry is null)
        {
            throw EntryNotFound(fileName);
        }

        using var stream = entry.Open();
        StreamReader reader = new(stream, Encoding.UTF8);
        var data = reader.ReadToEnd();
        return data;
    }

    public Stream OpenFileStream(string fileName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(fileName);

        var entry = this.archive.GetEntry(fileName);
        if (entry is null)
        {
            throw EntryNotFound(fileName);
        }

        if (ExtractFullTrackInMemory)
        {
            // ZipArchive is not reliable enough to provide random seekable streams,
            // so we copy the entry to a memory stream and return that instead.
            var memoryStream = new MemoryStream(capacity: (int)entry.Length);

            using var zipStream = entry.Open();
            zipStream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            return memoryStream;
        }
        else
        {
            return new ZipEntrySeekableStream(entry);
        }
    }

    private static FileNotFoundException EntryNotFound(string fileName)
    {
        return new FileNotFoundException("File not found inside archive.", fileName);
    }
}
