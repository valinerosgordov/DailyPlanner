using System.Windows;
using DailyPlanner.Models;
using DailyPlanner.ViewModels;

namespace DailyPlanner.Views;

public partial class WeeklyReviewDialog : Window
{
    private readonly WeeklyReviewViewModel _vm = new();

    public WeeklyReviewDialog(PlannerWeek week)
    {
        InitializeComponent();
        _vm.LoadFrom(week);
        DataContext = _vm;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
