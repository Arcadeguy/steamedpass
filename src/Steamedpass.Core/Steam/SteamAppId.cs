using System.Text;
using Force.Crc32;

namespace Steamedpass.Core.Steam;

/// <summary>
/// Computes the app ids Steam derives for non-Steam shortcuts.
/// CRC32/appid scheme adapted from UWPHook's GamesWindow.GenerateSteamGridAppId
/// (https://github.com/BrianLima/UWPHook), MIT License, Copyright (c) 2016 Brian Lima,
/// and cross-checked against https://github.com/CorporalQuesadilla/Steam-Shortcut-Manager/wiki/Steam-Shortcuts-Documentation.
/// </summary>
public static class SteamAppId
{
    /// <summary>
    /// The 32-bit id stored in the shortcuts.vdf "appid" field.
    /// </summary>
    public static int ComputeLegacyAppId(string exe, string appName)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(exe + appName);
        uint crc = Crc32Algorithm.Compute(bytes);
        uint top32 = crc | 0x80000000;
        return unchecked((int)top32);
    }

    /// <summary>
    /// The full 64-bit id used by "steam://rungameid/&lt;id&gt;" and by
    /// grid/icon cache filenames under config/grid/.
    /// </summary>
    public static ulong ComputeShortcutId64(string exe, string appName)
    {
        uint top32 = unchecked((uint)ComputeLegacyAppId(exe, appName));
        return ((ulong)top32 << 32) | 0x02000000;
    }
}
