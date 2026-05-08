using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Controls;

namespace DailyPlanner.ViewModels;

/// <summary>
/// Single entry in the Ctrl+K command palette.
/// `Action` runs after the palette closes — never close inside the action.
/// </summary>
public sealed class CommandPaletteAction
{
    public required string Label { get; init; }
    public string? Hint { get; init; }
    public string? Shortcut { get; init; }
    public SymbolRegular Icon { get; init; } = SymbolRegular.Sparkle24;
    public required Action Run { get; init; }

    public bool HasHint => !string.IsNullOrEmpty(Hint);
    public bool HasShortcut => !string.IsNullOrEmpty(Shortcut);

    /// <summary>Lower-cased haystack pre-built once for fuzzy matching.</summary>
    public string SearchKey => _searchKey ??= $"{Label} {Hint}".ToLowerInvariant();
    private string? _searchKey;
}

public sealed partial class CommandPaletteViewModel : ObservableObject
{
    private readonly IReadOnlyList<CommandPaletteAction> _all;

    [ObservableProperty] private string _query = string.Empty;
    [ObservableProperty] private int _selectedIndex;

    public ObservableCollection<CommandPaletteAction> Filtered { get; } = [];

    public bool HasResults => Filtered.Count > 0;

    /// <summary>True = run selected action, False = just close.</summary>
    public event EventHandler<bool>? RequestClose;

    public CommandPaletteViewModel(IReadOnlyList<CommandPaletteAction> actions)
    {
        _all = actions;
        Refilter();
    }

    partial void OnQueryChanged(string value) => Refilter();

    partial void OnSelectedIndexChanged(int value)
    {
        // Clamp to valid range; fires from keyboard nav too.
        if (Filtered.Count == 0) return;
        if (value < 0) SelectedIndex = 0;
        else if (value >= Filtered.Count) SelectedIndex = Filtered.Count - 1;
    }

    private void Refilter()
    {
        Filtered.Clear();

        if (string.IsNullOrWhiteSpace(Query))
        {
            foreach (var a in _all) Filtered.Add(a);
        }
        else
        {
            var q = Query.Trim().ToLowerInvariant();
            foreach (var a in _all)
                if (a.SearchKey.Contains(q)) Filtered.Add(a);
        }

        SelectedIndex = Filtered.Count > 0 ? 0 : -1;
        OnPropertyChanged(nameof(HasResults));
    }

    [RelayCommand]
    private void Close() => RequestClose?.Invoke(this, false);

    [RelayCommand]
    private void ExecuteSelected()
    {
        if (SelectedIndex < 0 || SelectedIndex >= Filtered.Count) return;
        var action = Filtered[SelectedIndex];
        // Close first, then run — avoids action-thrown exceptions leaving dialog open.
        RequestClose?.Invoke(this, true);
        action.Run();
    }

    [RelayCommand]
    private void MoveSelectionDown()
    {
        if (Filtered.Count == 0) return;
        SelectedIndex = (SelectedIndex + 1) % Filtered.Count;
    }

    [RelayCommand]
    private void MoveSelectionUp()
    {
        if (Filtered.Count == 0) return;
        SelectedIndex = SelectedIndex <= 0 ? Filtered.Count - 1 : SelectedIndex - 1;
    }
}
