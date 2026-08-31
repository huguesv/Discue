// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.Discue.Cli;

using System.Timers;
using Woohoo.Audio.Core.Playback;
using Woohoo.Audio.Services;

internal class ConsolePlayer
{
    private readonly IMediaPlayerService mediaPlayerService;
    private readonly Timer commandEraseTimer;

    private int volume;
    private bool isMuted;

    private (int Left, int Top) cursorBeginPos;

    private string previousTrackNumberAndTitle;
    private string previousTrackTime;

    public ConsolePlayer(IMediaPlayerService mediaPlayerService)
    {
        this.mediaPlayerService = mediaPlayerService;
        this.mediaPlayerService.ActiveTrackChanged += this.MediaPlayerService_ActiveTrackChanged;
        this.mediaPlayerService.LyricsUpdated += this.MediaPlayerService_LyricsUpdated;
        this.mediaPlayerService.MetadataUpdated += this.MediaPlayerService_MetadataUpdated;
        this.mediaPlayerService.PlaybackPositionChanged += this.MediaPlayerService_PlaybackPositionChanged;
        this.mediaPlayerService.PlaybackStateChanged += this.MediaPlayerService_PlaybackStateChanged;
        this.mediaPlayerService.PlaylistUpdated += this.MediaPlayerService_PlaylistUpdated;

        this.volume = this.mediaPlayerService.Volume;

        this.commandEraseTimer = new Timer(2000);
        this.commandEraseTimer.Elapsed += this.OnCommandEraseTimerElapsed;
        this.commandEraseTimer.Enabled = false;
        this.commandEraseTimer.AutoReset = false;

        this.previousTrackNumberAndTitle = string.Empty;
        this.previousTrackTime = string.Empty;
    }

    public static void ClearScreen()
    {
        Console.Clear();
    }

    public static void PrintCopyright()
    {
        Console.WriteLine("Discue. Copyright (c) 2025-2026 Hugues Valois.");
        Console.WriteLine("Play music from CDs in bin/cue, zipped bin/cue and chd format.");
        Console.WriteLine();
    }

    public static void PrintUsage()
    {
        Console.WriteLine("Usage: Woohoo.Discue.Cli <cue-or-zip-or-chd-file>");
        Console.WriteLine();
    }

    public static void PrintCommands()
    {
        Console.WriteLine("Q  : Quit          P  : Play/Pause   M : Mute/Unmute");
        Console.WriteLine("UP : Volume Up     LT : Prev Track   - : Seek Back");
        Console.WriteLine("DN : Volume Down   RT : Next Track   + : Seek Forward");
        Console.WriteLine();
    }

    public async Task LoadAlbumAsync(string filePath, CancellationToken cancellationToken)
    {
        this.cursorBeginPos = Console.GetCursorPosition();
        await this.mediaPlayerService.LoadFromFileAsync(filePath, cancellationToken);
    }

    public bool HandleKey(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.P:
                this.PlayPauseTrack();
                break;
            case ConsoleKey.M:
                this.Mute();
                break;
            case ConsoleKey.UpArrow:
                this.VolumeUp();
                break;
            case ConsoleKey.DownArrow:
                this.VolumeDown();
                break;
            case ConsoleKey.LeftArrow:
                this.PreviousTrack();
                break;
            case ConsoleKey.RightArrow:
                this.NextTrack();
                break;
            case ConsoleKey.OemMinus:
                this.SkipBackward();
                break;
            case ConsoleKey.OemPlus:
                this.SkipForward();
                break;
            case ConsoleKey.Q:
                return false;
        }

