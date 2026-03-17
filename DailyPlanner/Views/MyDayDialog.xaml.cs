using System.Windows;

namespace DailyPlanner.Views;

public partial class MyDayDialog : Window
{
    public MyDayDialog()
    {
        InitializeComponent();
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
