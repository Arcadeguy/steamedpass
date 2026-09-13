namespace Steamedpass.Core.Icons;

/// <summary>
/// Resolves where extracted game icons are permanently stored. The desktop
/// shortcut's IconFile keeps pointing here, so these files are never deleted.
/// </summary>
public static class IconStore
{
    public static string GetIconPath(ulong shortcutId64)
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "steamedpass", "icons");
        Directory.CreateDirectory(directory);

        return Path.Combine(directory, $"{shortcutId64}.ico");
    }
}
