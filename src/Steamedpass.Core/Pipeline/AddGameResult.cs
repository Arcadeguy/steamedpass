using Steamedpass.Core.Discovery;

namespace Steamedpass.Core.Pipeline;

/// <summary>Per-game outcome of a batch add (see <see cref="AddGamesResult"/>).</summary>
public sealed record AddGameResult(
    bool AddedToSteam,
    bool DesktopIconExtracted,
    string? DesktopShortcutPath,
    bool GridArtInstalled);

/// <summary>One game's result within a batch, paired with the game it came from.</summary>
public sealed record AddGameOutcome(InstalledGame Game, AddGameResult Result);

/// <summary>
/// Result of adding one or more games to Steam in a single pass. Steam is only
/// restarted once for the whole batch, so that's reported at the batch level
/// rather than per game.
/// </summary>
public sealed record AddGamesResult(
    bool SteamRestarted,
    IReadOnlyList<AddGameOutcome> Games);
