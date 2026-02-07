using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Insait_Translator_Deutsch.Converters;

/// <summary>
/// ConvertBack: when a workspace ToggleButton becomes checked, returns its index.
/// </summary>
public sealed class WorkspaceCheckedToIndexConverter : IValueConverter
{
    public static readonly WorkspaceCheckedToIndexConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int selected) return false;
        if (parameter == null) return false;
        if (!int.TryParse(parameter.ToString(), out var idx)) return false;
        return selected == idx;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isChecked || !isChecked) return Avalonia.Data.BindingOperations.DoNothing;
        if (parameter == null) return Avalonia.Data.BindingOperations.DoNothing;
        return int.TryParse(parameter.ToString(), out var idx) ? idx : Avalonia.Data.BindingOperations.DoNothing;
    }
}

