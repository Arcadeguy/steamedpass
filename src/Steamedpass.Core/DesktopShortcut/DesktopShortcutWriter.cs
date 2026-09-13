namespace Steamedpass.Core.DesktopShortcut;

/// <summary>
/// Authors the desktop .url shortcut for a Steam entry directly, bypassing
/// Steam's own "Add desktop shortcut" feature - which leaves IconFile pointing
/// at an .ico Steam never generates for non-Steam shortcuts, hence the blank icon.
/// </summary>
public static class DesktopShortcutWriter
{
    /// <param name="gameName">Used only for the .url file's name on the Desktop.</param>
    /// <param name="shortcutId64">The 64-bit id used by steam://rungameid/.</param>
    /// <param name="iconIcoPath">Absolute path to a real .ico file, or null to leave the icon unset.</param>
    public static string Write(string gameName, ulong shortcutId64, string? iconIcoPath)
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string fileName = string.Concat(gameName.Split(Path.GetInvalidFileNameChars())) + ".url";
        string path = Path.Combine(desktop, fileName);

        var lines = new List<string>
        {
            "[InternetShortcut]",
            $"URL=steam://rungameid/{shortcutId64}",
            "IconIndex=0",
        };

        if (!string.IsNullOrEmpty(iconIcoPath))
        {
            lines.Add($"IconFile={iconIcoPath}");
        }

        File.WriteAllLines(path, lines);
        return path;
    }
}
