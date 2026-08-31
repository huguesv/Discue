// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Audio.Services.Impl;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Woohoo.Audio.Core;
using Woohoo.Audio.Core.Lyrics;
using Woohoo.Audio.Core.Media;
using Woohoo.Audio.Core.Metadata;
using Woohoo.Audio.Core.Playback;
using Woohoo.Audio.Core.Storage;

public sealed class MediaPlayerService : IMediaPlayerService
{
    private static readonly string LrcDbPath = Environment.GetEnvironmentVariable("LRCLIB_DB_PATH") ?? string.Empty;
    private static readonly MetadataProvider MetadataProvider = new(new HttpClientFactory());
    private static readonly LyricsProviderOptions LyricsProviderOptions = new()
    {
        DatabaseFilePath = LrcDbPath,
        UseDatabase = true,
        UseWeb = true,
        UseWebExternalSources = true,
    };

    private static readonly LyricsProvider LyricsProvider = new(LyricsProviderOptions, new HttpClientFactory());

    private readonly IMruService mruService;
    private readonly IBitmapCacheService bitmapCacheService;
    private readonly ILocalSettingsService localSettingsService;
    private readonly IAudioPlayer player;

    private readonly ConcurrentDictionary<Guid, AudioPlayerDiscMetadata> discMetadataCache = [];
    private readonly ConcurrentDictionary<Guid, AudioPlayerTrackMetadata> trackMetadataCache = [];
    private readonly ConcurrentDictionary<Guid, LyricsTrack> lyricsCache = [];

    public MediaPlayerService(
        IMruService mruService,
        IBitmapCacheService bitmapCacheService,
        ILocalSettingsService localSettingsService,
        IAudioPlayerProvider playerProvider)
    {
        ArgumentNullException.ThrowIfNull(mruService);
        ArgumentNullException.ThrowIfNull(bitmapCacheService);
        ArgumentNullException.ThrowIfNull(localSettingsService);
        ArgumentNullException.ThrowIfNull(playerProvider);

        this.mruService = mruService;
        this.bitmapCacheService = bitmapCacheService;
        this.localSettingsService = localSettingsService;
        this.player = playerProvider.GetAudioPlayer();

        this.player.PlaybackPositionChanged += this.Player_PlaybackPositionChanged;
        this.player.ActiveTrackChanged += this.Player_ActiveTrackChanged;
        this.player.PlaybackStateChanged += this.Player_PlaybackStateChanged;
    }

    public event EventHandler<EventArgs>? DiscLoading;

    public event EventHandler<EventArgs>? DiscLoaded;

    public event EventHandler<EventArgs>? PlaylistUpdated;

    public event EventHandler<AudioPlayerTrackEventArgs>? MetadataUpdated;

    public event EventHandler<AudioPlayerTrackEventArgs>? LyricsUpdated;

    public event EventHandler<EventArgs>? ActiveTrackChanged;

    public event EventHandler<EventArgs>? PlaybackStateChanged;

    public event EventHandler<EventArgs>? PlaybackPositionChanged;

    public string AudioEngineDisplayName => this.player.AudioEngineDisplayName;

    public ImmutableArray<AudioPlayerTrack> PlaylistTracks => this.player.Tracks;

    public ImmutableArray<AudioPlayerDisc> Discs => this.player.Discs;

    public AudioPlayerStatus PlaybackState => this.player.PlaybackState;

    public TimeSpan PlaybackPosition => this.player.PlaybackPosition;

    public bool CanAdjustVolume => this.player.CanAdjustVolume;

    public int Volume
    {
        get => this.player.Volume;
        set => this.player.Volume = value;
    }

    public bool IsNextTrackEnabled => this.player.IsNextTrackEnabled;

    public bool IsPreviousTrackEnabled => this.player.IsPreviousTrackEnabled;

    public IAudioPlayerVisualization Visualization => this.player.Visualization;

    public AudioPlayerTrack? GetActiveTrack() => this.player.ActiveTrack;

