namespace Steamedpass.Core.Pipeline;

public sealed record AddGameResult(
    bool AddedToSteam,
    bool SteamRestarted,
    bool DesktopIconExtracted,
    string? DesktopShortcutPath,
    bool GridArtInstalled);
