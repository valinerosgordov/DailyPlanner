using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using DailyPlanner.ViewModels;

namespace DailyPlanner.Views;

public partial class FinancePage : Page
{
    public FinancePage(FinanceViewModel viewModel)
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
                Debug.WriteLine($"[FinancePage] Failed to load data: {ex.Message}");
            }
        };
    }

    private void TabChanged(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag && int.TryParse(tag, out var index))
        {
            if (DataContext is FinanceViewModel vm)
                vm.SelectedTabIndex = index;
        }
    }
}
