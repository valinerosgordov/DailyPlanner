using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DailyPlanner.Converters;

public sealed class BoolToStrikethroughConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? TextDecorations.Strikethrough : null!;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? 0.5 : 1.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ProgressToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is int completed && values[1] is int total && total > 0)
            return (double)completed / total * 100;
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class RatingToStarsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var rating = value is int r ? r : 0;
        var maxStars = parameter is string p && int.TryParse(p, out var m) ? m : 5;
        return string.Concat(Enumerable.Repeat("\u2605", rating))
             + string.Concat(Enumerable.Repeat("\u2606", maxStars - rating));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

public sealed class MonthIsSelectedConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is int monthNumber && values[1] is int selectedMonth)
            return monthNumber == selectedMonth;
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class DoubleToPercentWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percent)
            return new System.Windows.GridLength(Math.Max(percent, 0), System.Windows.GridUnitType.Star);
        return new System.Windows.GridLength(0, System.Windows.GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class PriorityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is DailyPlanner.Models.TaskPriority p ? p switch
        {
            DailyPlanner.Models.TaskPriority.High => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFB, 0x71, 0x85)),
            DailyPlanner.Models.TaskPriority.Medium => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFB, 0xBF, 0x24)),
            DailyPlanner.Models.TaskPriority.Low => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x34, 0xD3, 0x99)),
            _ => System.Windows.Media.Brushes.Transparent
        } : System.Windows.Media.Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class PriorityToSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is DailyPlanner.Models.TaskPriority p ? p switch
        {
            DailyPlanner.Models.TaskPriority.High => "!!!",
            DailyPlanner.Models.TaskPriority.Medium => "!!",
            DailyPlanner.Models.TaskPriority.Low => "!",
            _ => ""
        } : "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class CategoryToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is DailyPlanner.Models.TaskCategory c ? c switch
        {
            DailyPlanner.Models.TaskCategory.Work => "\uE821",
            DailyPlanner.Models.TaskCategory.Study => "\uE82D",
            DailyPlanner.Models.TaskCategory.Personal => "\uE77B",
            DailyPlanner.Models.TaskCategory.Health => "\uE95E",
            DailyPlanner.Models.TaskCategory.Other => "\uE72D",
            _ => ""
        } : "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class CategoryToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is DailyPlanner.Models.TaskCategory c ? c switch
        {
            DailyPlanner.Models.TaskCategory.Work => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x38, 0xBD, 0xF8)),
            DailyPlanner.Models.TaskCategory.Study => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xA7, 0x8B, 0xFA)),
            DailyPlanner.Models.TaskCategory.Personal => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFB, 0xBF, 0x24)),
            DailyPlanner.Models.TaskCategory.Health => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x34, 0xD3, 0x99)),
            DailyPlanner.Models.TaskCategory.Other => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x58, 0x58, 0x78)),
            _ => System.Windows.Media.Brushes.Transparent
        } : System.Windows.Media.Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class HeatmapIntensityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double intensity)
        {
            if (intensity <= 0) return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x1E, 0x2E));
            if (intensity < 0.33) return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2D, 0x40, 0x2D));
            if (intensity < 0.66) return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x26, 0x6B, 0x26));
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x34, 0xD3, 0x99));
        }
        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x1E, 0x2E));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class NonZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DailyPlanner.Models.TaskPriority p) return p != DailyPlanner.Models.TaskPriority.None ? Visibility.Visible : Visibility.Collapsed;
        if (value is DailyPlanner.Models.TaskCategory c) return c != DailyPlanner.Models.TaskCategory.None ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToPlayPauseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "\u23F8 Пауза" : "\u25B6 Старт";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToThemeLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "Текущая: Тёмная" : "Текущая: Светлая";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
