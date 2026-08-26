// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Avalonia.Android;

using global::Android.App;
using global::Android.Runtime;
using global::Avalonia;
using global::Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using Woohoo.Discue.Avalonia.Android.Services;
using Woohoo.Discue.Avalonia.Services;

[Application]
public class Application : AvaloniaAndroidApplication<App>
{
    protected Application(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
        App.RegisterPlatformServices = services =>
        {
            services.AddSingleton<IPlatformStorageService, AndroidPlatformStorageService>();
        };
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
