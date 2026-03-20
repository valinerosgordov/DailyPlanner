using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.Converters;

public sealed class FinanceTypeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is FinanceEntryType t
            ? t == FinanceEntryType.Income
                ? new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99))
                : new SolidColorBrush(Color.FromRgb(0xFB, 0x71, 0x85))
            : Brushes.Transparent;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class FinanceTypeToSignConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is FinanceEntryType t
            ? t == FinanceEntryType.Income ? "+" : "-"
            : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class DebtDirectionToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is DebtDirection d
            ? d == DebtDirection.Lent ? Loc.Get("DebtLent") : Loc.Get("DebtBorrowed")
            : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BudgetProgressToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double percent) return Brushes.Transparent;
        return percent switch
        {
            >= 100 => new SolidColorBrush(Color.FromRgb(0xFB, 0x71, 0x85)),
            >= 80 => new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)),
            _ => new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class DecimalToCurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is decimal d ? $"{d:N2}" : "0.00";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
        return 0m;
    }
}

public sealed class BoolToPaidStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Loc.Get("Paid") : Loc.Get("Pending");

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
