using System.Text.Json;

namespace Steamedpass.Core.Settings;

/// <summary>
/// Small local, per-user settings file (not checked into source control).
/// Options mirror UWPHook's SettingsWindow (https://github.com/BrianLima/UWPHook),
/// MIT License, Copyright (c) 2016 Brian Lima.
/// </summary>
public sealed class SteamedpassSettings
{
    public string? SteamGridDbApiKey { get; set; }

    /// <summary>Index into SteamGridDbOptions.Styles.</summary>
    public int SteamGridDbStyle { get; set; }

    /// <summary>Index into SteamGridDbOptions.Types.</summary>
    public int SteamGridDbType { get; set; }

    /// <summary>Index into SteamGridDbOptions.Nsfw.</summary>
    public int SteamGridDbNsfw { get; set; }

    /// <summary>Index into SteamGridDbOptions.Humor.</summary>
    public int SteamGridDbHumor { get; set; }

    /// <summary>Comma-separated Steam category tags applied to every shortcut.</summary>
    public string Tags { get; set; } = "GAMEPASS";

    /// <summary>0 = Error, 1 = Debug, 2 = Verbose/Trace.</summary>
    public int LogLevel { get; set; }

    /// <summary>Seconds between checks of whether the launched game is still running.</summary>
    public int PollSeconds { get; set; } = 5;

    /// <summary>Shows a full-screen "launching" cover window for ~10s before activating the app.</summary>
    public bool StreamMode { get; set; }

    public bool ChangeLanguage { get; set; }

    /// <summary>A culture name, e.g. "en-US". Empty = current UI culture.</summary>
    public string TargetLanguage { get; set; } = string.Empty;

    public bool ChangeResolution { get; set; }

    /// <summary>"&lt;width&gt; x &lt;height&gt;". Empty = current display resolution.</summary>
    public string TargetResolution { get; set; } = string.Empty;

    /// <summary>Whether adding a game also authors a desktop shortcut for it.</summary>
    public bool CreateDesktopShortcut { get; set; } = true;

    /// <summary>"System", "Light", or "Dark".</summary>
    public string ThemeMode { get; set; } = "System";

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "steamedpass", "settings.json");

    public static SteamedpassSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<SteamedpassSettings>(json) ?? new SteamedpassSettings();
            }
        }
        catch
        {
            // Fall through to defaults if the file is missing/corrupt.
        }

        return new SteamedpassSettings();
    }

    public void Save()
    {
        string? directory = Path.GetDirectoryName(SettingsPath);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this));
    }
}