        return true;
    }

    private static string PositionToString(TimeSpan position)
    {
        return position.ToString("mm\\:ss");
    }

    private void PrintAlbumTitle(string text) => this.PrintAtLine(text, 0);

    private void PrintTrackNumberAndTitle(string text) => this.PrintAtLine(text, 1);

    private void PrintTrackTime(string text) => this.PrintAtLine(text, 2);

    private void PrintLyric(string text) => this.PrintAtLine(text, 3);

    private void PrintCommand(string text)
    {
        this.PrintAtLine(text, 4);

        // Commands are only visible for a short time
        if (!string.IsNullOrEmpty(text))
        {
            this.commandEraseTimer.Stop();
            this.commandEraseTimer.Start();
        }
    }

    private void PrintAtLine(string text, int lineOffset)
    {
        Console.SetCursorPosition(this.cursorBeginPos.Left, this.cursorBeginPos.Top + lineOffset);

        int width = Console.WindowWidth;
        if (text.Length > width)
        {
            text = text[..width];
        }

        Console.WriteLine(text.PadRight(width));
    }

    private void OnCommandEraseTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        this.PrintCommand(string.Empty);
    }

    private void SkipForward()
    {
        this.PrintCommand($"Seek Forward 15 secs");

        this.mediaPlayerService.SeekForward(TimeSpan.FromSeconds(15));
    }

    private void SkipBackward()
    {
        this.PrintCommand($"Seek Back 15 secs");

        this.mediaPlayerService.SeekBackward(TimeSpan.FromSeconds(15));
    }

    private void PlayPauseTrack()
    {
        var currentState = this.mediaPlayerService.PlaybackState;

        this.mediaPlayerService.PlayPause();

        this.PrintCommand(currentState == AudioPlayerStatus.Playing ? $"Pause" : $"Play");
    }

    private void Mute()
    {
        if (!this.mediaPlayerService.CanAdjustVolume)
        {
            return;
        }

        this.isMuted = !this.isMuted;
        this.mediaPlayerService.Volume = this.isMuted ? 0 : this.volume;

        this.PrintCommand(this.isMuted ? $"Mute" : $"Unmute");
    }

    private void VolumeDown()
    {
        if (!this.mediaPlayerService.CanAdjustVolume)
        {
            return;
        }

        this.isMuted = false;
        this.volume = Math.Max(0, this.volume - 5);
        this.mediaPlayerService.Volume = this.volume;

        this.PrintCommand($"Volume {this.volume}");
    }

    private void VolumeUp()
    {
        if (!this.mediaPlayerService.CanAdjustVolume)
        {
            return;
        }

        this.isMuted = false;
        this.volume = Math.Min(100, this.volume + 5);
        this.mediaPlayerService.Volume = this.volume;

        this.PrintCommand($"Volume {this.volume}");
    }

    private void NextTrack()
    {
        if (!this.mediaPlayerService.IsNextTrackEnabled)
        {
            return;
        }

        this.PrintCommand("Next Track");

        this.mediaPlayerService.NextTrack();
    }

    private void PreviousTrack()
    {
        if (!this.mediaPlayerService.IsPreviousTrackEnabled)
        {
            return;
        }

        this.PrintCommand("Previous Track");

        this.mediaPlayerService.PreviousTrack();
    }

    private void MediaPlayerService_ActiveTrackChanged(object? sender, EventArgs e)
    {
        var track = this.mediaPlayerService.GetActiveTrack();

        this.UpdateCurrentAlbumTitle();
        this.UpdateCurrentTrackTitle(track);
        this.UpdateCurrentTrackPosition(track);
        this.UpdateCurrentLyric(track);
    }

    private void MediaPlayerService_LyricsUpdated(object? sender, AudioPlayerTrackEventArgs e)
    {
        var track = this.mediaPlayerService.GetActiveTrack();

        this.UpdateCurrentLyric(track);
    }

    private void MediaPlayerService_MetadataUpdated(object? sender, AudioPlayerTrackEventArgs e)
    {
        var track = this.mediaPlayerService.GetActiveTrack();

        this.UpdateCurrentAlbumTitle();
        this.UpdateCurrentTrackTitle(track);
    }

    private void MediaPlayerService_PlaybackPositionChanged(object? sender, EventArgs e)
    {
        var track = this.mediaPlayerService.GetActiveTrack();

        this.UpdateCurrentTrackPosition(track);
        this.UpdateCurrentLyric(track);
    }

    private void MediaPlayerService_PlaybackStateChanged(object? sender, EventArgs e)
    {
    }

    private void MediaPlayerService_PlaylistUpdated(object? sender, EventArgs e)
    {
        var track = this.mediaPlayerService.GetActiveTrack();

        this.UpdateCurrentTrackTitle(track);
    }

    private void UpdateCurrentAlbumTitle()
    {
        var track = this.mediaPlayerService.GetActiveTrack();
        if (track is null)
        {
            return;
        }

        var disc = this.mediaPlayerService.FindDisc(track.DiscId);
        var discMetadata = this.mediaPlayerService.GetDiscMetadata(track.DiscId);

        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(discMetadata?.AlbumPerformer))
        {
            sb.Append(discMetadata?.AlbumPerformer);
            sb.Append(" - ");
        }

        if (!string.IsNullOrEmpty(discMetadata?.AlbumTitle))
        {
            sb.Append(discMetadata?.AlbumTitle);
        }
        else if (!string.IsNullOrEmpty(disc?.Name))
        {
            sb.Append(disc.Name);
        }

        this.PrintAlbumTitle(sb.ToString());
    }

    private void UpdateCurrentLyric(AudioPlayerTrack? track)
    {
        var lyrics = track is not null ? this.mediaPlayerService.GetTrackLyrics(track.Id) : null;
        if (lyrics is not null)
        {
            var lyric = lyrics.GetLineAt(this.mediaPlayerService.PlaybackPosition);
            this.PrintLyric(lyric ?? string.Empty);
        }
    }

    private void UpdateCurrentTrackTitle(AudioPlayerTrack? track)
    {
        if (track is null)
        {
            return;
        }

        var tracks = this.mediaPlayerService.PlaylistTracks;
        var trackIndex = tracks.IndexOf(track);

        var trackMetadata = this.mediaPlayerService.GetTrackMetadata(track.Id);

        StringBuilder sb = new StringBuilder();
        sb.Append($"Track {trackIndex + 1:D2} of {tracks.Length:D2}");

        if (!string.IsNullOrEmpty(trackMetadata?.TrackPerformer))
        {
            sb.Append(" - ");
            sb.Append(trackMetadata?.TrackPerformer);
        }

        if (!string.IsNullOrEmpty(trackMetadata?.TrackTitle))
        {
            sb.Append(" - ");
            sb.Append(trackMetadata?.TrackTitle);
        }

        string trackNumberAndTitle = sb.ToString();

        if (trackNumberAndTitle != this.previousTrackNumberAndTitle)
        {
            this.PrintTrackNumberAndTitle(trackNumberAndTitle);
            this.previousTrackNumberAndTitle = trackNumberAndTitle;
        }
    }

    private void UpdateCurrentTrackPosition(AudioPlayerTrack? track)
    {
        if (track is null)
        {
            return;
        }

        string trackLength = PositionToString(track.Duration);
        string trackPos = PositionToString(this.mediaPlayerService.PlaybackPosition);

        string trackTime = $"[{trackPos}/{trackLength}]";

        // Avoid re-printing the same thing to avoid flicker
        if (trackTime != this.previousTrackTime)
        {
            this.PrintTrackTime(trackTime);
            this.previousTrackTime = trackTime;
        }
    }
}
