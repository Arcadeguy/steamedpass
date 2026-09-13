using System.Runtime.InteropServices;

namespace Steamedpass.App;

/// <summary>
/// Attaches a console to this WinExe process on demand, so CLI mode can print
/// output without the app carrying a console window at all times.
/// </summary>
internal static class NativeConsole
{
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    public static void Attach() => AllocConsole();
}
