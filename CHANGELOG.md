# Changelog

All notable changes to SteamedPass are documented in this file.

## [1.0.3] - 2026-09-14

### Changed

- Updated all NuGet dependencies to their latest versions, including
  `System.Security.Cryptography.Xml` (fixing a known vulnerability),
  Serilog and its sinks, `Microsoft.PowerShell.SDK` /
  `System.Management.Automation`, `System.Drawing.Common`, and the Vanara
  P/Invoke packages.

## [1.0.2] - 2026-09-13

### Added

- The games grid now shows each app's icon (resolved from its own package
  logo assets) and flags apps that already have a matching Steam shortcut,
  so it's easy to see what's already been added.
- Scanning installed apps (on launch and on **Refresh**) now runs in the
  background with a progress indicator instead of freezing the window.
- A GitHub Actions workflow builds and attaches a release archive
  automatically whenever a version tag is pushed.

### Removed

- The "Steam category tags" setting. Modern Steam no longer reads
  `shortcuts.vdf`'s `tags` field for categorization - it keeps that in a
  separate, undocumented Collections store - so the setting never actually
  did anything. Removed rather than leave a non-functional control in
  Settings.

## [1.0.0] - 2026-09-13

Initial release.

### Added

- Add Microsoft Store / Xbox Game Pass (UWP) games to Steam as non-Steam
  shortcuts, from a WPF GUI or a headless CLI (`steamedpass list`,
  `steamedpass add`, `steamedpass config`).
- Fix for Steam's broken desktop-shortcut icon generation: extracts a real
  icon straight from the AUMID via the Shell API and authors a working
  `.url` desktop shortcut, with a setting to enable/disable it.
- Steam Library grid art (grids, hero, logo) installed automatically via the
  SteamGridDB API, with style/type/NSFW/humor filters in Settings.
- Multi-app batch support: a checkbox column (with select-all) in the GUI
  grid, and repeatable `--name`/`--aumid` flags plus `--all` in the CLI, so
  several games are added in one pass with a single Steam restart instead
  of one per game.
- Settings page with SteamGridDB API key, category tags, desktop shortcut
  toggle, language/resolution override, log level, and a System/Light/Dark
  theme switch, restyled with WPF-UI's Fluent/Mica theme.
- Sortable, styled games grid with a SteamedPass icon/logo and title bar.
- Link to this repository, plus UWPHook/VDFParser attribution, in the
  Settings About section.

### Changed

- Renamed the output executable to `steamedpass.exe`.
- Default Steam category tag changed from `READY TO PLAY,XBOX` to
  `GAMEPASS`.

### Fixed

- "Access is denied" crash when adding some Game Pass games, caused by
  querying Steam's own process `MainModule` at a different integrity level;
  Steam's exe path is now read from its known install folder instead.

### Credits

Discovery, shortcut handling, and the launcher stub are adapted from
[UWPHook](https://github.com/BrianLima/UWPHook) (MIT); `shortcuts.vdf`
parsing is vendored from
[VDFParser](https://github.com/BrianLima/VDFParser) (MIT). See
THIRD-PARTY-NOTICES.md for full attribution.
