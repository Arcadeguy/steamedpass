using Steamedpass.Core.DesktopShortcut;
using Steamedpass.Core.Discovery;
using Steamedpass.Core.GridArt;
using Steamedpass.Core.Icons;
using Steamedpass.Core.Settings;
using Steamedpass.Core.Steam;
using VDFParser.Models;

namespace Steamedpass.Core.Pipeline;

/// <summary>
/// The one-click flow: add a Game Pass game to Steam as a non-Steam shortcut,
/// restart Steam so the entry is registered, extract a proper icon from the
/// AUMID, and author a desktop shortcut that actually shows it.
/// </summary>
public static class AddGamePipeline
{
    public static async Task<AddGameResult> RunAsync(InstalledGame game, string steamedpassExePath, string[] tags)
    {
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

        foreach (string userDataDirectory in userDataDirectories)
        {
            SteamShortcuts.AddOrUpdateShortcut(userDataDirectory, entry);
        }

        bool restarted = await SteamProcess.RestartAsync();

        bool gridArtInstalled = false;
        string? apiKey = SteamedpassSettings.Load().SteamGridDbApiKey;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            gridArtInstalled = await GridArtInstaller.TryInstallAsync(
                apiKey, game.Name, unchecked((uint)legacyAppId), shortcutId64, userDataDirectories);
        }

        string? desktopIconPath = null;
        using (var extractedIcon = IconExtractor.TryExtractIcon(game.Aumid))
        {
            if (extractedIcon is not null)
            {
                desktopIconPath = IconStore.GetIconPath(shortcutId64);
                IcoEncoder.SaveAsIco(extractedIcon, desktopIconPath);
            }
        }

        string desktopShortcutPath = DesktopShortcutWriter.Write(game.Name, shortcutId64, desktopIconPath);

        return new AddGameResult(
            AddedToSteam: true,
            SteamRestarted: restarted,
            DesktopIconExtracted: desktopIconPath is not null,
            DesktopShortcutPath: desktopShortcutPath,
            GridArtInstalled: gridArtInstalled);
    }
}
