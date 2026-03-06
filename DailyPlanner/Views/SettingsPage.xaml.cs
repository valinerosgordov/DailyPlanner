using System.Windows.Controls;
using DailyPlanner.ViewModels;

namespace DailyPlanner.Views;

public partial class SettingsPage : Page
{
    public SettingsPage(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
