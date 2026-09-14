using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Serilog;
using Steamedpass.Core.Discovery;
using Steamedpass.Core.Icons;
using Steamedpass.Core.Pipeline;
using Steamedpass.Core.Settings;
using Steamedpass.Core.Steam;

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

        string titleWithVersion = $"SteamedPass v{AppInfo.Version}";
        Title = titleWithVersion;
        AppTitleBar.Title = titleWithVersion;

        Loaded += async (_, _) =>
        {
            await RefreshGamesAsync();

            // The ListView hasn't finished its first layout pass yet at this point,
            // so the other columns' ActualWidth isn't reliable until it settles.
            _ = Dispatcher.BeginInvoke(ResizeAumidColumn, System.Windows.Threading.DispatcherPriority.Loaded);
        };
        GamesList.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(GamesList_HeaderClick));
        GamesList.SizeChanged += (_, _) => ResizeAumidColumn();

        // The AUMID column fills whatever space the others don't use, so it always
        // reaches the right edge - re-measure it whenever an earlier column is resized.
        DependencyPropertyDescriptor widthDescriptor =
            DependencyPropertyDescriptor.FromProperty(GridViewColumn.WidthProperty, typeof(GridViewColumn));
        foreach (GridViewColumn column in new[] { SelectColumn, AddedColumn, IconColumn, NameColumn, ExecutableColumn })
        {
            widthDescriptor.AddValueChanged(column, (_, _) => ResizeAumidColumn());
        }
    }

    private void ResizeAumidColumn()
    {
        double used = SelectColumn.ActualWidth + AddedColumn.ActualWidth + IconColumn.ActualWidth +
            NameColumn.ActualWidth + ExecutableColumn.ActualWidth;
        double available = GamesList.ActualWidth - used - SystemParameters.VerticalScrollBarWidth - 8;

        if (available > 120)
        {
            AumidColumn.Width = available;
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await RefreshGamesAsync();

    private async Task RefreshGamesAsync()
    {
        StatusText.Text = "Scanning installed apps...";
        RefreshProgressBar.Visibility = Visibility.Visible;
        IsEnabled = false;

        try
        {
            List<SelectableGame> selectableGames = await Task.Run(() =>
            {
                IReadOnlyList<InstalledGame> games = GameScanner.GetInstalledGames();
                HashSet<string> addedNames = GetAddedAppNames();

                return games
                    .Select(g => new SelectableGame(g)
                    {
                        IsAdded = addedNames.Contains(g.Name),
                        IconPath = PackageIconResolver.FindThumbnail(g.LogoDirectory),
                    })
                    .ToList();
            });

            GamesList.ItemsSource = selectableGames;
            ApplyCurrentSort();
            SelectAllCheckBox.IsChecked = false;
            StatusText.Text = $"{selectableGames.Count} app(s) found.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error scanning installed apps: {ex.Message}";
        }
        finally
        {
            RefreshProgressBar.Visibility = Visibility.Collapsed;
            IsEnabled = true;
        }
    }

    /// <summary>
    /// Best-effort: which installed games this exact steamedpass.exe has already
    /// added to Steam. Returns an empty set (rather than throwing) if Steam isn't
    /// found or its shortcuts can't be read, so a scan never fails because of it.
    /// </summary>
    private static HashSet<string> GetAddedAppNames()
    {
        try
        {
            string? steamFolder = SteamPaths.GetSteamFolder();
            if (steamFolder is null)
            {
                return new HashSet<string>();
            }

            string[] userDataDirectories = SteamPaths.GetUserDataDirectories(steamFolder);
            return SteamShortcuts.GetAddedAppNames(userDataDirectories, Environment.ProcessPath!);
        }
        catch
        {
            return new HashSet<string>();
        }
    }

    private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
    {
        bool select = SelectAllCheckBox.IsChecked == true;
        foreach (SelectableGame game in GamesList.Items.OfType<SelectableGame>())
        {
            game.IsSelected = select;
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
            if (GetSortProperty(column) is null)
            {
                // Not a sortable, text-headed column (e.g. the "Select" checkbox
                // column) - its Header is a CheckBox control, not a string, so
                // leave it alone rather than stomping it with Header.ToString().
                continue;
            }

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
        List<InstalledGame> selectedGames = GamesList.Items.OfType<SelectableGame>()
            .Where(g => g.IsSelected)
            .Select(g => g.Game)
            .ToList();

        if (selectedGames.Count == 0)
        {
            MessageBox.Show("Select at least one game first.", "SteamedPass", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsEnabled = false;
        StatusText.Text = selectedGames.Count == 1
            ? $"Adding '{selectedGames[0].Name}' to Steam (Steam will restart)..."
            : $"Adding {selectedGames.Count} apps to Steam (Steam will restart)...";

        try
        {
            string exePath = Environment.ProcessPath!;
            SteamedpassSettings settings = SteamedpassSettings.Load();
            AddGamesResult result = await AddGamePipeline.RunAsync(selectedGames, exePath, settings);

            int gridArtCount = result.Games.Count(g => g.Result.GridArtInstalled);
            int desktopShortcutCount = result.Games.Count(g => g.Result.DesktopShortcutPath is not null);

            StatusText.Text =
                $"Done. Added {result.Games.Count} app(s) to Steam. Steam restarted: {result.SteamRestarted}. " +
                $"Library grid art installed for {gridArtCount}. Desktop shortcuts created for {desktopShortcutCount}.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add {Count} game(s) to Steam", selectedGames.Count);
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
