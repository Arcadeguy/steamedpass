using System.Diagnostics;

namespace Steamedpass.Core.Steam;

/// <summary>
/// Closes and relaunches Steam around a shortcuts.vdf write, since Steam holds the
/// file in memory and overwrites external edits on exit.
/// Adapted from UWPHook's GamesWindow.RestartSteam (https://github.com/BrianLima/UWPHook),
/// MIT License, Copyright (c) 2016 Brian Lima.
/// </summary>
public static class SteamProcess
{
    /// <param name="steamFolder">Steam's install folder (from SteamPaths.GetSteamFolder()).</param>
    public static async Task<bool> RestartAsync(string steamFolder, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(8);

        if (!Process.GetProcessesByName("steam").Any())
        {
            return true;
        }

        // Built from the known install folder rather than Process.MainModule.FileName:
        // querying a running process's module info requires OpenProcess access that
        // Windows denies when Steam runs at a higher integrity level (elevated) than
        // this process, or when security software is guarding it.
        string steamExe = Path.Combine(steamFolder, "steam.exe");

        Process.Start(steamExe, "-exitsteam");

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout.Value)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            if (!Process.GetProcessesByName("steam").Any())
            {
                Process.Start(steamExe);
                return true;
            }
        }

        return false;
    }
}
