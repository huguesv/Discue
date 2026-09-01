// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.CueToolsDatabase.Web;

using System;
using System.Text;
using Woohoo.Audio.CueToolsDatabase.Web.Models;

internal sealed class CTDBResponseCache
{
    private readonly string filePath;

    public CTDBResponseCache(string cacheFolder, string toc)
    {
        this.filePath = Path.Combine(cacheFolder, CreateMD5(toc) + ".xml");
    }

    public TimeSpan? Age
    {
        get => GetCacheAge(this.filePath);
    }

    public bool Exists
    {
        get => File.Exists(this.filePath);
    }

    public bool TryRead(out CTDBResponse? response)
    {
        return TryRead(this.filePath, out response);
    }

    public void WriteRaw(string result)
    {
        WriteRaw(this.filePath, result);
    }

    private static string CreateMD5(string input)
    {
        var inputBytes = Encoding.ASCII.GetBytes(input);
        var hashBytes = MD5.HashData(inputBytes);
        return Convert.ToHexString(hashBytes);
    }

    private static void SafeDelete(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Ignore deletion errors
        }
    }

    private static TimeSpan? GetCacheAge(string cacheFilePath)
    {
        var cacheFileInfo = new FileInfo(cacheFilePath);
        if (!cacheFileInfo.Exists)
        {
            return null;
        }

        return DateTime.UtcNow - cacheFileInfo.LastWriteTimeUtc;
    }

    private static bool TryRead(string cacheFilePath, out CTDBResponse? response)
    {
        response = null;

        try
        {
            using var fileStream = File.OpenRead(cacheFilePath);
            using var streamReader = new StreamReader(fileStream);
            response = CTDBSerialization.Deserialize(streamReader);
            return response is not null;
        }
        catch
        {
            // Delete corrupted cache
            SafeDelete(cacheFilePath);
        }

        return false;
    }

    private static void WriteRaw(string cacheFilePath, string result)
    {
        try
        {
            var folderPath = Path.GetDirectoryName(cacheFilePath);
            if (folderPath is null)
            {
                return;
            }

            Directory.CreateDirectory(folderPath);

            File.WriteAllText(cacheFilePath, result);
        }
        catch
        {
            // Ignore cache write errors
        }
    }
}
