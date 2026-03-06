using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DailyPlanner.Views;

public partial class RatingControl : UserControl
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(int), typeof(RatingControl),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.Register(nameof(MaxValue), typeof(int), typeof(RatingControl),
            new PropertyMetadata(5, OnValueChanged));

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int MaxValue
    {
        get => (int)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public RatingControl()
    {
        InitializeComponent();
        Loaded += (_, _) => RenderStars();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RatingControl control)
            control.RenderStars();
    }

    private void RenderStars()
    {
        StarsPanel.Children.Clear();
        var accent = TryFindResource("AccentBrush") as SolidColorBrush ?? Brushes.Purple;
        var muted = TryFindResource("MutedBrush") as SolidColorBrush ?? Brushes.Gray;

        for (var i = 1; i <= MaxValue; i++)
        {
            var index = i;
            var btn = new Button
            {
                Content = i <= Value ? "\u2605" : "\u2606",
                Foreground = i <= Value ? accent : muted,
                Style = FindResource("RatingButton") as Style
            };
            btn.Click += (_, _) =>
            {
                Value = Value == index ? index - 1 : index;
            };
            StarsPanel.Children.Add(btn);
        }
    }
}
