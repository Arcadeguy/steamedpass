using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Steamedpass.App;

/// <summary>
/// Applies a persisted "System"/"Light"/"Dark" theme choice via WPF-UI's
/// <see cref="ApplicationThemeManager"/>.
/// </summary>
internal static class ThemeManager
{
    public static void Apply(string themeMode)
    {
        switch (themeMode)
        {
            case "Light":
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                break;
            case "Dark":
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                break;
            default:
                ApplicationThemeManager.ApplySystemTheme();
                break;
        }

        // ApplicationThemeManager only repaints Application.Current.MainWindow's
        // Mica backdrop; force every other open FluentWindow (e.g. Settings) to
        // repaint too, so a live theme switch is visible everywhere at once.
        ApplicationTheme currentTheme = ApplicationThemeManager.GetAppTheme();
        foreach (Window window in Application.Current.Windows)
        {
            if (window is FluentWindow fluentWindow)
            {
                WindowBackgroundManager.UpdateBackground(fluentWindow, currentTheme, fluentWindow.WindowBackdropType);
            }
        }
    }
}
