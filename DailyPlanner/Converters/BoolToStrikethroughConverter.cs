using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DailyPlanner.Converters;

public sealed class BoolToStrikethroughConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? TextDecorations.Strikethrough : null;

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


public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

public sealed class InvertedBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
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
        var res = Application.Current?.Resources;
        var trackColor = res?["ProgressTrackBrush"] is System.Windows.Media.SolidColorBrush track
            ? track.Color
            : System.Windows.Media.Color.FromRgb(0x1E, 0x1E, 0x2E);
        var successColor = res?["SuccessBrush"] is System.Windows.Media.SolidColorBrush success
            ? success.Color
            : System.Windows.Media.Color.FromRgb(0x34, 0xD3, 0x99);

        if (value is double intensity)
        {
            if (intensity <= 0) return new System.Windows.Media.SolidColorBrush(trackColor);
            if (intensity < 0.33) return new System.Windows.Media.SolidColorBrush(BlendColors(trackColor, successColor, 0.25));
            if (intensity < 0.66) return new System.Windows.Media.SolidColorBrush(BlendColors(trackColor, successColor, 0.55));
            return new System.Windows.Media.SolidColorBrush(successColor);
        }
        return new System.Windows.Media.SolidColorBrush(trackColor);
    }

    private static System.Windows.Media.Color BlendColors(System.Windows.Media.Color a, System.Windows.Media.Color b, double t)
        => System.Windows.Media.Color.FromRgb(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));

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

public sealed class PriorityToTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is DailyPlanner.Models.TaskPriority p ? p switch
        {
            DailyPlanner.Models.TaskPriority.High => "Высокий приоритет (клик — сменить)",
            DailyPlanner.Models.TaskPriority.Medium => "Средний приоритет (клик — сменить)",
            DailyPlanner.Models.TaskPriority.Low => "Низкий приоритет (клик — сменить)",
            _ => "Без приоритета"
        } : "Без приоритета";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class CategoryToTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is DailyPlanner.Models.TaskCategory c ? c switch
        {
            DailyPlanner.Models.TaskCategory.Work => "Работа (клик — сменить)",
            DailyPlanner.Models.TaskCategory.Study => "Учёба (клик — сменить)",
            DailyPlanner.Models.TaskCategory.Personal => "Личное (клик — сменить)",
            DailyPlanner.Models.TaskCategory.Health => "Здоровье (клик — сменить)",
            DailyPlanner.Models.TaskCategory.Other => "Другое (клик — сменить)",
            _ => ""
        } : "";

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

