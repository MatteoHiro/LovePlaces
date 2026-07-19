using System.Globalization;

namespace LovePlaceApp.Converters;

public class InverseBoolConverter : IValueConverter
{
    public static object ConvertValue(object? value)
    {
        return value is bool flag && !flag;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) // NOSONAR: interface implementation cannot be static
    {
        return ConvertValue(value);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) // NOSONAR: interface implementation cannot be static
    {
        return ConvertValue(value);
    }
}
