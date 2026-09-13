using VDFParser;
using VDFParser.Models;

namespace Steamedpass.Core.Steam;

/// <summary>
/// Reads, updates and backs up a Steam user's shortcuts.vdf (non-Steam games).
/// Adapted from UWPHook's SteamManager + GamesWindow.ExportGames
/// (https://github.com/BrianLima/UWPHook), MIT License, Copyright (c) 2016 Brian Lima.
/// </summary>
public static class SteamShortcuts
{
    public static VDFEntry[] ReadShortcuts(string userDataDirectory)
    {
        string shortcutFile = Path.Combine(userDataDirectory, "config", "shortcuts.vdf");

        if (!File.Exists(shortcutFile))
        {
            return Array.Empty<VDFEntry>();
        }

        try
        {
            return global::VDFParser.VDFParser.Parse(shortcutFile);
        }
        catch (VDFTooShortException)
        {
            return Array.Empty<VDFEntry>();
        }
    }

    /// <summary>
    /// Adds a new shortcut, or overwrites one whose AppName+Exe already match, and
    /// writes the result back to disk (after backing up the previous file).
    /// </summary>
    public static void AddOrUpdateShortcut(string userDataDirectory, VDFEntry entry)
    {
        VDFEntry[] shortcuts = ReadShortcuts(userDataDirectory);

        bool updated = false;
        for (int i = 0; i < shortcuts.Length; i++)
        {
            if (shortcuts[i].AppName == entry.AppName && shortcuts[i].Exe == entry.Exe)
            {
                entry.Index = shortcuts[i].Index;
                shortcuts[i] = entry;
                updated = true;
                break;
            }
        }

        if (!updated)
        {
            entry.Index = shortcuts.Length;
            Array.Resize(ref shortcuts, shortcuts.Length + 1);
            shortcuts[^1] = entry;
        }

        string configDirectory = Path.Combine(userDataDirectory, "config");
        Directory.CreateDirectory(configDirectory);

        BackupShortcuts(userDataDirectory);

        string shortcutFile = Path.Combine(configDirectory, "shortcuts.vdf");
        File.WriteAllBytes(shortcutFile, VDFSerializer.Serialize(shortcuts));
    }

    private static void BackupShortcuts(string userDataDirectory)
    {
        string sourceFile = Path.Combine(userDataDirectory, "config", "shortcuts.vdf");
        if (!File.Exists(sourceFile))
        {
            return;
        }

        string backupFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "steamedpass", "backups");
        Directory.CreateDirectory(backupFolder);

        string userId = Path.GetFileName(userDataDirectory.TrimEnd(Path.DirectorySeparatorChar));
        string backupFile = Path.Combine(backupFolder, $"{userId}_{DateTime.Now:yyyyMMddHHmmss}_shortcuts.vdf");

        File.Copy(sourceFile, backupFile, overwrite: true);
    }
}