    public AudioPlayerDisc? FindDisc(Guid id) => this.player.FindDisc(id);

    public AudioPlayerTrack? FindTrack(Guid id) => this.player.FindTrack(id);

    public AudioPlayerDiscMetadata? GetDiscMetadata(Guid id)
    {
        return this.discMetadataCache.TryGetValue(id, out var metadata)
            ? metadata
            : null;
    }

    public AudioPlayerTrackMetadata? GetTrackMetadata(Guid id)
    {
        return this.trackMetadataCache.TryGetValue(id, out var metadata)
            ? metadata
            : null;
    }

    public LyricsTrack? GetTrackLyrics(Guid id)
    {
        return this.lyricsCache.TryGetValue(id, out var lyrics)
            ? lyrics
            : null;
    }

    public void PreviousTrack() => this.player.PreviousTrack();

    public void PlayPause() => this.player.PlayPause();

    public void NextTrack() => this.player.NextTrack();

    public void SeekForward(TimeSpan span) => this.player.SeekForward(span);

    public void SeekBackward(TimeSpan span) => this.player.SeekBackward(span);

    public void SeekTo(TimeSpan span) => this.player.SeekTo(span);

    public void Play(Guid trackId) => this.player.Play(trackId);

    public void InitializePlayer()
    {
        this.player.Initialize();
    }

    public async Task LoadFromFileAsync(string albumFilePath, CancellationToken cancellationToken)
    {
        this.DiscLoading?.Invoke(this, EventArgs.Empty);

        await this.player.ClearAsync(cancellationToken);

        var media = await new MediaLoader().LoadFromAsync(albumFilePath, cancellationToken);

        var albumName = Path.GetFileNameWithoutExtension(albumFilePath);
        string mruItemMoniker = albumFilePath;

        await this.PostLoadAsync(media, albumName, mruItemMoniker, cancellationToken);
    }

    public async Task LoadFromStorageAsync(IXPlatStorageFile storageFile, CancellationToken cancellationToken)
    {
        this.DiscLoading?.Invoke(this, EventArgs.Empty);

        await this.player.ClearAsync(cancellationToken);

        var media = await new MediaLoader().LoadFromAsync(storageFile, cancellationToken);

        var albumName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(storageFile.Name));

        // TODO: probably need to obtain a bookmark here for the moniker
        string mruItemMoniker = storageFile.Path.ToString();

