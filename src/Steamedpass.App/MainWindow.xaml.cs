using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Serilog;
using Steamedpass.Core.Discovery;
using Steamedpass.Core.Pipeline;
using Steamedpass.Core.Settings;

namespace Steamedpass.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly Dictionary<GridViewColumn, string> _originalHeaderText = new();
    private GridViewColumn? _sortedColumn;
    private ListSortDirection _sortDirection = ListSortDirection.Ascending;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshGames();
        GamesList.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(GamesList_HeaderClick));
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
            ApplyCurrentSort();
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

    private void GamesList_HeaderClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader header || header.Role != GridViewColumnHeaderRole.Normal)
        {
            return;
        }

        string? sortProperty = GetSortProperty(header.Column);
        if (sortProperty is null)
        {
            return;
        }

        _sortDirection = header.Column == _sortedColumn && _sortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;
        _sortedColumn = header.Column;

        SortBy(sortProperty, _sortDirection);
        UpdateHeaderText();
    }

    private void ApplyCurrentSort()
    {
        string? sortProperty = GetSortProperty(_sortedColumn);
        if (sortProperty is not null)
        {
            SortBy(sortProperty, _sortDirection);
        }

        UpdateHeaderText();
    }

    private void SortBy(string propertyPath, ListSortDirection direction)
    {
        ICollectionView view = CollectionViewSource.GetDefaultView(GamesList.ItemsSource);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(propertyPath, direction));
        view.Refresh();
    }

    private void UpdateHeaderText()
    {
        if (GamesList.View is not GridView gridView)
        {
            return;
        }

        foreach (GridViewColumn column in gridView.Columns)
        {
            if (!_originalHeaderText.TryGetValue(column, out string? baseText))
            {
                baseText = column.Header?.ToString() ?? string.Empty;
                _originalHeaderText[column] = baseText;
            }

            column.Header = column == _sortedColumn
                ? $"{baseText} {(_sortDirection == ListSortDirection.Ascending ? "▲" : "▼")}"
                : baseText;
        }
    }

    private static string? GetSortProperty(GridViewColumn? column) =>
        (column?.DisplayMemberBinding as Binding)?.Path?.Path;

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (GamesList.SelectedItem is not InstalledGame game)
        {
            MessageBox.Show("Select a game first.", "SteamedPass", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsEnabled = false;
        StatusText.Text = $"Adding '{game.Name}' to Steam (Steam will restart)...";

        try
        {
            string exePath = Environment.ProcessPath!;
            SteamedpassSettings settings = SteamedpassSettings.Load();
            AddGameResult result = await AddGamePipeline.RunAsync(game, exePath, settings);

            string desktopShortcutStatus = result.DesktopShortcutPath is null
                ? "skipped (disabled in settings)"
                : $"{result.DesktopShortcutPath} (icon extracted: {result.DesktopIconExtracted})";

            StatusText.Text =
                $"Done. Added to Steam: {result.AddedToSteam}. " +
                $"Library grid art installed: {result.GridArtInstalled}. " +
                $"Desktop shortcut: {desktopShortcutStatus}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add {Game} to Steam", game.Name);
            StatusText.Text = $"Error: {ex.Message} (see %AppData%\\steamedpass\\application.log for details)";
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow { Owner = this };
        settingsWindow.ShowDialog();
    }
}
