using Steamedpass.Core.Discovery;
using Steamedpass.Core.Pipeline;
using Steamedpass.Core.Settings;

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
        string? name = GetArgValue(args, "--name");
        string? aumid = GetArgValue(args, "--aumid");

        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(aumid))
        {
            Console.Error.WriteLine("Specify --name \"<game name>\" or --aumid <AUMID>.");
            return 1;
        }

        IReadOnlyList<InstalledGame> games = GameScanner.GetInstalledGames();
        InstalledGame? game = !string.IsNullOrEmpty(aumid)
            ? games.FirstOrDefault(g => string.Equals(g.Aumid, aumid, StringComparison.OrdinalIgnoreCase))
            : games.FirstOrDefault(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));

        if (game is null)
        {
            Console.Error.WriteLine("No installed Game Pass app matched. Run 'steamedpass list' to see installed apps.");
            return 1;
        }

        Console.WriteLine($"Adding '{game.Name}' to Steam...");

        string exePath = Environment.ProcessPath!;
        AddGameResult result = await AddGamePipeline.RunAsync(game, exePath, tags: Array.Empty<string>());

        Console.WriteLine($"Added to Steam: {result.AddedToSteam}");
        Console.WriteLine($"Steam restarted: {result.SteamRestarted}");
        Console.WriteLine($"Desktop icon extracted: {result.DesktopIconExtracted}");
        Console.WriteLine($"Desktop shortcut: {result.DesktopShortcutPath}");
        Console.WriteLine($"Steam library grid art installed: {result.GridArtInstalled}");
        return 0;
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

    private static string? GetArgValue(string[] args, string flag)
    {
        int index = Array.IndexOf(args, flag);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  steamedpass list");
        Console.WriteLine("  steamedpass add --name \"<game name>\"");
        Console.WriteLine("  steamedpass add --aumid <AUMID>");
        Console.WriteLine("  steamedpass config --steamgriddb-key <key>");
    }
}
