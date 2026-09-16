using Serilog;
using Steamedpass.App.Updates;
using Steamedpass.Core.Discovery;
using Steamedpass.Core.Pipeline;
using Steamedpass.Core.Settings;
using Velopack;

namespace Steamedpass.App;

/// <summary>
/// Headless CLI entry point: "steamedpass add --name &lt;name&gt;" or
/// "steamedpass add --aumid &lt;aumid&gt;", and "steamedpass list". Runs the same
/// pipeline as the GUI's one-click action.
/// </summary>
internal static class CliRunner
{
    public static async Task<int> RunAsync(string[] args)
    {
        NativeConsole.Attach();

        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "list":
                return RunList();
            case "add":
                return await RunAddAsync(args.Skip(1).ToArray());
            case "config":
                return RunConfig(args.Skip(1).ToArray());
            case "update":
                return await RunUpdateAsync();
            default:
                PrintUsage();
                return 1;
        }
    }

    private static int RunList()
    {
        IReadOnlyList<InstalledGame> games = GameScanner.GetInstalledGames();
        foreach (InstalledGame game in games)
        {
            Console.WriteLine($"{game.Name}\t{game.Aumid}");
        }

        Console.WriteLine($"{games.Count} game(s) found.");
        return 0;
    }

    private static async Task<int> RunAddAsync(string[] args)
    {
        bool all = args.Any(a => string.Equals(a, "--all", StringComparison.OrdinalIgnoreCase));
        string[] names = GetArgValues(args, "--name");
        string[] aumids = GetArgValues(args, "--aumid");

        if (!all && names.Length == 0 && aumids.Length == 0)
        {
            Console.Error.WriteLine("Specify --name \"<game name>\" (repeatable), --aumid <AUMID> (repeatable), or --all.");
            return 1;
        }

        IReadOnlyList<InstalledGame> installedGames = GameScanner.GetInstalledGames();
        var games = new List<InstalledGame>();

        if (all)
        {
            games.AddRange(installedGames);
        }
        else
        {
            foreach (string aumid in aumids)
            {
                InstalledGame? match = installedGames.FirstOrDefault(g => string.Equals(g.Aumid, aumid, StringComparison.OrdinalIgnoreCase));
                if (match is null)
                {
                    Console.Error.WriteLine($"No installed Game Pass app matched AUMID '{aumid}'.");
                    return 1;
                }

                games.Add(match);
            }

            foreach (string name in names)
            {
                InstalledGame? match = installedGames.FirstOrDefault(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));
                if (match is null)
                {
                    Console.Error.WriteLine($"No installed Game Pass app matched name '{name}'. Run 'steamedpass list' to see installed apps.");
                    return 1;
                }

                games.Add(match);
            }
        }

        games = games.Distinct().ToList();

        if (games.Count == 0)
        {
            Console.Error.WriteLine("No installed Game Pass apps matched.");
            return 1;
        }

        Console.WriteLine($"Adding {games.Count} app(s) to Steam: {string.Join(", ", games.Select(g => g.Name))}");

        try
        {
            string exePath = Environment.ProcessPath!;
            SteamedpassSettings settings = SteamedpassSettings.Load();
            AddGamesResult result = await AddGamePipeline.RunAsync(games, exePath, settings);

            Console.WriteLine($"Steam restarted: {result.SteamRestarted}");
            foreach (AddGameOutcome outcome in result.Games)
            {
                string desktopShortcutStatus = outcome.Result.DesktopShortcutPath is null
                    ? "skipped (disabled in settings)"
                    : $"{outcome.Result.DesktopShortcutPath} (icon extracted: {outcome.Result.DesktopIconExtracted})";

                Console.WriteLine(
                    $"- {outcome.Game.Name}: added={outcome.Result.AddedToSteam}, " +
                    $"grid art installed={outcome.Result.GridArtInstalled}, desktop shortcut={desktopShortcutStatus}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add game(s) to Steam");
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine("See %AppData%\\steamedpass\\application.log for details.");
            return 1;
        }
    }

    private static int RunConfig(string[] args)
    {
        string? apiKey = GetArgValue(args, "--steamgriddb-key");
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.Error.WriteLine("Usage: steamedpass config --steamgriddb-key <key>");
            return 1;
        }

        var settings = SteamedpassSettings.Load();
        settings.SteamGridDbApiKey = apiKey;
        settings.Save();

        Console.WriteLine("SteamGridDB API key saved.");
        return 0;
    }

    private static async Task<int> RunUpdateAsync()
    {
        Console.WriteLine("Checking for updates...");

        UpdateInfo? update = await UpdateService.CheckAsync();
        if (update is null)
        {
            Console.WriteLine("SteamedPass is up to date.");
            return 0;
        }

        Console.WriteLine($"Downloading v{update.TargetFullRelease.Version}...");
        bool applied = await UpdateService.DownloadAndApplyAsync(
            update, percent => Console.Write($"\r{percent}%   "));

        // ApplyUpdatesAndRestart replaces the process on success; only reachable on failure.
        Console.WriteLine();
        Console.Error.WriteLine("Update failed. See %AppData%\\steamedpass\\application.log for details.");
        return applied ? 0 : 1;
    }

    private static string? GetArgValue(string[] args, string flag)
    {
        int index = Array.IndexOf(args, flag);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static string[] GetArgValues(string[] args, string flag)
    {
        var values = new List<string>();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
            {
                values.Add(args[i + 1]);
            }
        }

        return values.ToArray();
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  steamedpass list");
        Console.WriteLine("  steamedpass add --name \"<game name>\" [--name \"<another game>\" ...]");
        Console.WriteLine("  steamedpass add --aumid <AUMID> [--aumid <another AUMID> ...]");
        Console.WriteLine("  steamedpass add --all");
        Console.WriteLine("  steamedpass config --steamgriddb-key <key>");
        Console.WriteLine("  steamedpass update");
    }
}