        await this.PostLoadAsync(media, albumName, mruItemMoniker, cancellationToken);
    }

    public void Shutdown()
    {
        this.player.Shutdown();
    }

    private static (AudioPlayerDisc Disc, AudioPlayerDiscMetadata DiscMetadata, ImmutableArray<(AudioPlayerTrack PlayerTrack, AudioPlayerTrackMetadata TrackMetadata, IAlbumTrack AlbumTrack)> Tracks) Convert(IAlbumMedia media, string name)
    {
        var disc = new AudioPlayerDisc()
        {
            Id = Guid.NewGuid(),
            CTDBToc = media.CTDBToc,
            Name = name,
        };

        var tracks = new List<(AudioPlayerTrack, AudioPlayerTrackMetadata, IAlbumTrack)>();

        foreach (var mediaTrack in media.Tracks)
        {
            var trackMetadata = new AudioPlayerTrackMetadata
            {
                TrackTitle = mediaTrack.TrackTitle.Trim(),
                TrackPerformer = mediaTrack.TrackPerformer.Trim(),
                AlbumTitle = mediaTrack.AlbumTitle.Trim(),
                AlbumPerformer = mediaTrack.AlbumPerformer.Trim(),
            };

            var item = new AudioPlayerTrack
            {
                Id = Guid.NewGuid(),
                DiscId = disc.Id,
                TrackNumber = mediaTrack.TrackNumber,
                TrackSize = mediaTrack.TrackSize,
                Duration = TimeConversion.FromPosition(mediaTrack.TrackSize),
            };

            tracks.Add((item, trackMetadata, mediaTrack));
        }

        var discMetadata = new AudioPlayerDiscMetadata();

        return (disc, discMetadata, [.. tracks]);
    }

    private async Task PostLoadAsync(IAlbumMedia media, string albumName, string mruItemMoniker, CancellationToken cancellationToken)
    {
        this.discMetadataCache.Clear();
        this.trackMetadataCache.Clear();
        this.lyricsCache.Clear();

        var (disc, discMetadata, tracks) = Convert(media, albumName);
        foreach (var track in tracks)
        {
            this.trackMetadataCache.AddOrUpdate(track.PlayerTrack.Id, track.TrackMetadata, (id, existing) => track.TrackMetadata);
        }

        this.discMetadataCache.AddOrUpdate(disc.Id, discMetadata, (id, existing) => discMetadata);

        await this.player.LoadAsync(disc, tracks, cancellationToken);

        this.DiscLoaded?.Invoke(this, EventArgs.Empty);

        this.PlaylistUpdated?.Invoke(this, EventArgs.Empty);

        var mruItem = this.mruService.FindItem(mruItemMoniker);
        if (mruItem is not null)
        {
            var updatedMruItem = mruItem with
            {
                LastUpdated = DateTime.UtcNow,
            };

            this.mruService.AddOrUpdateItem(updatedMruItem);
        }
        else
        {
            mruItem = new MruItem(
                mruItemMoniker,
                albumName,
                string.Empty,
                DateTime.UtcNow);

            this.mruService.AddOrUpdateItem(mruItem);
        }

        _ = this.LoadExtrasAsync(mruItemMoniker, disc.Id, disc.CTDBToc, tracks, cancellationToken);
    }

    private void Player_PlaybackStateChanged(object? sender, EventArgs e)
    {
        this.PlaybackStateChanged?.Invoke(this, e);
    }

    private void Player_ActiveTrackChanged(object? sender, EventArgs e)
    {
        this.ActiveTrackChanged?.Invoke(this, e);
    }

    private void Player_PlaybackPositionChanged(object? sender, EventArgs e)
    {
        this.PlaybackPositionChanged?.Invoke(this, e);
    }

    private async Task LoadExtrasAsync(
        string mruItemMoniker,
        Guid discId,
        string toc,
        ImmutableArray<(AudioPlayerTrack PlayerTrack, AudioPlayerTrackMetadata TrackMetadata, IAlbumTrack AlbumTrack)> tracks,
        CancellationToken cancellationToken)
    {
        if (this.localSettingsService.ReadSetting<bool?>(KnownSettingKeys.QueryMetadataOnline) == true)
        {
            await this.LoadMetadataAsync(mruItemMoniker, discId, toc, tracks, cancellationToken);
        }

        if (this.localSettingsService.ReadSetting<bool?>(KnownSettingKeys.QueryLyricsOnline) == true)
        {
            await this.QueryLyricsAsync(tracks.Select(t => t.PlayerTrack.Id), cancellationToken);
        }
    }

    private async Task LoadMetadataAsync(
        string mruItemMoniker,
        Guid discId,
        string toc,
        ImmutableArray<(AudioPlayerTrack PlayerTrack, AudioPlayerTrackMetadata TrackMetadata, IAlbumTrack AlbumTrack)> tracks,
        CancellationToken cancellationToken)
    {
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var audioTrackCount = tracks.Length;
        var metadata = await MetadataProvider.QueryAsync(audioTrackCount, toc, cancellationTokenSource.Token);
        if (metadata is not null)
        {
            var discMetadata = new AudioPlayerDiscMetadata
            {
                AlbumTitle = BestString(metadata.Album.Title, tracks[0].TrackMetadata.AlbumTitle).Trim(),
                AlbumPerformer = BestString(metadata.Album.Artist, tracks[0].TrackMetadata.AlbumPerformer).Trim(),
                Year = metadata.Album.Year,
                AlbumArtImages = [.. metadata.Album.Images.Where(im => im is not null)],
            };

            this.discMetadataCache.AddOrUpdate(discId, discMetadata, (id, existing) => discMetadata);

            await this.player.UpdateDiscMetadataAsync(discId, discMetadata, cancellationToken);

            var imageUrl = metadata.Album.Images.FirstOrDefault(md => md.IsPrimary)?.Url;

            for (int i = 0; i < tracks.Length; i++)
            {
                var track = tracks[i];
                var dbTrack = metadata.Tracks[i];

                var trackMetadata = track.TrackMetadata with
                {
                    AlbumTitle = discMetadata.AlbumTitle,
                    AlbumPerformer = discMetadata.AlbumPerformer,
                    TrackTitle = BestString(dbTrack.Name, track.TrackMetadata.TrackTitle).Trim(),
                    TrackPerformer = BestString(dbTrack.Artist, metadata.Album.Artist, track.TrackMetadata.TrackPerformer).Trim(),
                };

                this.trackMetadataCache.AddOrUpdate(track.PlayerTrack.Id, trackMetadata, (id, existing) => trackMetadata);

                await this.player.UpdateTrackMetadataAsync(
                    track.PlayerTrack.Id,
                    trackMetadata,
                    imageUrl is not null ? new Uri(imageUrl) : null,
                    imageUrl is not null ? await this.bitmapCacheService.GetLocalUriAsync(new Uri(imageUrl)) : null,
                    cancellationToken);

                this.MetadataUpdated?.Invoke(this, new AudioPlayerTrackEventArgs(this.player.Tracks[i]));
            }

            var mruItem = this.mruService.FindItem(mruItemMoniker);
            if (mruItem is not null)
            {
                var updatedMruItem = mruItem with
                {
                    LastUpdated = DateTime.UtcNow,
                    FullAlbumTitle = $"{discMetadata.AlbumPerformer} - {discMetadata.AlbumTitle}",
                    AlbumArtUrl = imageUrl ?? string.Empty,
                };

                this.mruService.AddOrUpdateItem(updatedMruItem);
            }
            else
            {
                mruItem = new MruItem(
                    mruItemMoniker,
                    $"{discMetadata.AlbumPerformer} - {discMetadata.AlbumTitle}",
                    imageUrl ?? string.Empty,
                    DateTime.UtcNow);

                this.mruService.AddOrUpdateItem(mruItem);
            }
        }

        static string BestString(params string[] values)
        {
            return values.FirstOrDefault(val => !string.IsNullOrEmpty(val)) ?? string.Empty;
        }
    }

    private async Task QueryLyricsAsync(IEnumerable<Guid> trackIds, CancellationToken cancellationToken)
    {
        try
        {
            CancellationTokenSource tokenSource = new();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(30));
            foreach (var trackId in trackIds)
            {
                var track = this.player.FindTrack(trackId);
                if (track is null)
                {
                    continue;
                }

                var trackMetadata = this.GetTrackMetadata(trackId);
                if (trackMetadata is null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(trackMetadata.AlbumTitle) ||
                    string.IsNullOrEmpty(trackMetadata.AlbumPerformer) ||
                    string.IsNullOrEmpty(trackMetadata.TrackTitle))
                {
                    continue;
                }

                var lyrics = await LyricsProvider.QueryAsync(
                    trackMetadata.AlbumTitle,
                    trackMetadata.AlbumPerformer,
                    trackMetadata.TrackTitle,
                    TimeConversion.FromPosition(track.TrackSize),
                    cancellationToken: tokenSource.Token);

                if (lyrics is not null)
                {
                    this.lyricsCache.AddOrUpdate(trackId, lyrics, (id, existing) => lyrics);

                    this.LyricsUpdated?.Invoke(this, new AudioPlayerTrackEventArgs(track));
                }
            }
        }
        catch
        {
        }
    }
}
