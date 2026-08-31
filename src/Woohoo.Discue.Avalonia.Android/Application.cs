// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Avalonia.Android;

using global::Android.App;
using global::Android.Runtime;
using global::Avalonia;
using global::Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Woohoo.Audio.Core.Playback;
using Woohoo.Audio.Playback.Android;
using Woohoo.Audio.Services;

[Application]
public class Application : AvaloniaAndroidApplication<App>
{
    protected Application(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
        App.RegisterPlatformServices = services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IAudioPlayerProvider, AndroidAudioPlayerProvider>());
        };
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    private class AndroidAudioPlayerProvider : IAudioPlayerProvider
    {
        private readonly AndroidAudioPlayer player = new();

        public IAudioPlayer GetAudioPlayer() => this.player;
    }
}
