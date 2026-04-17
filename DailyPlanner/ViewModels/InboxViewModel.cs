using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class InboxViewModel : ObservableObject
{
    private readonly PlannerService _planner;
    private readonly TrelloService _trello;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSyncing;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private DateOnly _targetDate = DateOnly.FromDateTime(DateTime.Today);
    [ObservableProperty] private string _searchText = string.Empty;

    private readonly List<InboxTaskViewModel> _all = new();
    public ObservableCollection<InboxTaskViewModel> Tasks { get; } = [];
    public ObservableCollection<InboxDayViewModel> Days { get; } = [];
    public bool HasTasks => Tasks.Count > 0;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Tasks.Clear();
        var q = SearchText?.Trim() ?? string.Empty;
        var filtered = string.IsNullOrEmpty(q)
            ? _all
            : _all.Where(t =>
                t.Text.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (t.BoardName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase));
        foreach (var t in filtered) Tasks.Add(t);
    }

    public InboxViewModel(PlannerService planner, TrelloService trello)
    {
        _planner = planner;
        _trello = trello;
        Tasks.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasTasks));
        BuildWeekDays();
    }

    private void BuildWeekDays()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        Days.Clear();
        for (var i = 0; i < 7; i++)
            Days.Add(new InboxDayViewModel(monday.AddDays(i)));
    }

    [RelayCommand]
    private void ShiftWeek(string? deltaStr)
    {
        if (Days.Count == 0) return;
        if (!int.TryParse(deltaStr, out var delta)) return;
        var monday = Days[0].Date.AddDays(delta * 7);
        Days.Clear();
        for (var i = 0; i < 7; i++)
            Days.Add(new InboxDayViewModel(monday.AddDays(i)));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _planner.GetInboxTasksAsync();
            _all.Clear();
            foreach (var t in items)
                _all.Add(new InboxTaskViewModel(t));
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SyncTrelloAsync()
    {
        if (IsSyncing) return;
        IsSyncing = true;
        StatusMessage = Loc.Get("TrelloSyncing");
        try
        {
            var added = await _planner.SyncTrelloAsync(_trello);
            StatusMessage = string.Format(Loc.Get("TrelloSyncDone"), added);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"{Loc.Get("TrelloSyncError")}: {ex.Message}";
        }
        finally
        {
            IsSyncing = false;
        }
    }

    [RelayCommand]
    private async Task AddManualAsync()
    {
        var task = new InboxTask
        {
            Text = Loc.Get("NewTask"),
            Source = InboxSource.Manual,
            CreatedDate = DateOnly.FromDateTime(DateTime.Today)
        };
        await _planner.SaveInboxTaskAsync(task);
        var vm = new InboxTaskViewModel(task);
        _all.Insert(0, vm);
        ApplyFilter();
    }

    [RelayCommand]
    private async Task SaveTaskAsync(InboxTaskViewModel? vm)
    {
        if (vm is null) return;
        vm.Model.Text = vm.Text;
        vm.Model.DueDate = vm.DueDate;
        await _planner.SaveInboxTaskAsync(vm.Model);
    }

    [RelayCommand]
    private async Task RemoveTaskAsync(InboxTaskViewModel? vm)
    {
        if (vm is null) return;
        await _planner.RemoveInboxTaskAsync(vm.Id);
        _all.Remove(vm);
        Tasks.Remove(vm);
    }

    public async Task MoveToDayAsync(InboxTaskViewModel vm, DateOnly date)
    {
        await _planner.MoveInboxToDayAsync(vm.Id, date);
        _all.Remove(vm);
        Tasks.Remove(vm);
        StatusMessage = string.Format(Loc.Get("InboxMovedToDate"), date.ToString("dd.MM"));
    }

    [RelayCommand]
    private async Task MoveToSelectedDateAsync(InboxTaskViewModel? vm)
    {
        if (vm is null) return;
        await MoveToDayAsync(vm, TargetDate);
    }
}
