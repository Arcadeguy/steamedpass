using System.Drawing;

namespace Steamedpass.Core.Icons;

/// <summary>
/// Resolves the icon used for the shortcuts.vdf "icon" field (Steam's own UI,
/// distinct from the desktop shortcut icon). Adapted from UWPHook's
/// AppEntry.widestSquareIcon + GamesWindow.PersistAppIcon
/// (https://github.com/BrianLima/UWPHook), MIT License, Copyright (c) 2016 Brian Lima.
/// </summary>
public static class PackageIconResolver
{
    /// <summary>
    /// Scans a UWP package's logo directory for the widest square image (UWP
    /// packages ship several tile-scale assets), then copies it to a persistent
    /// app-local cache, since the source path can go stale across app updates.
    /// Returns an empty string if no usable icon was found.
    /// </summary>
    public static string ResolveAndPersist(string aumid, string logoDirectory)
    {
        string widest = FindWidestSquareIcon(logoDirectory);
        if (string.IsNullOrEmpty(widest))
        {
            return string.Empty;
        }

        try
        {
            string cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "steamedpass", "vdf-icons");
            Directory.CreateDirectory(cacheDir);

            string destination = Path.Combine(cacheDir, aumid.Replace('!', '_') + Path.GetFileName(widest));
            File.Copy(widest, destination, overwrite: true);
            return destination;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Some package folders under Program Files\WindowsApps have ACLs
            // that deny direct copies (UnauthorizedAccessException, which is
            // NOT an IOException) - fall back to the source path unmodified.
            return widest;
        }
    }

    private static string FindWidestSquareIcon(string logoDirectory)
    {
        var images = new List<string>();
        try
        {
            images.AddRange(Directory.GetFiles(logoDirectory, "*.png"));
            images.AddRange(Directory.GetFiles(logoDirectory, "*.jpg"));
            images.AddRange(Directory.GetFiles(logoDirectory, "*.jpeg"));
        }
        catch (Exception ex) when (ex is DirectoryNotFoundException or UnauthorizedAccessException)
        {
            return string.Empty;
        }

        string result = string.Empty;
        var widest = new Size(0, 0);

        foreach (string image in images)
        {
            Size size;
            try
            {
                using var img = Image.FromFile(image);
                size = img.Size;
            }
            catch
            {
                continue;
            }

            if (size.Width == size.Height && size.Height > widest.Height)
            {
                widest = size;
                result = image;
            }
        }

        return result;
    }
}
