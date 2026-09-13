namespace Steamedpass.Core.GridArt;

/// <summary>
/// Downloads SteamGridDB artwork for a game and installs it into every Steam
/// user's grid cache. Steam has used two different id conventions for these
/// filenames across versions (the plain 32-bit shortcut id, and a 64-bit
/// "extended" id) - we write both, so whichever one the installed Steam
/// client reads is present. Best-effort: any failure here should not break
/// the rest of the add-to-Steam pipeline.
/// </summary>
public static class GridArtInstaller
{
    public static async Task<bool> TryInstallAsync(
        string apiKey, string gameName, uint legacyAppId, ulong shortcutId64, string[] userDataDirectories)
    {
        try
        {
            var client = new SteamGridDbClient(apiKey);
            SteamGridDbGame[] matches = await client.SearchGameAsync(gameName);
            if (matches.Length == 0)
            {
                return false;
            }

            int gameId = matches[0].Id;

            SteamGridDbImage[] verticalGrids = await client.GetGridsAsync(gameId, "600x900,342x482,660x930");
            SteamGridDbImage[] horizontalGrids = await client.GetGridsAsync(gameId, "460x215,920x430");
            SteamGridDbImage[] heroes = await client.GetHeroesAsync(gameId);
            SteamGridDbImage[] logos = await client.GetLogosAsync(gameId);

            using var httpClient = new HttpClient();

            async Task DownloadAndInstall(SteamGridDbImage[] images, string suffix)
            {
                if (images.Length == 0)
                {
                    return;
                }

                byte[] bytes = await httpClient.GetByteArrayAsync(images[0].Url);

                foreach (string userDataDirectory in userDataDirectories)
                {
                    string gridDirectory = Path.Combine(userDataDirectory, "config", "grid");
                    Directory.CreateDirectory(gridDirectory);

                    await File.WriteAllBytesAsync(Path.Combine(gridDirectory, $"{legacyAppId}{suffix}"), bytes);
                    await File.WriteAllBytesAsync(Path.Combine(gridDirectory, $"{shortcutId64}{suffix}"), bytes);
                }
            }

            await Task.WhenAll(
                DownloadAndInstall(horizontalGrids, ".png"),
                DownloadAndInstall(verticalGrids, "p.png"),
                DownloadAndInstall(heroes, "_hero.png"),
                DownloadAndInstall(logos, "_logo.png"));

            return true;
        }
        catch
        {
            return false;
        }
    }
}
