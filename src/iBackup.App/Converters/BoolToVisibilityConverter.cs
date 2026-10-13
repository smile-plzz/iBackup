using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace iBackup.App.Converters;

/// <summary>
/// Converts boolean values to XAML <see cref="Visibility"/> values, supporting inversion via ConverterParameter.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to a <see cref="Visibility"/> value.
    /// </summary>
    /// <param name="value">The source boolean value.</param>
    /// <param name="targetType">The target binding type.</param>
    /// <param name="parameter">Optional inversion parameter ("Inverse", "Invert", or "True").</param>
    /// <param name="language">The culture language.</param>
    /// <returns><see cref="Visibility.Visible"/> or <see cref="Visibility.Collapsed"/>.</returns>
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        var boolValue = value is bool b && b;
        var invert = parameter is string paramStr &&
                     (paramStr.Equals("Inverse", StringComparison.OrdinalIgnoreCase) ||
                      paramStr.Equals("Invert", StringComparison.OrdinalIgnoreCase) ||
                      paramStr.Equals("True", StringComparison.OrdinalIgnoreCase));

        if (invert)
        {
            boolValue = !boolValue;
        }

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Converts a <see cref="Visibility"/> value back to a boolean value.
    /// </summary>
    /// <param name="value">The <see cref="Visibility"/> value.</param>
    /// <param name="targetType">The target binding type.</param>
    /// <param name="parameter">Optional inversion parameter.</param>
    /// <param name="language">The culture language.</param>
    /// <returns>True or false.</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        var isVisible = value is Visibility v && v == Visibility.Visible;
        var invert = parameter is string paramStr &&
                     (paramStr.Equals("Inverse", StringComparison.OrdinalIgnoreCase) ||
                      paramStr.Equals("Invert", StringComparison.OrdinalIgnoreCase) ||
                      paramStr.Equals("True", StringComparison.OrdinalIgnoreCase));

        return invert ? !isVisible : isVisible;
    }
}
