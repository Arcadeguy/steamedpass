using Steamedpass.Core.Settings;

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
        SteamedpassSettings settings, string gameName, uint legacyAppId, ulong shortcutId64, string[] userDataDirectories)
    {
        if (string.IsNullOrWhiteSpace(settings.SteamGridDbApiKey))
        {
            return false;
        }

        try
        {
            var client = new SteamGridDbClient(settings.SteamGridDbApiKey);
            SteamGridDbGame[] matches = await client.SearchGameAsync(gameName);
            if (matches.Length == 0)
            {
                return false;
            }

            int gameId = matches[0].Id;
            string filterParams = SteamGridDbOptions.BuildQueryParameters(
                settings.SteamGridDbStyle, settings.SteamGridDbType, settings.SteamGridDbNsfw, settings.SteamGridDbHumor, dimensions: null);
            string verticalFilterParams = SteamGridDbOptions.BuildQueryParameters(
                settings.SteamGridDbStyle, settings.SteamGridDbType, settings.SteamGridDbNsfw, settings.SteamGridDbHumor, "600x900,342x482,660x930");
            string horizontalFilterParams = SteamGridDbOptions.BuildQueryParameters(
                settings.SteamGridDbStyle, settings.SteamGridDbType, settings.SteamGridDbNsfw, settings.SteamGridDbHumor, "460x215,920x430");

            SteamGridDbImage[] verticalGrids = await client.GetGridsAsync(gameId, "600x900,342x482,660x930", verticalFilterParams);
            SteamGridDbImage[] horizontalGrids = await client.GetGridsAsync(gameId, "460x215,920x430", horizontalFilterParams);
            SteamGridDbImage[] heroes = await client.GetHeroesAsync(gameId, filterParams);
            SteamGridDbImage[] logos = await client.GetLogosAsync(gameId, filterParams);

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
