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
    public static void AddOrUpdateShortcut(string userDataDirectory, VDFEntry entry) =>
        AddOrUpdateShortcuts(userDataDirectory, new[] { entry });

    /// <summary>
    /// Adds/overwrites several shortcuts in one read-modify-write pass, so adding
    /// multiple games only backs up and rewrites shortcuts.vdf once.
    /// </summary>
    public static void AddOrUpdateShortcuts(string userDataDirectory, IEnumerable<VDFEntry> entries)
    {
        VDFEntry[] shortcuts = ReadShortcuts(userDataDirectory);

        foreach (VDFEntry entry in entries)
        {
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
        }

        string configDirectory = Path.Combine(userDataDirectory, "config");
        Directory.CreateDirectory(configDirectory);

        BackupShortcuts(userDataDirectory);

        string shortcutFile = Path.Combine(configDirectory, "shortcuts.vdf");
        File.WriteAllBytes(shortcutFile, VDFSerializer.Serialize(shortcuts));
    }

    /// <summary>
    /// Removes a single shortcut matching AppName+Exe, if present, after backing up
    /// the previous file. Returns true if an entry was found and removed.
    /// </summary>
    public static bool RemoveShortcut(string userDataDirectory, string appName, string exe)
    {
        VDFEntry[] shortcuts = ReadShortcuts(userDataDirectory);
        VDFEntry[] remaining = shortcuts.Where(s => !(s.AppName == appName && s.Exe == exe)).ToArray();

        if (remaining.Length == shortcuts.Length)
        {
            return false;
        }

        for (int i = 0; i < remaining.Length; i++)
        {
            remaining[i].Index = i;
        }

        string configDirectory = Path.Combine(userDataDirectory, "config");
        Directory.CreateDirectory(configDirectory);

        BackupShortcuts(userDataDirectory);

        string shortcutFile = Path.Combine(configDirectory, "shortcuts.vdf");
        File.WriteAllBytes(shortcutFile, VDFSerializer.Serialize(remaining));
        return true;
    }

    /// <summary>
    /// Removes ALL non-Steam shortcuts for a user (not just ones steamedpass added),
    /// after backing up the previous file. Matches UWPHook's "Clear All" maintenance
    /// action - callers must confirm with the user before invoking this.
    /// </summary>
    public static void ClearAllShortcuts(string userDataDirectory)
    {
        string configDirectory = Path.Combine(userDataDirectory, "config");
        Directory.CreateDirectory(configDirectory);

        BackupShortcuts(userDataDirectory);

        string shortcutFile = Path.Combine(configDirectory, "shortcuts.vdf");
        File.WriteAllBytes(shortcutFile, VDFSerializer.Serialize(Array.Empty<VDFEntry>()));
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
