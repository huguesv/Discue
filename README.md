# Discue - Audio Player for Windows, Linux and MacOS

![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/huguesv/Discue/build-and-test.yml)

Discue is for playing audio tracks from CDs dumped to raw bin/cue or chd files.

The bin/cue files can be loaded from a folder on disk, or from a zip archive.

This player provides a convenient way of listening to dumps in that format
without any additional step(s):

- No need to mount the cue file as a virtual drive.
- No need to extract the cue/bin files from zip files.
- No need to convert the data to wav/flac/ogg/mp3.

This is currently NOT supported:

- Playing from archives in 7z and rar formats.
- Playing music from other formats such as wav, mp3, flac, ogg, etc. Just use
  a regular music player for that.

Album and track metadata is loaded from the CDTEXT information when
present in cue file.

Additional metadata is optionally retrieved from [CueToolsDB](https://db.cue.tools/),
including album art.

Lyrics are optionally retrieved from [LRCLIB](https://lrclib.net).
Using a local LRCLIB sqlite3 database is also supported.

## Desktop Player

![Desktop Player on Windows Screenshot](images/windows-dark-now.png?raw=true "Desktop Player on Windows Screenshot")

![Desktop Player on Mac Screenshot](images/macos-dark-now.png?raw=true "Desktop Player on Windows Screenshot")

![Desktop Player on Mac Screenshot](images/linux-dark-now.png?raw=true "Desktop Player on Linux Screenshot")

## Terminal UI Player

![TUI Player on Windows Terminal](images/consolonia-now.png?raw=true "TUI Player on Windows Terminal")

## Console Player

![Console Player on Windows Terminal](images/windows-cli.png?raw=true "Console Player on Windows Terminal")

For more screenshots, see the [SCREENSHOTS.md](SCREENSHOTS.md) file.

## Releases

Download the [latest release here](https://github.com/huguesv/Discue/releases/latest).

The desktop player (Woohoo.Discue.Avalonia.Desktop) is available for Windows 11,
Linux and MacOS.

The terminal UI player (Woohoo.Discue.Consolonia) is available for Windows 11
and Linux. It may not work on all Linux distributions, depending on the
available version of ncurses.

The console player (Woohoo.Discue.Cli) is available for Windows 11 and Linux.

Windows may prevent you from launching the application, since it is not signed.
- You can still run it by clicking on "More info" and then "Run anyway".

MacOS may prevent you from launching the application, since it is not signed.
- If installing with the package installer, then open **System Settings**,
  **Privacy & Security**, then scroll down to find **Discue** and
  select **Open Anyway**.
- If installing by opening the disc image and drag & dropping into the
  **Applications** folder, then run the following commands from a **Terminal**.
  ```
  cd /Applications
  xattr -d com.apple.quarantine Discue.app
  ```

## Lyrics Configuration

Lyrics are fetched from [LRCLIB](https://lrclib.net) using their API.

You can optionally use a local version of the LRCLIB database:

1. Download a [dump of the latest database](https://lrclib.net/db-dumps).
   Warning: this is a VERY large (~20GB).
1. Extract the .sqlite3 file from the downloaded .gz file.
1. Set the path to the .sqlite3 file in `LRCLIB_DB_PATH` environment variable.
1. Restart the application.

## Usage (Desktop Player)

1. Click the **Open file** button in the home page.
   Also available from the **File** menu on MacOS.

1. Select a .cue file, a .zip file that contains a .cue file, or a .chd file.

1. A new playlist that consists of the audio tracks from the cue sheet will be
   opened and the first track will start playing.

1. You can only load one album at a time. When you load another, the current
   playlist is replaced with the tracks from the new album.

1. Click the **Settings** button (also **Settings** menu on MacOS) to change
   the **Fetch Online Metadata** setting, which is off by default.

1. Click the **Settings** button (also **Settings** menu on MacOS) to change
   the **Fetch Lyrics** setting, which is off by default.
   Note that this requires metadata to be available for your tracks, either
   from CDTEXT in the cue file, or from CueToolsDB.

## Usage (Terminal UI Player)

1. Click the **File** menu, then **Open**.

1. Select a .cue file, a .zip file that contains a .cue file, or a .chd file.

1. A new playlist that consists of the audio tracks from the cue sheet will be
   opened and the first track will start playing.

1. You can only load one album at a time. When you load another, the current
   playlist is replaced with the tracks from the new album.

1. Click the **View** menu, then the view you want to switch to:
   currently playing, playlist, and lyrics.

## Usage (Console Player)

1. Open a terminal window.

1. For help on command line options, run:
   ```shell
   Woohoo.Discue.Cli -h
   ```

1. Run the executable and pass a path to a .cue file, a .zip file that contains
   a .cue file, or a .chd file.
   ```shell
   Woohoo.Discue.Cli "Life Is Strange - Before the Storm - Original Soundtrack (USA, Europe) (PS4 Game Bundle).zip"
   ```

1. Optionally pass in `-m` or `--metadata` to fetch metadata from CueToolsDB.
   ```shell
   Woohoo.Discue.Cli -m "Life Is Strange - Before the Storm - Original Soundtrack (USA, Europe) (PS4 Game Bundle).zip"
   ```

1. Optionally pass in `-l` or `--lyrics` to fetch lyrics from LRCLIB.net.
   Note that this requires metadata to be available for your tracks, either
   from CDTEXT in the cue file, or from CueToolsDB.
   ```shell
   Woohoo.Discue.Cli -m -l "Life Is Strange - Before the Storm - Original Soundtrack (USA, Europe) (PS4 Game Bundle).zip"
   ```

1. Optionally pass in `-ldb <path>` or `--lyrics-db <path>` to use a local LRCLIB
   database. A path to a .sqlite3 file must be provided. The latest database dump
   can be downloaded from [here](https://lrclib.net/db-dumps).
   When specified, lyrics will be fetched from the local database first, and fall
   back to the online service if no match is found locally.
   ```shell
   Woohoo.Discue.Cli -m -l -ldb "C:\path\to\lrclib.sqlite3" "Life Is Strange - Before the Storm - Original Soundtrack (USA, Europe) (PS4 Game Bundle).zip"
   ```

1. Press the following keys to control the player:
   - `Q` to quit.
   - `P` to pause playback.
   - `R` to resume playback.
   - `Up` to increase volume.
   - `Down` to decrease volume.
   - `Left` to go to the previous track.
   - `Right` to go to the next track.
   - `-` to seek backward.
   - `+` to seek forward.

## Building

Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

To build the application, use the following command from the `\src` folder:

```
dotnet build
```

To run the desktop player, use the following command from the `\src` folder:
```
dotnet run --project Woohoo.Discue.Avalonia.Desktop
```

To run the terminal UI player, use the following command from the `\src` folder:
```
dotnet run --project Woohoo.Discue.Consolonia
```

To run the unit tests, use the following command from the `\src` folder:

```
dotnet test
```

## License and Credits

This software is licensed under the MIT License. See the [LICENSE](LICENSE) file.

Copyright (c) 2025-2026 Hugues Valois. All rights reserved.

This software uses the following libraries:

- [Avalonia](https://github.com/AvaloniaUI/Avalonia)
- [CommunityToolkit](https://github.com/CommunityToolkit/dotnet)
- [FftSharp](https://github.com/swharden/FftSharp)
- [FluentIcons.Avalonia](https://github.com/davidxuang/FluentIcons)
- [ScottPlot](https://github.com/ScottPlot/ScottPlot)
- [SDL3-CS from ppy](https://github.com/ppy/SDL3-CS)
- [SDL3-CS from flibitijibibo](https://github.com/flibitijibibo/SDL3-CS)
- [SDL3](https://github.com/libsdl-org/SDL)
- [ZstdSharp.Port](https://github.com/oleg-st/ZstdSharp)

This software uses assets from:

- [icons-icons.com](https://icon-icons.com/)
  - [Disc Icon Free](https://icon-icons.com/icon/disc/114465)

This software queries metadata from:

- [CueToolsDB](http://db.cue.tools/)

This software queries lyrics from:

- [LRCLIB](https://lrclib.net)

Additional material is listed in [ThirdPartyNotices.txt](ThirdPartyNotices.txt).
