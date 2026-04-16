using System.Diagnostics;
using System.Windows.Controls;
using DailyPlanner.ViewModels;

namespace DailyPlanner.Views;

public partial class StatisticsPage : Page
{
    public StatisticsPage(StatisticsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) =>
        {
            try
            {
                await viewModel.LoadDataAsync();
            }
            catch (Exception ex)
            {
                Log.Error("StatisticsPage", $"Failed to load data: {ex.Message}");
            }
        };
    }
}
