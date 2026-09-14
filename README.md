# SteamedPass

Adds Microsoft Store / Xbox Game Pass games to Steam as non-Steam shortcuts,
and — unlike other tools — makes sure the **desktop shortcut** for each game
actually shows the right icon and the Steam Library shows real box art
instead of a blank tile. Add one game or a whole batch at once.

## Why

Tools like [UWPHook](https://github.com/BrianLima/UWPHook) already add Game
Pass games to Steam's library, and Steam displays them fine there. But when
you create a desktop shortcut for one of these non-Steam entries, Steam
leaves it with a **blank icon** — it never generates the `.ico` file its own
shortcut feature relies on for non-Steam games. SteamedPass fixes that by
pulling a real icon straight from the app's package and authoring the
desktop shortcut itself, plus restores Steam's Library grid art via
SteamGridDB.

## What it does (one click / one command)

1. Scans installed Game Pass / Microsoft Store apps.
2. Adds the selected app(s) to Steam's `shortcuts.vdf` as non-Steam games
   (closing and relaunching Steam once for the whole batch, even when adding
   several games at a time, so the change takes effect).
3. Downloads Library grid/hero/logo art from SteamGridDB for each game, if
   an API key is configured.
4. Extracts a proper icon directly from each app's AUMID (via the Shell API
   — no intermediate shortcut file needed) and saves it as a real `.ico`.
5. Authors each desktop shortcut (`.url`) itself, pointing `IconFile` at that
   `.ico`, bypassing Steam's own broken desktop-shortcut generation.

## Installation

SteamedPass is a portable executable — there's no installer. Windows 10/11
is required either way (this uses WPF and Win32 APIs and won't build or run
on other platforms).

### Option 1: Download a release (easiest)

1. Grab the latest `SteamedPass-<version>-win.zip` from the
   [Releases page](https://github.com/Arcadeguy/steamedpass/releases) and
   extract it anywhere.
2. Install the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
   if you don't already have it (the release download is framework-dependent,
   so it needs the runtime — not the full SDK — installed separately).
3. Make sure Steam is installed.
4. Run `steamedpass.exe` from the extracted folder.

### Option 2: Build from source

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
2. Make sure Steam is installed.
3. Clone the repo and build it:

   ```
   git clone https://github.com/Arcadeguy/steamedpass.git
   cd steamedpass
   dotnet build Steamedpass.slnx
   ```

4. Run it from `src/Steamedpass.App/bin/Debug/net10.0-windows/steamedpass.exe`.

Either way, keep the exe at a stable path once you've added games with it —
Steam launches games *through* this same exe (see "How launching works"
below), so moving or deleting it breaks any shortcuts you've already
created.

Optional: get a free [SteamGridDB API key](https://www.steamgriddb.com/profile/preferences/api)
and paste it into Settings to enable Library grid/hero/logo art.

## Using the GUI

Run `steamedpass.exe` with no arguments. The games grid shows each app's
icon and whether it's already been added to Steam, and scans in the
background with a progress indicator (on launch and whenever you click
**Refresh**) rather than freezing the window. Check the box next to one or
more games (or use the header checkbox to select/deselect all) and click
**Add Selected to Steam** — Steam is only restarted once no matter how many
games you selected. Click **Settings** to configure the SteamGridDB API key,
language/resolution overrides, log level, and other options (see below).

## Using the CLI

```
steamedpass list
steamedpass add --name "Halo Infinite"
steamedpass add --name "Halo Infinite" --name "Forza Horizon 5"
steamedpass add --aumid <AUMID> --aumid <another AUMID>
steamedpass add --all
steamedpass config --steamgriddb-key <key>
```

`--name` and `--aumid` are repeatable and can be mixed in one `add` call;
`--all` adds every installed Game Pass app. Either way, all selected games
are written to `shortcuts.vdf` and Steam is restarted once for the whole
batch.

`add` runs the exact same pipeline as the GUI's one-click action.

## Settings

Stored locally at `%LocalAppData%\steamedpass\settings.json` (never checked
into source control). Options mirror UWPHook's settings page:

| Setting | Notes |
|---|---|
| SteamGridDB API key + Style/Type/NSFW/Humor filters | Controls Library grid art downloads |
| Create desktop shortcut | On by default; extracts a real icon and authors the `.url` shortcut |
| Theme | System / Light / Dark |
| Log level | Error / Debug / Trace, written to `%AppData%\steamedpass\application.log` |
| Poll seconds | How often to check whether a launched game is still running |
| Stream mode | Shows a full-screen cover window for ~10s before launch |
| Change language | Overrides the game's UI language for the session, restores after |
| Change resolution | Changes the display resolution before launch (off by default, like UWPHook's own setting) |

Note: UWPHook's resolution-change feature calls a `Set-DisplayResolution`
PowerShell cmdlet that doesn't actually exist in Windows or ship with
UWPHook, so it silently no-ops there. SteamedPass implements this natively
via the Win32 display API instead, so if you enable it, it actually works.

The Settings window's About section also shows the current app version and
a link back to this repository.

## How launching works

Steam can't launch an arbitrary AUMID directly, so the shortcut's `Exe`
points back at `steamedpass.exe` itself with the AUMID and executable
name as launch arguments. When invoked that way, it activates the app via
the same Windows API Explorer uses (`IApplicationActivationManager`, which
also returns a real process ID so Steam's playtime/running-state tracking
works) and blocks until the game exits.

## Project layout

- `src/Steamedpass.Core` — discovery, `shortcuts.vdf` read/write, icon
  extraction, SteamGridDB client, desktop shortcut writer, settings, and the
  `AddGamePipeline` that ties it all together. No UI dependencies.
- `src/Steamedpass.App` — WPF GUI, CLI, and the launcher-stub entry point.

See [CHANGELOG.md](CHANGELOG.md) for what's changed release to release.

## Credits

Vendors and adapts code from [UWPHook](https://github.com/BrianLima/UWPHook)
(discovery, `shortcuts.vdf` handling, UWP launch activation) and
[VDFParser](https://github.com/BrianLima/VDFParser) (binary `shortcuts.vdf`
parsing), both MIT licensed. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)
for full attribution.
