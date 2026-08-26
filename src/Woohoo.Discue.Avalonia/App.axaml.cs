// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Avalonia;

using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Controls.ApplicationLifetimes;
using global::Avalonia.Data.Core.Plugins;
using global::Avalonia.Markup.Xaml;
using global::Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Woohoo.Audio.Core;
using Woohoo.Audio.Core.Playback;
using Woohoo.Audio.Playback.Sdl3;
using Woohoo.Audio.Services;
using Woohoo.Audio.Services.Impl;
using Woohoo.Discue.Avalonia.Services;
using Woohoo.Discue.Avalonia.Services.DesignTime;
using Woohoo.Discue.Avalonia.Services.Impl;
using Woohoo.Discue.Avalonia.ViewModels;
using Woohoo.Discue.Avalonia.Views;
using Woohoo.Discue.Shared.Avalonia.Services;
using Woohoo.Discue.Shared.Avalonia.Services.Impl;

public partial class App : Application
{
    public App()
    {
        this.Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .UseSerilog()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IAudioPlayerProvider>(serviceProvider =>
                {
                    var factoryMap = new Dictionary<string, Func<IAudioPlayer>>
                    {
                        { AudioEngineType.Sdl3.ToString(), () => new Sdl3AudioPlayer() },
                    };

                    return new AudioPlayerProvider(
                        serviceProvider.GetRequiredService<ILocalSettingsService>(),
                        AudioEngineType.Sdl3.ToString(),
                        factoryMap);
                });

                services.AddSingleton<IAvaloniaBitmapCacheService, AvaloniaBitmapCacheService>();
                services.AddSingleton<IBitmapCacheService>(serviceProvider =>
                {
                    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

                    var cacheFolderPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Woohoo.Discue.Avalonia",
                        "Cache");

                    return new BitmapCacheService(httpClientFactory)
                    {
                        CacheFolderPath = cacheFolderPath,
                    };
                });
                services.AddSingleton<IDispatcherQueueService, DispatcherQueueService>();
                services.AddSingleton<IFilePickerService, FilePickerService>();
                services.AddSingleton<IHttpClientFactory, HttpClientFactory>();
                services.AddSingleton<ILocalSettingsService>(_ =>
                {
                    var settingsFilePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Woohoo.Discue.Avalonia",
                        "ApplicationData",
                        "LocalSettings.json");
                    return new LocalSettingsService() { FilePath = settingsFilePath };
                });
                services.AddSingleton<IMediaPlayerService, MediaPlayerService>();
                services.AddSingleton<IMruService>(_ =>
                {
                    var mruFilePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Woohoo.Discue.Avalonia",
                        "ApplicationData",
                        "Mru.json");
                    return new MruService() { MruFilePath = mruFilePath };
                });
                services.AddSingleton<IVisualizationProviderService, VisualizationProviderService>();

                if (OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600))
                {
                    services.AddSingleton<IPowerManagementService, WindowsPowerManagementService>();
                }
                else if (OperatingSystem.IsMacOS())
                {
                    services.AddSingleton<IPowerManagementService, MacOSPowerManagementService>();
                }
                else
                {
                    services.AddSingleton<IPowerManagementService, NullPowerManagementService>();
                }

                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainViewModel>();

                services.AddSingleton<HomeViewModel>();
                services.AddSingleton<LyricsViewModel>();
                services.AddSingleton<NowPlayingViewModel>();
                services.AddSingleton<PlaybackViewModel>();
                services.AddSingleton<PlaylistViewModel>();
                services.AddSingleton<SettingsViewModel>();

                RegisterPlatformServices?.Invoke(services);
            })
            .Build();
    }

    public static Action<IServiceCollection>? RegisterPlatformServices { get; set; }

    public IHost Host { get; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var vm = this.Host.Services.GetRequiredService<MainViewModel>();

        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.MainWindow = new MainWindow
            {
                DataContext = vm,
            };
        }
        else if (this.ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory = () => new PageNavigationHost()
            {
                Page = new MobileView { DataContext = vm },
            };
        }
        else if (this.ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new PageNavigationHost()
            {
                Page = new MobileView { DataContext = vm },
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
