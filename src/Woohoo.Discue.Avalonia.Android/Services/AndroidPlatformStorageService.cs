namespace Woohoo.Discue.Avalonia.Android.Services;

using global::Avalonia;
using global::Avalonia.Android;
using global::Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using Woohoo.Discue.Avalonia.Services;

internal class AndroidPlatformStorageService : IPlatformStorageService
{
    public global::Avalonia.Platform.Storage.IStorageProvider? GetStorageProvider()
    {
        // 🧠 Cast the active activity to IAvaloniaActivity to extract the platform StorageProvider
        if (MainActivity.Instance is IAvaloniaActivity avaloniaActivity)
        {
            return null;
        }

        return null;
    }
}
