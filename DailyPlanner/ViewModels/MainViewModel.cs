using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyPlanner.Models;
using DailyPlanner.Services;
using Microsoft.Win32;

namespace DailyPlanner.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly PlannerService _service = new();

    [ObservableProperty] private int _selectedYear = DateTime.Today.Year;
    [ObservableProperty] private int _selectedMonth = DateTime.Today.Month;
    [ObservableProperty] private WeekViewModel? _selectedWeek;
    [ObservableProperty] private int _selectedWeekIndex;
    [ObservableProperty] private bool _isSettingsOpen;
    [ObservableProperty] private bool _isSearchOpen;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isPomodoroOpen;
    [ObservableProperty] private bool _isAutoStartEnabled;
    [ObservableProperty] private TaskCategory _filterCategory = TaskCategory.None;

    public ObservableCollection<WeekViewModel> Weeks { get; } = [];
    public ObservableCollection<SearchResultItem> SearchResults { get; } = [];
    public ObservableCollection<RecurringTemplate> Templates { get; } = [];
    public ObservableCollection<ReminderViewModel> Reminders { get; } = [];

    public PomodoroViewModel Pomodoro { get; } = new();
    public StatisticsViewModel Statistics { get; }
    public PlannerService Service => _service;
    public string DailyQuote { get; } = QuoteService.GetDailyQuote();
    public string AppVersion { get; } = typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "?";

    private readonly UpdateService _updateService = new("https://github.com/valinerosgordov/DailyPlanner");
    [ObservableProperty] private string _updateStatus = string.Empty;
    [ObservableProperty] private bool _isUpdateAvailable;
    [ObservableProperty] private int _updateProgress;

    private bool _isInitializing;

    public MainViewModel()
    {
        Statistics = new StatisticsViewModel(_service);
    }

    public MonthItem[] Months { get; } =
    [
        new(1, "Январь"), new(2, "Февраль"), new(3, "Март"), new(4, "Апрель"),
        new(5, "Май"), new(6, "Июнь"), new(7, "Июль"), new(8, "Август"),
        new(9, "Сентябрь"), new(10, "Октябрь"), new(11, "Ноябрь"), new(12, "Декабрь")
    ];

    public string SelectedMonthName => Months[SelectedMonth - 1].Name;

    [RelayCommand]
    public async Task LoadMonthAsync()
    {
        Weeks.Clear();
        var weekStarts = PlannerService.GetWeekStartsForMonth(SelectedYear, SelectedMonth);

        foreach (var start in weekStarts)
        {
            var week = await _service.GetOrCreateWeekAsync(start);
            await _service.ApplyTemplatesAsync(week);
            Weeks.Add(new WeekViewModel(week, _service));
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentWeek = Weeks.FirstOrDefault(w => w.StartDate <= today && w.EndDate >= today);
        SelectedWeek = currentWeek ?? Weeks.FirstOrDefault();
        SelectedWeekIndex = SelectedWeek is not null ? Weeks.IndexOf(SelectedWeek) : 0;

        OnPropertyChanged(nameof(SelectedMonthName));
        OnPropertyChanged(nameof(TodayTasks));
        OnPropertyChanged(nameof(TodayProgress));
    }

    public string TodayTasks
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var day = SelectedWeek?.Days.FirstOrDefault(d => d.Date == today);
            if (day is null) return "Нет задач на сегодня";
            var tasks = day.Tasks
                .Where(t => !string.IsNullOrWhiteSpace(t.Text))
                .Select(t => (t.IsCompleted ? "\u2705 " : "\u25cb ") + t.Text)
                .ToList();
            return tasks.Count > 0 ? string.Join("\n", tasks.Take(5)) : "Нет задач";
        }
    }

    public string TodayProgress
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var day = SelectedWeek?.Days.FirstOrDefault(d => d.Date == today);
            if (day is null) return "";
            return $"{day.CompletedCount}/{day.TotalWithText} выполнено";
        }
    }

    [RelayCommand]
    private async Task SelectMonthAsync(int month)
    {
        SelectedMonth = month;
        await LoadMonthAsync();
    }

    [RelayCommand]
    private async Task PreviousMonthAsync()
    {
        if (SelectedMonth == 1) { SelectedMonth = 12; SelectedYear--; }
        else SelectedMonth--;
        OnPropertyChanged(nameof(SelectedMonthName));
        await LoadMonthAsync();
    }

    [RelayCommand]
    private async Task NextMonthAsync()
    {
        if (SelectedMonth == 12) { SelectedMonth = 1; SelectedYear++; }
        else SelectedMonth++;
        OnPropertyChanged(nameof(SelectedMonthName));
        await LoadMonthAsync();
    }

    [RelayCommand]
    private async Task GoToTodayAsync()
    {
        SelectedYear = DateTime.Today.Year;
        SelectedMonth = DateTime.Today.Month;
        OnPropertyChanged(nameof(SelectedMonthName));
        await LoadMonthAsync();
    }

    partial void OnSelectedWeekChanged(WeekViewModel? oldValue, WeekViewModel? newValue)
    {
        if (oldValue is not null) oldValue.IsSelected = false;
        if (newValue is not null) newValue.IsSelected = true;
    }

    partial void OnSelectedWeekIndexChanged(int value)
    {
        if (value >= 0 && value < Weeks.Count)
            SelectedWeek = Weeks[value];
    }

    [RelayCommand] private void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;

    [RelayCommand]
    private void ToggleSearch()
    {
        IsSearchOpen = !IsSearchOpen;
        if (!IsSearchOpen) { SearchQuery = string.Empty; SearchResults.Clear(); }
    }

    [RelayCommand] private void TogglePomodoro() => IsPomodoroOpen = !IsPomodoroOpen;

    partial void OnSearchQueryChanged(string value)
    {
        DebounceService.Debounce("search", () =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() => PerformSearch(value));
            return Task.CompletedTask;
        }, 200);
    }

    private void PerformSearch(string query)
    {
        SearchResults.Clear();
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return;

        var q = query.Trim().ToLowerInvariant();
        for (var i = 0; i < Weeks.Count; i++)
        {
            var week = Weeks[i];
            foreach (var day in week.Days)
            {
                foreach (var task in day.Tasks)
                {
                    if (!string.IsNullOrWhiteSpace(task.Text) &&
                        task.Text.Contains(q, StringComparison.OrdinalIgnoreCase))
                    {
                        SearchResults.Add(new SearchResultItem(
                            task.Text, $"{day.DayName} ({day.DateFormatted})", week.WeekRange, i, day.Date));
                    }
                }
            }

            foreach (var goal in week.Goals)
            {
                if (!string.IsNullOrWhiteSpace(goal.Text) &&
                    goal.Text.Contains(q, StringComparison.OrdinalIgnoreCase))
                {
                    SearchResults.Add(new SearchResultItem(
                        goal.Text, "Цель", week.WeekRange, i, null));
                }
            }
        }
    }

    [RelayCommand]
    private void NavigateToSearchResult(SearchResultItem? item)
    {
        if (item is null) return;
        if (item.WeekIndex >= 0 && item.WeekIndex < Weeks.Count)
            SelectedWeekIndex = item.WeekIndex;
        IsSearchOpen = false;
        SearchQuery = string.Empty;
        SearchResults.Clear();
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (SelectedWeek is null) return;

        var dialog = new SaveFileDialog
        {
            FileName = $"Планер_{SelectedWeek.StartDate:yyyy-MM-dd}",
            DefaultExt = ".xlsx",
            Filter = "Excel файлы (*.xlsx)|*.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            var ok = ExportService.ExportWeekToExcel(SelectedWeek.Model, dialog.FileName);
            NotificationService.ShowToast("Экспорт",
                ok ? "Файл успешно сохранён" : "Ошибка при экспорте");
        }
    }

    // Auto-start with Windows — reacts to CheckBox binding
    partial void OnIsAutoStartEnabledChanged(bool value)
    {
        if (_isInitializing) return;
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        if (key is null) return;

        if (value)
        {
            var exePath = Environment.ProcessPath ?? "";
            key.SetValue("DailyPlanner", $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue("DailyPlanner", false);
        }
    }

    // Backup database
    [RelayCommand]
    private void BackupDatabase()
    {
        var dialog = new SaveFileDialog
        {
            FileName = $"DailyPlanner_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db",
            DefaultExt = ".db",
            Filter = "SQLite DB (*.db)|*.db"
        };
        if (dialog.ShowDialog() != true) return;

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DailyPlanner", "planner.db");
        if (File.Exists(dbPath))
            File.Copy(dbPath, dialog.FileName, true);
    }

    // Restore database
    [RelayCommand]
    private async Task RestoreDatabaseAsync()
    {
        var dialog = new OpenFileDialog
        {
            DefaultExt = ".db",
            Filter = "SQLite DB (*.db)|*.db"
        };
        if (dialog.ShowDialog() != true) return;

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DailyPlanner", "planner.db");

        // Close all pooled SQLite connections before overwriting the file
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        File.Copy(dialog.FileName, dbPath, true);

        // Remove stale WAL/SHM files that belong to the old database
        var walPath = dbPath + "-wal";
        var shmPath = dbPath + "-shm";
        if (File.Exists(walPath)) File.Delete(walPath);
        if (File.Exists(shmPath)) File.Delete(shmPath);

        // Reload all data from the restored database
        await LoadMonthAsync();
        await LoadTemplatesAsync();
        await LoadRemindersAsync();
    }

    // Recurring templates
    [RelayCommand]
    private async Task LoadTemplatesAsync()
    {
        Templates.Clear();
        var templates = await _service.GetActiveTemplatesAsync();
        foreach (var t in templates) Templates.Add(t);
    }

    [RelayCommand]
    private async Task AddTemplateAsync()
    {
        var template = new RecurringTemplate();
        await _service.SaveTemplateAsync(template);
        Templates.Add(template);
    }

    [RelayCommand]
    private async Task RemoveTemplateAsync(RecurringTemplate? template)
    {
        if (template is null) return;
        await _service.RemoveTemplateAsync(template.Id);
        Templates.Remove(template);
    }

    // Category filter
    [RelayCommand]
    private void CycleFilter()
    {
        FilterCategory = FilterCategory switch
        {
            TaskCategory.None => TaskCategory.Work,
            TaskCategory.Work => TaskCategory.Study,
            TaskCategory.Study => TaskCategory.Personal,
            TaskCategory.Personal => TaskCategory.Health,
            TaskCategory.Health => TaskCategory.Other,
            TaskCategory.Other => TaskCategory.None,
            _ => TaskCategory.None
        };
    }

    // Copy previous week structure
    [RelayCommand]
    private async Task CopyPreviousWeekAsync()
    {
        if (SelectedWeek is null) return;
        var prevStart = SelectedWeek.StartDate.AddDays(-7);
        var pw = await _service.GetOrCreateWeekAsync(prevStart);
        await _service.CopyWeekStructureAsync(pw.Id, SelectedWeek.Model.Id);
        await LoadMonthAsync();
        NotificationService.ShowToast("Копирование", "Структура прошлой недели скопирована");
    }

    // Reminders
    [RelayCommand]
    private async Task LoadRemindersAsync()
    {
        Reminders.Clear();
        var reminders = await _service.GetRemindersAsync();
        foreach (var r in reminders) Reminders.Add(new ReminderViewModel(r, _service));
    }

    [RelayCommand]
    private async Task AddReminderAsync()
    {
        var reminder = new Reminder
        {
            Title = "Напоминание",
            Time = new TimeOnly(9, 0),
            IsEnabled = true
        };
        await _service.SaveReminderAsync(reminder);
        Reminders.Add(new ReminderViewModel(reminder, _service));
    }

    [RelayCommand]
    private async Task RemoveReminderAsync(ReminderViewModel? vm)
    {
        if (vm is null) return;
        await _service.RemoveReminderAsync(vm.Model.Id);
        Reminders.Remove(vm);
    }

    // Palette presets
    [ObservableProperty] private string _selectedThemePreset = ThemeService.CurrentPalette;

    public string[] ThemePresets { get; } = [.. ThemeService.Palettes.Keys];

    [RelayCommand]
    private void ApplyThemePreset(string? preset)
    {
        if (preset is null) return;
        SelectedThemePreset = preset;
        ThemeService.ApplyPalette(preset);
    }

    public async Task InitializeAsync()
    {
        _isInitializing = true;

        // Check auto-start registry
        using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
        {
            IsAutoStartEnabled = key?.GetValue("DailyPlanner") is not null;
        }

        _isInitializing = false;

        await LoadMonthAsync();
        await LoadTemplatesAsync();
        await LoadRemindersAsync();

        StartReminderCheck();

        // Check for updates in background
        CheckForUpdatesAsync().FireAndForget("UpdateCheck");
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        if (!_updateService.IsInstalled)
        {
            UpdateStatus = "Обновления недоступны (dev-режим)";
            return;
        }

        UpdateStatus = "Проверка обновлений...";
        var update = await _updateService.CheckForUpdatesAsync();

        if (update is null)
        {
            UpdateStatus = "Вы используете последнюю версию";
            IsUpdateAvailable = false;
        }
        else
        {
            UpdateStatus = $"Доступно обновление: v{update.TargetFullRelease.Version}";
            IsUpdateAvailable = true;
        }
    }

    [RelayCommand]
    private async Task DownloadAndInstallUpdateAsync()
    {
        if (!_updateService.IsInstalled) return;

        var update = await _updateService.CheckForUpdatesAsync();
        if (update is null) return;

        UpdateStatus = "Скачивание обновления...";
        UpdateProgress = 0;

        await _updateService.DownloadAndApplyAsync(update, p =>
        {
            UpdateProgress = p;
            UpdateStatus = $"Скачивание: {p}%";
        });
    }

    private System.Windows.Threading.DispatcherTimer? _reminderTimer;

    private void StartReminderCheck()
    {
        _reminderTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _reminderTimer.Tick += (_, _) => CheckReminders();
        _reminderTimer.Start();
    }

    private readonly HashSet<string> _firedReminders = [];
    private DateOnly _lastReminderDate = DateOnly.FromDateTime(DateTime.Today);

    private void CheckReminders()
    {
        var now = TimeOnly.FromDateTime(DateTime.Now);
        var today = DateTime.Today.DayOfWeek;
        var currentDate = DateOnly.FromDateTime(DateTime.Today);

        if (currentDate != _lastReminderDate)
        {
            _firedReminders.Clear();
            _lastReminderDate = currentDate;
        }

        var dateKey = currentDate.ToString();

        foreach (var vm in Reminders)
        {
            if (!vm.IsEnabled) continue;
            var r = vm.Model;
            if (r.DayOfWeek is not null && r.DayOfWeek != today) continue;

            var diff = Math.Abs((now - r.Time).TotalMinutes);
            if (diff > 1) continue;

            var key = $"{dateKey}:{r.Id}";
            if (!_firedReminders.Add(key)) continue;

            NotificationService.ShowToast(r.Title, r.Message);
        }
    }
}

public sealed record MonthItem(int Number, string Name);

public sealed record SearchResultItem(string Text, string Context, string Week, int WeekIndex, DateOnly? DayDate);
