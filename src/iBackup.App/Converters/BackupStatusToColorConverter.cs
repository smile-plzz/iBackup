using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace iBackup.App.Converters;

/// <summary>
/// Converts backup status values or strings to corresponding WinUI <see cref="SolidColorBrush"/> representations.
/// Complete -> Green, Failed -> Red, InProgress -> Blue, Partial / Skipped -> Orange.
/// </summary>
public sealed class BackupStatusToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush GreenBrush = new(Color.FromArgb(255, 16, 124, 65));     // #107C41
    private static readonly SolidColorBrush RedBrush = new(Color.FromArgb(255, 209, 52, 56));       // #D13438
    private static readonly SolidColorBrush BlueBrush = new(Color.FromArgb(255, 0, 120, 212));       // #0078D4
    private static readonly SolidColorBrush OrangeBrush = new(Color.FromArgb(255, 202, 80, 16));     // #CA5010
    private static readonly SolidColorBrush GrayBrush = new(Color.FromArgb(255, 138, 136, 134));     // #8A8886

    /// <summary>
    /// Converts a backup status enum or string to a status color brush.
    /// </summary>
    /// <param name="value">The backup status enum or string representation.</param>
    /// <param name="targetType">The target binding type.</param>
    /// <param name="parameter">Optional converter parameter.</param>
    /// <param name="language">The culture language.</param>
    /// <returns>A <see cref="SolidColorBrush"/> corresponding to the status.</returns>
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is null)
        {
            return GrayBrush;
        }

        var status = value.ToString()?.Trim().ToLowerInvariant();

        return status switch
        {
            "complete" or "completed" or "success" => GreenBrush,
            "failed" or "error" => RedBrush,
            "inprogress" or "in_progress" or "running" or "scanning" => BlueBrush,
            "partial" or "skipped" or "paused" => OrangeBrush,
            _ => GrayBrush
        };
    }

    /// <summary>
    /// ConvertBack is not supported.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotSupportedException("Converting from color brush back to BackupStatus is not supported.");
    }
}
