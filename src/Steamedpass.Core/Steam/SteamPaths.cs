using Microsoft.Win32;

namespace Steamedpass.Core.Steam;

/// <summary>
/// Locates the Steam installation and per-user config folders.
/// Adapted from UWPHook's SteamManager (https://github.com/BrianLima/UWPHook),
/// MIT License, Copyright (c) 2016 Brian Lima.
/// </summary>
public static class SteamPaths
{
    public static string? GetSteamFolder()
    {
        const string registryPath = @"SOFTWARE\Valve\Steam";

        using (RegistryKey localKey64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
        using (RegistryKey? key64 = localKey64.OpenSubKey(registryPath))
        {
            if (key64?.GetValue("InstallPath") is string path64 && !string.IsNullOrEmpty(path64))
            {
                return path64;
            }
        }

        using (RegistryKey localKey32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
        using (RegistryKey? key32 = localKey32.OpenSubKey(registryPath))
        {
            if (key32?.GetValue("InstallPath") is string path32 && !string.IsNullOrEmpty(path32))
            {
                return path32;
            }
        }

        return null;
    }

    public static string[] GetUserDataDirectories(string steamInstallPath)
    {
        string userDataPath = Path.Combine(steamInstallPath, "userdata");
        return Directory.Exists(userDataPath) ? Directory.GetDirectories(userDataPath) : Array.Empty<string>();
    }
}
