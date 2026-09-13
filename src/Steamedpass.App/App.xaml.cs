using System.Windows;

namespace Steamedpass.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        string[] args = e.Args;

        // Steam invokes this exe as "<exe> <aumid> <executable> [...]" to launch a game.
        if (args.Length >= 2 && args[0].Contains('!'))
        {
            await LauncherRunner.RunAsync(args);
            Shutdown();
            return;
        }

        string[] cliVerbs = { "add", "list", "config" };
        if (args.Length >= 1 && cliVerbs.Contains(args[0], StringComparer.OrdinalIgnoreCase))
        {
            int exitCode = await CliRunner.RunAsync(args);
            Shutdown(exitCode);
            return;
        }

        base.OnStartup(e);
        new MainWindow().Show();
    }
}
