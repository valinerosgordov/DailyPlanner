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

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Handled via binding — SelectedLanguage -> OnSelectedLanguageChanged -> Loc.Instance.Language
    }
}
