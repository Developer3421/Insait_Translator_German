using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Insait_Translator_Deutsch.Converters;

public sealed class WorkspaceIndexEqualsConverter : IValueConverter
{
    public static readonly WorkspaceIndexEqualsConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int selected) return false;
        if (parameter == null) return false;

        if (parameter is int idx) return selected == idx;

        return int.TryParse(parameter.ToString(), out var parsed) && selected == parsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

