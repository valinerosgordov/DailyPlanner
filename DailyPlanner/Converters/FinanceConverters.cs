using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.Converters;

public sealed class FinanceTypeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not FinanceEntryType t) return Brushes.Transparent;
        var key = t == FinanceEntryType.Income ? "SuccessBrush" : "DangerBrush";
        return Application.Current?.Resources[key] as Brush ?? Brushes.Transparent;
    }

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

/// <summary>Converts int == parameter to bool (for RadioButton IsChecked).</summary>
public sealed class IntEqualConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int v && parameter is string s && int.TryParse(s, out var p))
            return v == p;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string s && int.TryParse(s, out var p))
            return p;
        return System.Windows.Data.Binding.DoNothing;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}

/// <summary>Shows element only when int == parameter.</summary>
public sealed class IntToVisibilityConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int v && parameter is string s && int.TryParse(s, out var p))
            return v == p ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
