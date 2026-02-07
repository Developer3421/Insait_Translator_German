using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Insait_Translator_Deutsch.Converters;

public sealed class IntGreaterThanConverter : IValueConverter
{
    public static readonly IntGreaterThanConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int current) return false;
        if (parameter == null) return false;
        if (!int.TryParse(parameter.ToString(), out var threshold)) return false;
        return current > threshold;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

