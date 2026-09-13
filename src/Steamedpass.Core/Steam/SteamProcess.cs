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
    public static async Task<bool> RestartAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(8);

        Process? steam = Process.GetProcessesByName("steam").SingleOrDefault();
        if (steam is null)
        {
            return true;
        }

        string steamExe = steam.MainModule!.FileName!;

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
