using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Steamedpass.Core.Launch;

/// <summary>
/// Activates an installed UWP app by AUMID and reports whether it is still running,
/// so Steam can track playtime/running-state for the shortcut that launches it.
/// Adapted from UWPHook's AppManager (https://github.com/BrianLima/UWPHook),
/// MIT License, Copyright (c) 2016 Brian Lima.
/// </summary>
public sealed class GameLauncher
{
    private int _runningProcessId;

    /// <summary>
    /// Activates the app and returns the process id Windows reports for it.
    /// </summary>
    public int Launch(string aumid, string extraArguments = "")
    {
        var manager = new ApplicationActivationManager();
        manager.ActivateApplication(aumid, extraArguments, ActivateOptions.None, out uint processId);
        _runningProcessId = (int)processId;
        BringToForeground();
        return _runningProcessId;
    }

    public bool IsRunning()
    {
        if (_runningProcessId == 0)
        {
            return false;
        }

        try
        {
            Process.GetProcessById(_runningProcessId);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private void BringToForeground()
    {
        try
        {
            Process process = Process.GetProcessById(_runningProcessId);
            IntPtr handle = process.MainWindowHandle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            if (IsIconic(handle))
            {
                ShowWindowAsync(handle, 3 /* SW_SHOWMAXIMIZED */);
            }

            SetForegroundWindow(handle);
        }
        catch (ArgumentException)
        {
            // Process may have already exited.
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);
}

internal enum ActivateOptions
{
    None = 0x00000000,
    DesignMode = 0x00000001,
    NoErrorUI = 0x00000002,
    NoSplashScreen = 0x00000004,
}

[ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IApplicationActivationManager
{
    IntPtr ActivateApplication(
        [In] string appUserModelId,
        [In] string arguments,
        [In] ActivateOptions options,
        [Out] out uint processId);
}

[ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
internal class ApplicationActivationManager : IApplicationActivationManager
{
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public extern IntPtr ActivateApplication(
        [In] string appUserModelId,
        [In] string arguments,
        [In] ActivateOptions options,
        [Out] out uint processId);
}
