using System.Runtime.InteropServices;

namespace Steamedpass.Core.Display;

/// <summary>
/// Reads and changes the primary display resolution. UWPHook's equivalent
/// feature shells out to a "Set-DisplayResolution" PowerShell cmdlet that
/// isn't a built-in Windows cmdlet and ships with no implementation, so it
/// silently no-ops unless the user has a matching module installed. This
/// uses the native ChangeDisplaySettings API directly instead, so the
/// option actually works.
/// </summary>
public static class DisplayResolution
{
    public static (int Width, int Height) GetCurrent()
    {
        var mode = new DEVMODE { dmSize = (short)Marshal.SizeOf<DEVMODE>() };
        EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref mode);
        return (mode.dmPelsWidth, mode.dmPelsHeight);
    }

    public static IReadOnlyList<(int Width, int Height)> EnumerateSupported()
    {
        var results = new List<(int, int)>();
        var mode = new DEVMODE { dmSize = (short)Marshal.SizeOf<DEVMODE>() };

        for (int i = 0; EnumDisplaySettings(null, i, ref mode); i++)
        {
            var resolution = (mode.dmPelsWidth, mode.dmPelsHeight);
            if (!results.Contains(resolution))
            {
                results.Add(resolution);
            }
        }

        results.Sort((a, b) => (b.Item1 * b.Item2).CompareTo(a.Item1 * a.Item2));
        return results;
    }

    /// <returns>true if the resolution was changed successfully.</returns>
    public static bool TrySet(int width, int height)
    {
        var mode = new DEVMODE { dmSize = (short)Marshal.SizeOf<DEVMODE>() };
        if (!EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref mode))
        {
            return false;
        }

        mode.dmPelsWidth = width;
        mode.dmPelsHeight = height;
        mode.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT;

        int result = ChangeDisplaySettings(ref mode, CDS_UPDATEREGISTRY);
        return result == DISP_CHANGE_SUCCESSFUL;
    }

    private const int ENUM_CURRENT_SETTINGS = -1;
    private const int CDS_UPDATEREGISTRY = 0x01;
    private const int DISP_CHANGE_SUCCESSFUL = 0;
    private const int DM_PELSWIDTH = 0x80000;
    private const int DM_PELSHEIGHT = 0x100000;

    [StructLayout(LayoutKind.Sequential)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);

    [DllImport("user32.dll")]
    private static extern int ChangeDisplaySettings(ref DEVMODE devMode, int flags);
}
