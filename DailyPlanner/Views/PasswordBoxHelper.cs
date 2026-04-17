using System.Windows;
using System.Windows.Controls;

namespace DailyPlanner.Views;

/// <summary>
/// Enables TwoWay binding for PasswordBox.Password via attached property.
/// Usage in XAML:  behaviors:PasswordBoxHelper.BoundPassword="{Binding MyPassword, Mode=TwoWay}"
/// </summary>
public static class PasswordBoxHelper
{
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxHelper),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

    private static readonly DependencyProperty _updatingProperty =
        DependencyProperty.RegisterAttached("_updating", typeof(bool), typeof(PasswordBoxHelper), new PropertyMetadata(false));

    public static string GetBoundPassword(DependencyObject d) => (string)d.GetValue(BoundPasswordProperty);
    public static void SetBoundPassword(DependencyObject d, string value) => d.SetValue(BoundPasswordProperty, value);

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box) return;
        if ((bool)box.GetValue(_updatingProperty)) return;

        // Ensure the handler is attached exactly once
        box.PasswordChanged -= OnPasswordChanged;
        box.Password = (string?)e.NewValue ?? string.Empty;
        box.PasswordChanged += OnPasswordChanged;

        // First attach to a blank PasswordBox
        if (e.OldValue is null && string.IsNullOrEmpty(box.Password))
            box.PasswordChanged += OnPasswordChanged;
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox box) return;
        box.SetValue(_updatingProperty, true);
        SetBoundPassword(box, box.Password);
        box.SetValue(_updatingProperty, false);
    }

    public static void Attach(PasswordBox box) => box.PasswordChanged += OnPasswordChanged;
}
