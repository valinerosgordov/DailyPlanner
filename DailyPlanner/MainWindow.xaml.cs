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
        };
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
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

    private void Settings_Click(object sender, RoutedEventArgs e)
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
            _viewModel.Statistics.SelectedYear = _viewModel.SelectedYear;
            _viewModel.Statistics.SelectedMonth = _viewModel.SelectedMonth;
            _statisticsPage = new StatisticsPage(_viewModel.Statistics);
            NavigateWithAnimation(_statisticsPage);
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
