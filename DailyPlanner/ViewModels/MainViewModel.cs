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
    [ObservableProperty] private bool _isStatisticsOpen;
    [ObservableProperty] private bool _isAutoStartEnabled;
    [ObservableProperty] private TaskCategory _filterCategory = TaskCategory.None;
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<WeekViewModel> Weeks { get; } = [];
    public ObservableCollection<SearchResultItem> SearchResults { get; } = [];
    public ObservableCollection<RecurringTemplate> Templates { get; } = [];
    public ObservableCollection<ReminderViewModel> Reminders { get; } = [];
    public ObservableCollection<MeetingViewModel> Meetings { get; } = [];

    public PomodoroViewModel Pomodoro { get; } = new();
    public StatisticsViewModel Statistics { get; }
    public PlannerService Service => _service;
    public string DailyQuote { get; private set; } = QuoteService.GetDailyQuote();
    public string AppVersion { get; } = typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "?";

    private readonly UpdateService _updateService = new("https://github.com/valinerosgordov/DailyPlanner");
    [ObservableProperty] private string _updateStatus = string.Empty;
    [ObservableProperty] private bool _isUpdateAvailable;
    [ObservableProperty] private int _updateProgress;

    private bool _isInitializing;

    public MainViewModel()
    {
        Statistics = new StatisticsViewModel(_service);
        Loc.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        for (var i = 0; i < _months.Length; i++)
            _months[i] = new MonthItem(i + 1, Loc.GetMonthName(i + 1));
        OnPropertyChanged(nameof(Months));
        OnPropertyChanged(nameof(SelectedMonthName));

        DailyQuote = QuoteService.GetDailyQuote();
        OnPropertyChanged(nameof(DailyQuote));
        OnPropertyChanged(nameof(TodayTasks));
        OnPropertyChanged(nameof(TodayProgress));

        if (SelectedWeek is not null)
        {
            foreach (var day in SelectedWeek.Days)
                day.RefreshLocalization();
            SelectedWeek.RefreshLocalization();
        }
    }

    private readonly MonthItem[] _months = Enumerable.Range(1, 12)
        .Select(i => new MonthItem(i, Loc.GetMonthName(i))).ToArray();

    public MonthItem[] Months => _months;

    public string SelectedMonthName => Loc.GetMonthName(SelectedMonth);

    [RelayCommand]
    public async Task LoadMonthAsync()
    {
        IsLoading = true;
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
        IsLoading = false;
    }

    public string TodayTasks
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var day = SelectedWeek?.Days.FirstOrDefault(d => d.Date == today);
            if (day is null) return Loc.Get("NoTasksToday");
            var tasks = day.Tasks
                .Where(t => !string.IsNullOrWhiteSpace(t.Text))
                .Select(t => (t.IsCompleted ? "\u2705 " : "\u25cb ") + t.Text)
                .ToList();
            return tasks.Count > 0 ? string.Join("\n", tasks.Take(5)) : Loc.Get("NoTasks");
        }
    }

    public string TodayProgress
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var day = SelectedWeek?.Days.FirstOrDefault(d => d.Date == today);
            if (day is null) return "";
            return $"{day.CompletedCount}/{day.TotalWithText} {Loc.Get("Completed")}";
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
                    foreach (var sub in task.SubTasks)
                    {
                        if (!string.IsNullOrWhiteSpace(sub.Text) &&
                            sub.Text.Contains(q, StringComparison.OrdinalIgnoreCase))
                        {
                            SearchResults.Add(new SearchResultItem(
                                $"  └ {sub.Text}", $"{day.DayName} ({day.DateFormatted})", week.WeekRange, i, day.Date));
                        }
                    }
                }
            }

            foreach (var goal in week.Goals)
            {
                if (!string.IsNullOrWhiteSpace(goal.Text) &&
                    goal.Text.Contains(q, StringComparison.OrdinalIgnoreCase))
                {
                    SearchResults.Add(new SearchResultItem(
                        goal.Text, Loc.Get("Goal"), week.WeekRange, i, null));
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
            FileName = string.Format(Loc.Get("PlannerFileName"), SelectedWeek.StartDate.ToString("yyyy-MM-dd")),
            DefaultExt = ".xlsx",
            Filter = Loc.Get("ExcelFilter")
        };

        if (dialog.ShowDialog() == true)
        {
            var ok = ExportService.ExportWeekToExcel(SelectedWeek.Model, dialog.FileName);
            NotificationService.ShowToast(Loc.Get("ExportTitle"),
                ok ? Loc.Get("ExportSuccess") : Loc.Get("ExportError"));
        }
    }

    partial void OnIsAutoStartEnabledChanged(bool value)
    {
        if (_isInitializing) return;
        try
        {
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
        catch (System.Security.SecurityException)
        {
            // Non-admin users may not have registry write access
        }
    }

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

        // Validate the backup file is a valid SQLite database
        try
        {
            await using var testDb = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dialog.FileName};Mode=ReadOnly");
            await testDb.OpenAsync();
            await testDb.CloseAsync();
        }
        catch
        {
            NotificationService.ShowToast(Loc.Get("RestoreTitle"), Loc.Get("RestoreInvalidDb"));
            return;
        }

        // Create safety backup before overwriting
        var backupPath = dbPath + ".pre-restore";
        try
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            if (File.Exists(dbPath))
                File.Copy(dbPath, backupPath, true);

            File.Copy(dialog.FileName, dbPath, true);

            var walPath = dbPath + "-wal";
            var shmPath = dbPath + "-shm";
            if (File.Exists(walPath)) File.Delete(walPath);
            if (File.Exists(shmPath)) File.Delete(shmPath);
        }
        catch
        {
            // Restore from safety backup if copy failed
            if (File.Exists(backupPath))
                File.Copy(backupPath, dbPath, true);
            NotificationService.ShowToast(Loc.Get("RestoreTitle"), Loc.Get("RestoreError"));
            return;
        }

        await LoadMonthAsync();
        await LoadTemplatesAsync();
        await LoadRemindersAsync();
        await LoadMeetingsAsync();
        NotificationService.ShowToast(Loc.Get("RestoreTitle"), Loc.Get("RestoreSuccess"));
    }

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

    [RelayCommand]
    private async Task CopyPreviousWeekAsync()
    {
        if (SelectedWeek is null) return;
        var prevStart = SelectedWeek.StartDate.AddDays(-7);
        var pw = await _service.GetOrCreateWeekAsync(prevStart);
        await _service.CopyWeekStructureAsync(pw.Id, SelectedWeek.Model.Id);
        await LoadMonthAsync();
        NotificationService.ShowToast(Loc.Get("CopyTitle"), Loc.Get("CopySuccess"));
    }

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
            Title = Loc.Get("Reminder"),
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

    [RelayCommand]
    private async Task LoadMeetingsAsync()
    {
        Meetings.Clear();
        var meetings = await _service.GetMeetingsAsync();
        foreach (var m in meetings) Meetings.Add(new MeetingViewModel(m, _service));
    }

    [RelayCommand]
    private async Task AddMeetingAsync()
    {
        var meeting = new Meeting
        {
            Title = Loc.Get("NewMeeting"),
            DateTime = DateTime.Today.AddDays(1).AddHours(10),
            DurationMinutes = 60,
            NotifyDayBefore = true,
            NotifyTwoHoursBefore = true,
            Notify30MinBefore = true
        };
        await _service.SaveMeetingAsync(meeting);
        Meetings.Add(new MeetingViewModel(meeting, _service));
    }

    [RelayCommand]
    private async Task RemoveMeetingAsync(MeetingViewModel? vm)
    {
        if (vm is null) return;
        await _service.RemoveMeetingAsync(vm.Model.Id);
        Meetings.Remove(vm);
    }

    [ObservableProperty] private string _selectedThemePreset = ThemeService.CurrentPalette;
    public string[] ThemePresets { get; } = [.. ThemeService.Palettes.Keys];

    [RelayCommand]
    private void ApplyThemePreset(string? preset)
    {
        if (preset is null) return;
        SelectedThemePreset = preset;
        ThemeService.ApplyPalette(preset);
    }

    // Language
    public LanguageItem[] LanguageItems { get; } = Loc.SupportedLanguages
        .Select(l => new LanguageItem(l, Loc.LanguageNames[l])).ToArray();

    [ObservableProperty] private string _selectedLanguage = Loc.Instance.Language;

    partial void OnSelectedLanguageChanged(string value)
    {
        Loc.Instance.Language = value;
    }

    public async Task InitializeAsync()
    {
        _isInitializing = true;

        using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
        {
            IsAutoStartEnabled = key?.GetValue("DailyPlanner") is not null;
        }

        _isInitializing = false;

        await LoadMonthAsync();
        await LoadTemplatesAsync();
        await LoadRemindersAsync();
        await LoadMeetingsAsync();

        StartReminderCheck();
        CheckForUpdatesAsync().FireAndForget("UpdateCheck");
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        if (!_updateService.IsInstalled)
        {
            UpdateStatus = Loc.Get("UpdateUnavailable");
            return;
        }

        UpdateStatus = Loc.Get("UpdateChecking");
        var update = await _updateService.CheckForUpdatesAsync();

        if (update is null)
        {
            UpdateStatus = Loc.Get("UpdateLatest");
            IsUpdateAvailable = false;
        }
        else
        {
            UpdateStatus = string.Format(Loc.Get("UpdateAvailable"), update.TargetFullRelease.Version);
            IsUpdateAvailable = true;
        }
    }

    [RelayCommand]
    private async Task DownloadAndInstallUpdateAsync()
    {
        if (!_updateService.IsInstalled) return;

        var update = await _updateService.CheckForUpdatesAsync();
        if (update is null) return;

        UpdateStatus = Loc.Get("UpdateDownloading");
        UpdateProgress = 0;

        await _updateService.DownloadAndApplyAsync(update, p =>
        {
            UpdateProgress = p;
            UpdateStatus = string.Format(Loc.Get("UpdateProgress"), p);
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

            var diff = (now - r.Time).TotalMinutes;
            if (diff < 0 || diff > 1.5) continue;

            var key = $"{dateKey}:{r.Id}";
            if (!_firedReminders.Add(key)) continue;

            NotificationService.ShowToast(r.Title, r.Message);
        }

        CheckMeetingReminders();
    }

    private void CheckMeetingReminders()
    {
        var now = DateTime.Now;

        foreach (var vm in Meetings)
        {
            var m = vm.Model;
            var meetingTime = m.DateTime;

            // Notify 1 day before
            if (m.NotifyDayBefore)
            {
                var dayBefore = meetingTime.AddDays(-1);
                if (Math.Abs((now - dayBefore).TotalMinutes) < 1)
                {
                    var key = $"meeting-day:{m.Id}:{meetingTime:yyyyMMdd}";
                    if (_firedReminders.Add(key))
                    {
                        NotificationService.ShowToast(
                            Loc.Get("MeetingTomorrow"),
                            $"{m.Title} — {meetingTime:HH:mm}\n{m.Attendees}");
                    }
                }
            }

            // Notify 2 hours before
            if (m.NotifyTwoHoursBefore)
            {
                var twoHours = meetingTime.AddHours(-2);
                if (Math.Abs((now - twoHours).TotalMinutes) < 1)
                {
                    var key = $"meeting-2h:{m.Id}:{meetingTime:yyyyMMdd}";
                    if (_firedReminders.Add(key))
                    {
                        NotificationService.ShowToast(
                            Loc.Get("MeetingSoon"),
                            $"{m.Title} — {Loc.Get("MeetingIn2Hours")}\n{m.Attendees}");
                    }
                }
            }

            // Notify 30 minutes before
            if (m.Notify30MinBefore)
            {
                var thirtyMin = meetingTime.AddMinutes(-30);
                if (Math.Abs((now - thirtyMin).TotalMinutes) < 1)
                {
                    var key = $"meeting-30m:{m.Id}:{meetingTime:yyyyMMdd}";
                    if (_firedReminders.Add(key))
                    {
                        NotificationService.ShowToast(
                            Loc.Get("MeetingSoon"),
                            $"{m.Title} — {Loc.Get("MeetingIn30Min")}\n{m.Attendees}");
                    }
                }
            }
        }
    }
}

public sealed record MonthItem(int Number, string Name);
public sealed record LanguageItem(string Code, string DisplayName);
public sealed record SearchResultItem(string Text, string Context, string Week, int WeekIndex, DateOnly? DayDate);
