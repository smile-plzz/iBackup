using System;
using Microsoft.UI.Xaml.Data;

namespace iBackup.App.Converters;

/// <summary>
/// Converts byte quantities into human-readable formatted file size strings (e.g. "2.4 GB", "512 KB").
/// </summary>
public sealed class FileSizeConverter : IValueConverter
{
    private static readonly string[] SizeSuffixes = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];

    /// <summary>
    /// Converts a numeric byte value into a formatted string.
    /// </summary>
    /// <param name="value">The byte value to convert.</param>
    /// <param name="targetType">The target binding type.</param>
    /// <param name="parameter">Optional formatting parameter.</param>
    /// <param name="language">The culture language.</param>
    /// <returns>A formatted file size string.</returns>
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is null)
        {
            return "0 B";
        }

        try
        {
            var bytes = System.Convert.ToInt64(value);
            return FormatBytes(bytes);
        }
        catch
        {
            return "0 B";
        }
    }

    /// <summary>
    /// Formats a byte count into a human-readable size string.
    /// </summary>
    /// <param name="bytes">The byte count.</param>
    /// <returns>A formatted string such as "2.4 GB".</returns>
    public static string FormatBytes(long bytes)
    {
        if (bytes < 0)
        {
            return "-" + FormatBytes(-bytes);
        }

        if (bytes == 0)
        {
            return "0 B";
        }

        var mag = (int)Math.Log(bytes, 1024);
        if (mag >= SizeSuffixes.Length)
        {
            mag = SizeSuffixes.Length - 1;
        }

        var adjustedSize = (decimal)bytes / (1L << (mag * 10));

        if (mag == 0)
        {
            return $"{bytes} B";
        }

        return $"{adjustedSize:0.#} {SizeSuffixes[mag]}";
    }

    /// <summary>
    /// ConvertBack is not supported for formatted file sizes.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotSupportedException("Converting from formatted string to bytes is not supported.");
    }
}
