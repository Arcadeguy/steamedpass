using System.Windows;

namespace Steamedpass.App;

/// <summary>
/// A plain full-screen cover window shown while Steam's in-home streaming
/// catches up, matching UWPHook's "stream mode" ("StreamMode" setting).
/// </summary>
public partial class LaunchingOverlayWindow : Window
{
    private static readonly string[] Messages =
    {
        "Hold on, making your stream full screen...",
        "Waiting for Steam in-home streaming to catch up...",
        "Starting your stream in a few seconds...",
        "Let's get this game started!",
        "Good game, enjoy!",
    };

    public LaunchingOverlayWindow()
    {
        InitializeComponent();
        MessageText.Text = Messages[DateTime.Now.Second % Messages.Length];
    }
}
