using System;
using System.Collections;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace Insait_Translator_Deutsch.Converters;

/// <summary>
/// Returns the index of the current item's DataContext within the nearest ItemsControl's ItemsSource.
/// </summary>
public sealed class ItemIndexConverter : IValueConverter
{
    public static readonly ItemIndexConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Control control)
            return 0;

        ItemsControl? itemsControl = null;
        Control? current = control;
        while (current != null)
        {
            current = current.Parent as Control;
            if (current is ItemsControl ic)
            {
                itemsControl = ic;
                break;
            }
        }

        var items = itemsControl?.ItemsSource as IEnumerable;
        if (items == null)
            return 0;

        var index = 0;
        foreach (var item in items)
        {
            if (ReferenceEquals(item, control.DataContext))
                return index;
            index++;
        }

        return 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
