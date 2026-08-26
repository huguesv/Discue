// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Consolonia;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Woohoo.Audio.Core;
using Woohoo.Audio.Core.Playback;
using Woohoo.Audio.Playback.Sdl3;
using Woohoo.Audio.Services;
using Woohoo.Audio.Services.Impl;
using Woohoo.Discue.Consolonia.Services;
using Woohoo.Discue.Consolonia.Services.Impl;
using Woohoo.Discue.Consolonia.ViewModels;
using Woohoo.Discue.Consolonia.Views;
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
                        "Woohoo.Discue.Consolonia",
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
                        "Woohoo.Discue.Consolonia",
                        "ApplicationData",
                        "LocalSettings.json");
                    return new LocalSettingsService() { FilePath = settingsFilePath };
                });
                services.AddSingleton<IMediaPlayerService, MediaPlayerService>();
                services.AddSingleton<IMruService>(_ =>
                {
                    var mruFilePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Woohoo.Discue.Consolonia",
                        "ApplicationData",
                        "Mru.json");
                    return new MruService() { MruFilePath = mruFilePath };
                });
                services.AddSingleton<IVisualizationProviderService, VisualizationProviderService>();

                services.AddSingleton<IThemeService, ThemeService>();

                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainViewModel>();

                services.AddSingleton<HomeViewModel>();
                services.AddSingleton<LyricsViewModel>();
                services.AddSingleton<NowPlayingViewModel>();
                services.AddSingleton<PlaybackViewModel>();
                services.AddSingleton<PlaylistViewModel>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<VisualizationViewModel>();

                services.AddSingleton<IFilePickerService, FilePickerService>();
            })
            .Build();
    }

    public IHost Host { get; }

    public static TopLevel? GetTopLevel(Application app)
    {
        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            return desktopLifetime.MainWindow;
        }

        return null;
    }

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

            var themeService = this.Host.Services.GetRequiredService<IThemeService>();
            themeService.SelectedTheme = vm.Settings.SelectedTheme;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
