using System.Windows;
using Steamedpass.Core.Logging;
using Steamedpass.Core.Settings;
using Velopack;

namespace Steamedpass.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // Velopack needs to inspect the process args and exit early during its own
    // install/update/uninstall lifecycle hooks, before WPF/theme startup cost is paid -
    // so this runs ahead of even constructing the Application object. See csproj's
    // <StartupObject>/<ApplicationDefinition> switch, which disables WPF's normal
    // auto-generated Main so this one runs instead.
    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        SteamedpassSettings settings = SteamedpassSettings.Load();
        AppLog.Initialize(settings.LogLevel);

        string[] args = e.Args;

        // Steam invokes this exe as "<exe> <aumid> <executable> [...]" to launch a game.
        if (args.Length >= 2 && args[0].Contains('!'))
        {
            await LauncherRunner.RunAsync(args);
            Shutdown();
            return;
        }

        string[] cliVerbs = { "add", "list", "config", "update" };
        if (args.Length >= 1 && cliVerbs.Contains(args[0], StringComparer.OrdinalIgnoreCase))
        {
            int exitCode = await CliRunner.RunAsync(args);
            Shutdown(exitCode);
            return;
        }

        base.OnStartup(e);
        ThemeManager.Apply(settings.ThemeMode);
        new MainWindow().Show();
    }
}
