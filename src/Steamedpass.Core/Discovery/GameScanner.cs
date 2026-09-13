using System.IO;
using System.Reflection;
using Steamedpass.Core.Scripting;

namespace Steamedpass.Core.Discovery;

/// <summary>
/// Scans the machine for installed Microsoft Store / Xbox Game Pass apps.
/// Adapted from UWPHook's AppManager.GetInstalledApps + ScriptManager.RunScript
/// (https://github.com/BrianLima/UWPHook), MIT License, Copyright (c) 2016 Brian Lima.
/// </summary>
public static class GameScanner
{
    public static IReadOnlyList<InstalledGame> GetInstalledGames()
    {
        string scriptText = ReadEmbeddedScript();
        string rawOutput = PowerShellRunner.Run(scriptText);

        var games = new List<InstalledGame>();
        foreach (string entry in rawOutput.Split(';'))
        {
            string cleaned = entry.Replace("\r", "").Replace("\n", "").Trim();
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                continue;
            }

            string[] values = cleaned.Split('|');
            if (values.Length < 4 || string.IsNullOrWhiteSpace(values[0]))
            {
                continue;
            }

            string logoDirectory = Path.GetDirectoryName(values[1]) ?? string.Empty;
            games.Add(new InstalledGame(values[0], logoDirectory, values[2], values[3]));
        }

        return games;
    }

    private static string ReadEmbeddedScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        string resourceName = assembly.GetManifestResourceNames()
            .First(name => name.EndsWith("GetInstalledGamesScript.ps1", StringComparison.Ordinal));

        using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
