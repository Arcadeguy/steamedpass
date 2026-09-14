using System.ComponentModel;
using Steamedpass.Core.Discovery;

namespace Steamedpass.App;

/// <summary>
/// Wraps an <see cref="InstalledGame"/> with a checkbox selection state for the
/// games grid, so multiple apps can be added to Steam in one batch.
/// </summary>
public sealed class SelectableGame(InstalledGame game) : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isAdded;

    public InstalledGame Game { get; } = game;

    public string Name => Game.Name;
    public string Executable => Game.Executable;
    public string Aumid => Game.Aumid;

    /// <summary>Whether this game already has a matching shortcut in Steam.</summary>
    public bool IsAdded
    {
        get => _isAdded;
        set
        {
            if (_isAdded == value)
            {
                return;
            }

            _isAdded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAdded)));
        }
    }

    /// <summary>Path to a thumbnail image for this game, or null if none was found.</summary>
    public string? IconPath { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
