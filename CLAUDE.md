# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A Windows WPF + CLI tool that adds Microsoft Store / Xbox Game Pass games to
Steam as non-Steam shortcuts. Unlike UWPHook (which it's partly adapted
from), it also fixes Steam's broken desktop-shortcut icon generation and
restores Steam Library grid art via SteamGridDB. See README.md for the
full feature description, settings table, and launch-flow explanation.

## Build

```
dotnet build Steamedpass.slnx
```

Output: `src/Steamedpass.App/bin/Debug/net10.0-windows/steamedpass.exe`.
Requires .NET 10 SDK and Windows (WPF, `net10.0-windows` TFM, and Vanara
Win32 P/Invoke wrappers make this non-portable to other OSes). There is no
test project in this repo.

## Running

```
steamedpass.exe                                   # launches the WPF GUI
steamedpass list
steamedpass add --name "Halo Infinite"
steamedpass add --aumid <AUMID>
steamedpass config --steamgriddb-key <key>
```

The same exe is also invoked *by Steam* to launch games — see "Three entry
points" below.

## Architecture

### Project layout

- `src/Steamedpass.Core` — all logic, no UI dependencies: discovery,
  `shortcuts.vdf` read/write, icon extraction, SteamGridDB client, desktop
  shortcut writer, settings, and `Pipeline/AddGamePipeline.cs`, which is the
  single orchestrator both the GUI and CLI call into.
- `src/Steamedpass.App` — WPF GUI (`MainWindow`, `SettingsWindow`,
  `LaunchingOverlayWindow`), the CLI (`CliRunner`), and the launcher stub
  (`LauncherRunner`).
- `src/Steamedpass.Core/ThirdParty/VDFParser` — vendored binary
  `shortcuts.vdf` parser/serializer from BrianLima/VDFParser (MIT). Treat as
  third-party code; don't restyle it gratuitously.

### Three entry points, one exe (`App.xaml.cs`)

`OnStartup` branches on `args` to decide which of three unrelated things
this process invocation is for:

1. **Game launch stub**: `args[0]` contains `!` (an AUMID always does) →
   `LauncherRunner.RunAsync`. This is how Steam itself invokes the exe when
   the user clicks Play — see "How launching works" in README.md. It must
   activate the UWP app, block until it exits, and return, so Steam's
   playtime/running-state tracking works.
2. **CLI**: `args[0]` is `add`/`list`/`config` → `CliRunner.RunAsync`.
3. **GUI**: no args (or unrecognized) → normal WPF `MainWindow`.

When changing argument parsing/handling, check all three paths — they share
one process and one `Main`.

### The add-game pipeline (`Steamedpass.Core/Pipeline/AddGamePipeline.cs`)

Single async method both `CliRunner` and the GUI call, taking a *batch* of
one or more `InstalledGame`s (the GUI's grid has a checkbox column so
multiple apps can be selected at once, à la UWPHook), in this order:
for every game, compute Steam's CRC32-based app ids (`SteamAppId`) and
resolve/persist a VDF icon (`PackageIconResolver`) → write all the games'
shortcut entries into every Steam user's `shortcuts.vdf` in one
read-modify-write pass per user (`SteamShortcuts.AddOrUpdateShortcuts`, one
per `userdata/<id>` directory) → restart Steam once for the whole batch
(`SteamProcess.RestartAsync`) so it picks up the change → per game, install
SteamGridDB grid art (`GridArtInstaller`, best-effort/optional) and extract
a real icon straight from the AUMID via Shell API (`IconExtractor` +
`IcoEncoder`) and author a `.url` desktop shortcut
(`DesktopShortcutWriter`), if enabled in settings. Keep new steps in this
same order-dependent flow rather than parallelizing them — later steps
depend on ids/state computed earlier (e.g. every shortcut must exist before
Steam is restarted; the exe path baked into each shortcut is
`steamedpassExePath`, i.e. this same running exe). `AddGamePipeline.RunAsync`
returns an `AddGamesResult` (one `SteamRestarted` flag for the batch, plus a
per-game `AddGameOutcome` list) rather than a per-game restart flag, since
Steam is only restarted once regardless of batch size.

### Steam app-id scheme (`Steamedpass.Core/Steam/SteamAppId.cs`)

Non-Steam shortcuts don't have a real Steam app id, so Steam (and this tool)
derive one deterministically from `CRC32(exe + appName)`. The same 32-bit
value is reused, shifted/masked two different ways, for two different
purposes: the legacy `int` `appid` field in `shortcuts.vdf`, and the 64-bit
id used in `steam://rungameid/...` URLs and grid/icon cache filenames under
`config/grid/`. Both must be derived from the exact same `(exe, appName)`
pair used when the shortcut was written, or Steam won't associate icons/art
with the right entry.

### Icon handling — two independent icon paths

Don't conflate these; they're extracted differently and serve different UI:

- **VDF icon** (`PackageIconResolver`) — the icon shown in Steam's own
  library list/grid for the shortcut, resolved from the package's manifest
  logo.
- **Desktop shortcut icon** (`IconExtractor` + `IcoEncoder`) — extracted via
  `IShellItemImageFactory` against `shell:AppsFolder\<AUMID>` (the same
  mechanism Explorer uses), converted from premultiplied to straight alpha,
  and encoded as a real `.ico`. This is the fix for Steam's own
  desktop-shortcut feature, which never generates a `.ico` for non-Steam
  games and leaves a blank icon. `IconExtractor.TryExtractIcon` returns
  `null` on failure — callers must leave the destination icon unset rather
  than show a wrong one, not throw or fall back to a placeholder.

### Settings

`SteamedpassSettings` (`Steamedpass.Core/Settings`) is a flat JSON blob at
`%LocalAppData%\steamedpass\settings.json`, loaded fresh at each entry point
(GUI startup, CLI `add`/`config`, launcher stub) rather than shared as a
singleton. `Load()` swallows all errors and falls back to field defaults —
preserve that behavior (a missing/corrupt settings file must never crash
startup). Logs go to `%AppData%\steamedpass\application.log` via Serilog,
level controlled by `settings.LogLevel`.

### Vendored/adapted code

`SteamAppId`, `LauncherRunner`, parts of discovery, and shortcuts.vdf
handling are adapted from UWPHook (MIT); `ThirdParty/VDFParser` is vendored
from BrianLima/VDFParser (MIT). See THIRD-PARTY-NOTICES.md for full
attribution — preserve existing attribution comments when touching this
code, and add matching attribution for any further code ported from those
projects.
