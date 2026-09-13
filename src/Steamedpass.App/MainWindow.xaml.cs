using System.Windows;
using Steamedpass.Core.Discovery;
using Steamedpass.Core.Pipeline;

namespace Steamedpass.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshGames();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => RefreshGames();

    private void RefreshGames()
    {
        StatusText.Text = "Scanning installed apps...";
        IsEnabled = false;

        try
        {
            IReadOnlyList<InstalledGame> games = GameScanner.GetInstalledGames();
            GamesList.ItemsSource = games;
            StatusText.Text = $"{games.Count} app(s) found.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error scanning installed apps: {ex.Message}";
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (GamesList.SelectedItem is not InstalledGame game)
        {
            MessageBox.Show("Select a game first.", "steamedpass", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsEnabled = false;
        StatusText.Text = $"Adding '{game.Name}' to Steam (Steam will restart)...";

        try
        {
            string exePath = Environment.ProcessPath!;
            AddGameResult result = await AddGamePipeline.RunAsync(game, exePath, tags: Array.Empty<string>());

            StatusText.Text =
                $"Done. Added to Steam: {result.AddedToSteam}. " +
                $"Desktop icon extracted: {result.DesktopIconExtracted}. " +
                $"Library grid art installed: {result.GridArtInstalled}. " +
                $"Desktop shortcut: {result.DesktopShortcutPath}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            IsEnabled = true;
        }
    }
}
