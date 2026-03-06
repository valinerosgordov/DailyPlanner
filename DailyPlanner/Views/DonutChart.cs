using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DailyPlanner.Views;

public sealed class DonutChart : Control
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(DonutChart),
            new PropertyMetadata(0.0, OnValueChanged));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(DonutChart),
            new PropertyMetadata(100.0, OnRenderPropertyChanged));

    public static readonly DependencyProperty RingThicknessProperty =
        DependencyProperty.Register(nameof(RingThickness), typeof(double), typeof(DonutChart),
            new PropertyMetadata(8.0, OnRenderPropertyChanged));

    public static readonly DependencyProperty TrackBrushProperty =
        DependencyProperty.Register(nameof(TrackBrush), typeof(Brush), typeof(DonutChart),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x24, 0x24, 0x34)), OnRenderPropertyChanged));

    public static readonly DependencyProperty ValueBrushProperty =
        DependencyProperty.Register(nameof(ValueBrush), typeof(Brush), typeof(DonutChart),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x7C, 0x5C, 0xFC)), OnRenderPropertyChanged));

    public static readonly DependencyProperty CenterTextProperty =
        DependencyProperty.Register(nameof(CenterText), typeof(string), typeof(DonutChart),
            new PropertyMetadata(string.Empty, OnRenderPropertyChanged));

    public static readonly DependencyProperty SubTextProperty =
        DependencyProperty.Register(nameof(SubText), typeof(string), typeof(DonutChart),
            new PropertyMetadata(string.Empty, OnRenderPropertyChanged));

    // Internal animated value
    private static readonly DependencyProperty AnimatedValueProperty =
        DependencyProperty.Register(nameof(AnimatedValue), typeof(double), typeof(DonutChart),
            new PropertyMetadata(0.0, OnRenderPropertyChanged));

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double RingThickness { get => (double)GetValue(RingThicknessProperty); set => SetValue(RingThicknessProperty, value); }
    public Brush TrackBrush { get => (Brush)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
    public Brush ValueBrush { get => (Brush)GetValue(ValueBrushProperty); set => SetValue(ValueBrushProperty, value); }
    public string CenterText { get => (string)GetValue(CenterTextProperty); set => SetValue(CenterTextProperty, value); }
    public string SubText { get => (string)GetValue(SubTextProperty); set => SetValue(SubTextProperty, value); }
    private double AnimatedValue { get => (double)GetValue(AnimatedValueProperty); set => SetValue(AnimatedValueProperty, value); }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DonutChart chart)
            chart.AnimateToValue((double)e.NewValue);
    }

    private void AnimateToValue(double target)
    {
        var animation = new DoubleAnimation
        {
            To = target,
            Duration = TimeSpan.FromMilliseconds(600),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(AnimatedValueProperty, animation);
    }

    private static void OnRenderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DonutChart chart)
            chart.InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0) return;

        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var radius = (size - RingThickness) / 2;
        var trackPen = new Pen(TrackBrush, RingThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var valuePen = new Pen(ValueBrush, RingThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

        // Track circle
        dc.DrawEllipse(null, trackPen, center, radius, radius);

        // Value arc (uses animated value)
        var fraction = Maximum > 0 ? Math.Clamp(AnimatedValue / Maximum, 0, 1) : 0;
        if (fraction > 0.001)
        {
            var startAngle = -90.0;
            var sweepAngle = fraction * 360;

            var startRad = startAngle * Math.PI / 180;
            var endRad = (startAngle + sweepAngle) * Math.PI / 180;

            var startPoint = new Point(
                center.X + radius * Math.Cos(startRad),
                center.Y + radius * Math.Sin(startRad));
            var endPoint = new Point(
                center.X + radius * Math.Cos(endRad),
                center.Y + radius * Math.Sin(endRad));

            var isLargeArc = sweepAngle > 180;

            if (fraction >= 0.999)
            {
                dc.DrawEllipse(null, valuePen, center, radius, radius);
            }
            else
            {
                var arcSegment = new ArcSegment(endPoint, new Size(radius, radius), 0, isLargeArc, SweepDirection.Clockwise, true);
                var figure = new PathFigure(startPoint, [arcSegment], false);
                var geometry = new PathGeometry([figure]);
                dc.DrawGeometry(null, valuePen, geometry);
            }
        }

        // Center text
        if (!string.IsNullOrEmpty(CenterText))
        {
            var typeface = new Typeface(new FontFamily("Segoe UI Variable, Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var text = new FormattedText(CenterText, System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, size * 0.22, Foreground ?? Brushes.White,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            text.TextAlignment = TextAlignment.Center;

            var textY = string.IsNullOrEmpty(SubText) ? center.Y - text.Height / 2 : center.Y - text.Height * 0.7;
            dc.DrawText(text, new Point(center.X, textY));
        }

        // Sub text
        if (!string.IsNullOrEmpty(SubText))
        {
            var typeface = new Typeface(new FontFamily("Segoe UI Variable, Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var subBrush = TryFindResource("MutedBrush") as Brush ?? new SolidColorBrush(Color.FromRgb(0x98, 0x98, 0xB0));
            var text = new FormattedText(SubText, System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, size * 0.11, subBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            text.TextAlignment = TextAlignment.Center;
            dc.DrawText(text, new Point(center.X, center.Y + text.Height * 0.3));
        }
    }
}
