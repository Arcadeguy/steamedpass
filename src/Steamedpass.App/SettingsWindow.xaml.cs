using System.Diagnostics;
using System.Globalization;
using System.Windows;
using Steamedpass.Core.Display;
using Steamedpass.Core.GridArt;
using Steamedpass.Core.Logging;
using Steamedpass.Core.Settings;
using Steamedpass.Core.Steam;

namespace Steamedpass.App;

/// <summary>
/// Settings page mirroring UWPHook's SettingsWindow
/// (https://github.com/BrianLima/UWPHook), MIT License, Copyright (c) 2016 Brian Lima.
/// </summary>
public partial class SettingsWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly SteamedpassSettings _settings;

    public SettingsWindow()
    {
        InitializeComponent();
        _settings = SteamedpassSettings.Load();

        CreateDesktopShortcutCheck.IsChecked = _settings.CreateDesktopShortcut;

        LanguageCombo.ItemsSource = CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Select(c => c.Name).Where(n => !string.IsNullOrEmpty(n)).OrderBy(n => n).ToList();
        LanguageCombo.SelectedItem = string.IsNullOrEmpty(_settings.TargetLanguage)
            ? CultureInfo.CurrentCulture.Name
            : _settings.TargetLanguage;
        ChangeLanguageCheck.IsChecked = _settings.ChangeLanguage;

        SecondsCombo.ItemsSource = Enumerable.Range(0, 10).Select(i => $"{i} seconds").ToList();
        SecondsCombo.SelectedIndex = _settings.PollSeconds;

        StreamModeCheck.IsChecked = _settings.StreamMode;

        var resolutions = DisplayResolution.EnumerateSupported()
            .Select(r => $"{r.Width} x {r.Height}").Distinct().ToList();
        ResolutionCombo.ItemsSource = resolutions;
        (int currentWidth, int currentHeight) = DisplayResolution.GetCurrent();
        ResolutionCombo.SelectedItem = string.IsNullOrEmpty(_settings.TargetResolution)
            ? $"{currentWidth} x {currentHeight}"
            : _settings.TargetResolution;
        ChangeResolutionCheck.IsChecked = _settings.ChangeResolution;

        LogLevelCombo.SelectedIndex = _settings.LogLevel;

        ApiKeyBox.Password = _settings.SteamGridDbApiKey ?? string.Empty;
        StyleCombo.ItemsSource = SteamGridDbOptions.Styles;
        StyleCombo.SelectedIndex = _settings.SteamGridDbStyle;
        TypeCombo.ItemsSource = SteamGridDbOptions.Types;
        TypeCombo.SelectedIndex = _settings.SteamGridDbType;
        NsfwCombo.ItemsSource = SteamGridDbOptions.Nsfw;
        NsfwCombo.SelectedIndex = _settings.SteamGridDbNsfw;
        HumorCombo.ItemsSource = SteamGridDbOptions.Humor;
        HumorCombo.SelectedIndex = _settings.SteamGridDbHumor;

        TagsBox.Text = _settings.Tags;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.CreateDesktopShortcut = CreateDesktopShortcutCheck.IsChecked == true;
        _settings.ChangeLanguage = ChangeLanguageCheck.IsChecked == true;
        _settings.TargetLanguage = LanguageCombo.SelectedItem?.ToString() ?? string.Empty;
        _settings.PollSeconds = SecondsCombo.SelectedIndex;
        _settings.StreamMode = StreamModeCheck.IsChecked == true;
        _settings.ChangeResolution = ChangeResolutionCheck.IsChecked == true;
        _settings.TargetResolution = ResolutionCombo.SelectedItem?.ToString() ?? string.Empty;
        _settings.LogLevel = LogLevelCombo.SelectedIndex;
        _settings.SteamGridDbApiKey = ApiKeyBox.Password.Trim();
        _settings.SteamGridDbStyle = StyleCombo.SelectedIndex;
        _settings.SteamGridDbType = TypeCombo.SelectedIndex;
        _settings.SteamGridDbNsfw = NsfwCombo.SelectedIndex;
        _settings.SteamGridDbHumor = HumorCombo.SelectedIndex;
        _settings.Tags = TagsBox.Text;

        _settings.Save();
        AppLog.SetLevel(_settings.LogLevel);

        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    private void GetApiKeyButton_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://www.steamgriddb.com/profile/preferences/api");

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult result = MessageBox.Show(
            "This will remove ALL non-Steam game shortcuts (not just ones steamedpass added). Are you sure you want to continue?",
            "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        string? steamFolder = SteamPaths.GetSteamFolder();
        if (steamFolder is null)
        {
            MessageBox.Show("Could not locate a Steam installation.", "SteamedPass", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        foreach (string userDataDirectory in SteamPaths.GetUserDataDirectories(steamFolder))
        {
            SteamShortcuts.ClearAllShortcuts(userDataDirectory);
        }

        MessageBox.Show("All non-Steam shortcuts have been cleared. Restart Steam to see the change.", "SteamedPass", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
}
