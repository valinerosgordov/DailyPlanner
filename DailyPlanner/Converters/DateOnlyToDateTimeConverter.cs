using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace DailyPlanner.Converters;

/// <summary>Converts DateOnly ↔ DateTime for WPF DatePicker binding.</summary>
public sealed class DateOnlyToDateTimeConverter : MarkupExtension, IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            DateOnly d => d.ToDateTime(TimeOnly.MinValue),
            _ => null
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
            return DateOnly.FromDateTime(dt);
        return DateOnly.FromDateTime(DateTime.Today);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}

/// <summary>Converts DateOnly? ↔ DateTime? for nullable date fields.</summary>
public sealed class NullableDateOnlyToDateTimeConverter : MarkupExtension, IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is DateOnly d ? d.ToDateTime(TimeOnly.MinValue) : null;
    }

    public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is DateTime dt ? DateOnly.FromDateTime(dt) : null;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
