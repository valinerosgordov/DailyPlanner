using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using DailyPlanner.Services;
using DailyPlanner.ViewModels;
using DailyPlanner.Views;
using Wpf.Ui.Controls;

namespace DailyPlanner;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel = new();
    private readonly WeekPage _weekPage = new();
    private readonly SettingsPage _settingsPage;
    private StatisticsPage? _statisticsPage;
    private FinancePage? _financePage;
    private InboxPage? _inboxPage;
    private System.Windows.Forms.NotifyIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _weekPage.DataContext = _viewModel;
        _settingsPage = new SettingsPage(_viewModel);

        SetupTrayIcon();

        Loaded += async (_, _) =>
        {
            await _viewModel.InitializeAsync();
            NavigateWithAnimation(_weekPage);
            NotificationService.Start();
            ShowMyDayDialog();
        };

        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                _trayIcon?.ShowBalloonTip(1000, "Daily Planner",
                    Loc.Get("TrayMinimized"), System.Windows.Forms.ToolTipIcon.Info);
            }
        };

        Closed += (_, _) =>
        {
            _trayIcon?.Dispose();
            NotificationService.Stop();
            _viewModel.Cleanup();
        };
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = Environment.ProcessPath is { } path ? System.Drawing.Icon.ExtractAssociatedIcon(path) : null,
            Visible = true,
            Text = "Daily Planner"
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add(Loc.Get("TrayOpen"), null, (_, _) => ShowFromTray());
        menu.Items.Add(Loc.Get("TrayToday"), null, (_, _) =>
        {
            ShowFromTray();
            _viewModel.GoToTodayCommand.Execute(null);
        });
        menu.Items.Add("-");
        menu.Items.Add(Loc.Get("TrayExit"), null, (_, _) =>
        {
            _trayIcon.Visible = false;
            Application.Current.Shutdown();
        });

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowFromTray();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void WeekTab_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is WeekViewModel week)
            _viewModel.SelectedWeek = week;
    }

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (ContentFrame.Content is SettingsPage)
        {
            _viewModel.IsSettingsOpen = false;
            NavigateWithAnimation(_weekPage);
        }
        else
        {
            _viewModel.IsSettingsOpen = true;
            _viewModel.IsStatisticsOpen = false;
            _viewModel.IsFinanceOpen = false;
            _viewModel.IsInboxOpen = false;
            try { await _viewModel.LoadTrelloSettingsAsync(); } catch { }
            NavigateWithAnimation(_settingsPage);
        }
    }

    private void Statistics_Click(object sender, RoutedEventArgs e)
    {
        if (ContentFrame.Content is StatisticsPage)
        {
            _viewModel.IsStatisticsOpen = false;
            NavigateWithAnimation(_weekPage);
        }
        else
        {
            _viewModel.IsStatisticsOpen = true;
            _viewModel.IsSettingsOpen = false;
            _viewModel.IsFinanceOpen = false;
            _viewModel.IsInboxOpen = false;
            _viewModel.Statistics.SelectedYear = _viewModel.SelectedYear;
            _viewModel.Statistics.SelectedMonth = _viewModel.SelectedMonth;
            _statisticsPage = new StatisticsPage(_viewModel.Statistics);
            NavigateWithAnimation(_statisticsPage);
        }
    }

    private void Finance_Click(object sender, RoutedEventArgs e)
    {
        if (ContentFrame.Content is FinancePage)
        {
            _viewModel.IsFinanceOpen = false;
            NavigateWithAnimation(_weekPage);
        }
        else
        {
            _viewModel.IsFinanceOpen = true;
            _viewModel.IsSettingsOpen = false;
            _viewModel.IsStatisticsOpen = false;
            _viewModel.IsInboxOpen = false;
            _viewModel.Finance.SelectedYear = _viewModel.SelectedYear;
            _viewModel.Finance.SelectedMonth = _viewModel.SelectedMonth;
            _financePage = new FinancePage(_viewModel.Finance);
            NavigateWithAnimation(_financePage);
        }
    }

    private void Inbox_Click(object sender, RoutedEventArgs e)
    {
        if (ContentFrame.Content is InboxPage)
        {
            _viewModel.IsInboxOpen = false;
            NavigateWithAnimation(_weekPage);
        }
        else
        {
            _viewModel.IsInboxOpen = true;
            _viewModel.IsSettingsOpen = false;
            _viewModel.IsStatisticsOpen = false;
            _viewModel.IsFinanceOpen = false;
            var vm = new InboxViewModel(_viewModel.Service, _viewModel.TrelloService);
            _inboxPage = new InboxPage(vm);
            NavigateWithAnimation(_inboxPage);
        }
    }

    private void SearchResult_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ViewModels.SearchResultItem item })
            _viewModel.NavigateToSearchResultCommand.Execute(item);
    }

    private void MonthBorder_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is int month)
            _viewModel.SelectMonthCommand.Execute(month);
    }

    private void ShowMyDayDialog()
    {
        // Check if user disabled it
        var settingsPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DailyPlanner", "myday.txt");
        try
        {
            if (System.IO.File.Exists(settingsPath))
            {
                var saved = System.IO.File.ReadAllText(settingsPath).Trim();
                if (saved == DateOnly.FromDateTime(DateTime.Today).ToString()) return;
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindow] MyDay settings: {ex.Message}"); }

        if (_viewModel.SelectedWeek is null) return;

        try
        {
            var vm = new ViewModels.MyDayViewModel(_viewModel.SelectedWeek);
            var dialog = new MyDayDialog { DataContext = vm, Owner = this };

            // Apply theme resources safely
            if (Application.Current?.Resources?.MergedDictionaries is { Count: > 0 } merged)
            {
                foreach (var dict in merged)
                    dialog.Resources.MergedDictionaries.Add(dict);
            }

            dialog.ShowDialog();

            if (vm.DontShowAgain)
            {
                try
                {
                    var dir = System.IO.Path.GetDirectoryName(settingsPath)!;
                    System.IO.Directory.CreateDirectory(dir);
                    System.IO.File.WriteAllText(settingsPath, DateOnly.FromDateTime(DateTime.Today).ToString());
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindow] MyDay save: {ex.Message}"); }
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindow] MyDay dialog: {ex.Message}"); }
    }

    private void NavigateWithAnimation(Page page)
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100));
        fadeOut.Completed += (_, _) =>
        {
            ContentFrame.Navigate(page);
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            ContentFrame.BeginAnimation(OpacityProperty, fadeIn);
        };
        ContentFrame.BeginAnimation(OpacityProperty, fadeOut);
    }
}
