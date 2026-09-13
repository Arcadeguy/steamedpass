using System.Text.Json;

namespace Steamedpass.Core.Settings;

/// <summary>
/// Small local, per-user settings file (not checked into source control).
/// </summary>
public sealed class SteamedpassSettings
{
    public string? SteamGridDbApiKey { get; set; }

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
