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
        => value is decimal d ? d.ToString("N2", CultureInfo.InvariantCulture) : "0.00";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            // Try invariant first, then current culture
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
                return result;
        }
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

/// <summary>Converts a color hex string to SolidColorBrush.</summary>
public sealed class StringToBrushConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrEmpty(s))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(s);
                return new SolidColorBrush(color);
            }
            catch { }
        }
        return new SolidColorBrush(Color.FromRgb(0xcb, 0xa6, 0xf7));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}

/// <summary>Calculates bar width as proportion of total (max 600px).</summary>
public sealed class CategoryBarWidthConverter : MarkupExtension, IMultiValueConverter
{
    private const double MaxWidth = 600;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is decimal amount && values[1] is decimal total && total > 0)
            return Math.Max(4, (double)(amount / total) * MaxWidth);
        return 4.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}

/// <summary>Converts decimal amount to bar height (max 80px, log-scaled).</summary>
public sealed class TrendBarHeightConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d && d > 0)
            return Math.Min(80, Math.Max(4, Math.Log10((double)d + 1) * 20));
        return 4.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}

/// <summary>Shows element when decimal > 0.</summary>
public sealed class DecimalToVisibilityConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is decimal d && d != 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

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
