using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Insait_Translator_Deutsch.ViewModels;

namespace Insait_Translator_Deutsch.Converters;

public sealed class MainTabToBoolConverter : IValueConverter
{
    public static readonly MainTabToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MainTab tab)
            return false;

        var paramString = parameter?.ToString();
        if (string.IsNullOrWhiteSpace(paramString))
            return false;

        return Enum.TryParse<MainTab>(paramString, ignoreCase: true, out var expected) && tab == expected;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

