using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BloxCord.Client.Converters;

public sealed class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool hasValue = value is string text && !string.IsNullOrWhiteSpace(text);
        if (Invert)
            hasValue = !hasValue;

        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
