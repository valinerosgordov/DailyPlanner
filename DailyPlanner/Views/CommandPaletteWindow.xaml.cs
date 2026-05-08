using System.Windows;
using System.Windows.Input;
using DailyPlanner.ViewModels;

namespace DailyPlanner.Views;

public partial class CommandPaletteWindow : Window
{
    public CommandPaletteWindow(CommandPaletteViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.RequestClose += OnRequestClose;
        Loaded += (_, _) =>
        {
            QueryBox.Focus();
            Keyboard.Focus(QueryBox);
        };
    }

    private void OnRequestClose(object? sender, bool execute)
    {
        DialogResult = execute;
        Close();
    }
}
