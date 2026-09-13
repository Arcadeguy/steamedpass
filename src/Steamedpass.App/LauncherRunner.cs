using Steamedpass.Core.Launch;

namespace Steamedpass.App;

/// <summary>
/// Runs when Steam launches this exe as a game's "Exe" (LaunchOptions =
/// "&lt;aumid&gt; &lt;executable&gt; [extra args]"). Activates the UWP app and blocks
/// until it exits, so Steam sees an accurate running/playtime state.
/// Adapted from UWPHook's GamesWindow.LauncherAsync (https://github.com/BrianLima/UWPHook),
/// MIT License, Copyright (c) 2016 Brian Lima.
/// </summary>
internal static class LauncherRunner
{
    public static async Task RunAsync(string[] args)
    {
        string aumid = args[0];
        string extraArgs = args.Length > 2 ? string.Join(" ", args.Skip(2)) : string.Empty;

        var launcher = new GameLauncher();
        launcher.Launch(aumid, extraArgs);

        while (launcher.IsRunning())
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}
