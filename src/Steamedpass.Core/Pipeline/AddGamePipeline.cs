using Steamedpass.Core.DesktopShortcut;
using Steamedpass.Core.Discovery;
using Steamedpass.Core.GridArt;
using Steamedpass.Core.Icons;
using Steamedpass.Core.Settings;
using Steamedpass.Core.Steam;
using VDFParser.Models;

namespace Steamedpass.Core.Pipeline;

/// <summary>
/// The one-click flow: add one or more Game Pass games to Steam as non-Steam
/// shortcuts, restart Steam once so the entries are registered, extract a
/// proper icon from each AUMID, and author desktop shortcuts that actually
/// show them.
/// </summary>
public static class AddGamePipeline
{
    public static Task<AddGamesResult> RunAsync(InstalledGame game, string steamedpassExePath, SteamedpassSettings settings) =>
        RunAsync(new[] { game }, steamedpassExePath, settings);

    public static async Task<AddGamesResult> RunAsync(
        IReadOnlyList<InstalledGame> games, string steamedpassExePath, SteamedpassSettings settings)
    {
        if (games.Count == 0)
        {
            throw new ArgumentException("Specify at least one game to add.", nameof(games));
        }

        string[] tags = settings.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string? steamFolder = SteamPaths.GetSteamFolder();
        if (steamFolder is null)
        {
            throw new InvalidOperationException("Could not locate a Steam installation.");
        }

        string[] userDataDirectories = SteamPaths.GetUserDataDirectories(steamFolder);
        if (userDataDirectories.Length == 0)
        {
            throw new InvalidOperationException("No Steam accounts found on this machine (steamapps/userdata is empty).");
        }

        var prepared = new List<(InstalledGame Game, VDFEntry Entry, int LegacyAppId, ulong ShortcutId64)>(games.Count);

        foreach (InstalledGame game in games)
        {
            int legacyAppId = SteamAppId.ComputeLegacyAppId(steamedpassExePath, game.Name);
            ulong shortcutId64 = SteamAppId.ComputeShortcutId64(steamedpassExePath, game.Name);
            string vdfIcon = PackageIconResolver.ResolveAndPersist(game.Aumid, game.LogoDirectory);

            var entry = new VDFEntry
            {
                appid = legacyAppId,
                AppName = game.Name,
                Exe = steamedpassExePath,
                StartDir = Path.GetDirectoryName(steamedpassExePath) ?? string.Empty,
                LaunchOptions = $"{game.Aumid} {game.Executable}",
                Icon = vdfIcon,
                ShortcutPath = string.Empty,
                AllowDesktopConfig = 1,
                AllowOverlay = 1,
                IsHidden = 0,
                OpenVR = 0,
                Devkit = 0,
                DevkitGameID = string.Empty,
                LastPlayTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Tags = tags,
            };

            prepared.Add((game, entry, legacyAppId, shortcutId64));
        }

        // One read-modify-write pass per Steam user, covering every game in the batch,
        // so shortcuts.vdf is only backed up/rewritten once regardless of batch size.
        foreach (string userDataDirectory in userDataDirectories)
        {
            SteamShortcuts.AddOrUpdateShortcuts(userDataDirectory, prepared.Select(p => p.Entry));
        }

        bool restarted = await SteamProcess.RestartAsync(steamFolder);

        var outcomes = new List<AddGameOutcome>(prepared.Count);

        foreach (var (game, _, legacyAppId, shortcutId64) in prepared)
        {
            bool gridArtInstalled = await GridArtInstaller.TryInstallAsync(
                settings, game.Name, unchecked((uint)legacyAppId), shortcutId64, userDataDirectories);

            string? desktopIconPath = null;
            string? desktopShortcutPath = null;

            if (settings.CreateDesktopShortcut)
            {
                using (var extractedIcon = IconExtractor.TryExtractIcon(game.Aumid))
                {
                    if (extractedIcon is not null)
                    {
                        desktopIconPath = IconStore.GetIconPath(shortcutId64);
                        IcoEncoder.SaveAsIco(extractedIcon, desktopIconPath);
                    }
                }

                desktopShortcutPath = DesktopShortcutWriter.Write(game.Name, shortcutId64, desktopIconPath);
            }

            outcomes.Add(new AddGameOutcome(
                game,
                new AddGameResult(
                    AddedToSteam: true,
                    DesktopIconExtracted: desktopIconPath is not null,
                    DesktopShortcutPath: desktopShortcutPath,
                    GridArtInstalled: gridArtInstalled)));
        }

        return new AddGamesResult(restarted, outcomes);
    }
}
