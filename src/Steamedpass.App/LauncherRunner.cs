using System.Globalization;
using Serilog;
using Steamedpass.Core.Display;
using Steamedpass.Core.Launch;
using Steamedpass.Core.Scripting;
using Steamedpass.Core.Settings;

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
        SteamedpassSettings settings = SteamedpassSettings.Load();
        string aumid = args[0];
        string extraArgs = args.Length > 2 ? string.Join(" ", args.Skip(2)) : string.Empty;
        string originalCulture = CultureInfo.CurrentCulture.Name;

        LaunchingOverlayWindow? overlay = null;
        if (settings.StreamMode)
        {
            overlay = new LaunchingOverlayWindow();
            overlay.Show();
            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        try
        {
            if (settings.ChangeLanguage && !string.IsNullOrEmpty(settings.TargetLanguage))
            {
                Log.Information("Overriding UI language to {Language}", settings.TargetLanguage);
                PowerShellRunner.Run($"Set-WinUILanguageOverride {settings.TargetLanguage}");
            }

            if (settings.ChangeResolution && !string.IsNullOrEmpty(settings.TargetResolution))
            {
                (int width, int height) = ParseResolution(settings.TargetResolution);
                Log.Information("Changing display resolution to {Width}x{Height}", width, height);
                DisplayResolution.TrySet(width, height);
            }

            overlay?.Close();

            var launcher = new GameLauncher();
            launcher.Launch(aumid, extraArgs);
            Log.Information("Launched {Aumid}", aumid);

            while (launcher.IsRunning())
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, settings.PollSeconds)));
            }
        }
        finally
        {
            overlay?.Close();

            if (settings.ChangeLanguage && !string.IsNullOrEmpty(settings.TargetLanguage))
            {
                PowerShellRunner.Run($"Set-WinUILanguageOverride {originalCulture}");
            }
        }
    }

    private static (int Width, int Height) ParseResolution(string resolution)
    {
        string[] parts = resolution.Split('x', StringSplitOptions.TrimEntries);
        if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
        {
            return (width, height);
        }

        throw new FormatException($"Invalid resolution format: '{resolution}'.");
    }
}
