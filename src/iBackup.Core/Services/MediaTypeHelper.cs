namespace iBackup.Core.Services;

using iBackup.Core.Models;

/// <summary>
/// Provides utility methods for classifying media types, validating extensions, and formatting data.
/// </summary>
public static class MediaTypeHelper
{
    private static readonly HashSet<string> PhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".heic", ".heif", ".png", ".tiff", ".tif", ".bmp", ".gif", ".webp"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mov", ".mp4", ".m4v"
    };

    private static readonly HashSet<string> RawExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dng"
    };

    private static readonly HashSet<string> AllSupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".heic", ".heif", ".png", ".tiff", ".tif", ".bmp", ".gif", ".webp",
        ".mov", ".mp4", ".m4v", ".dng"
    };

    /// <summary>
    /// Gets the set of all supported media file extensions (lowercase including leading dot).
    /// </summary>
    /// <returns>A hash set of supported file extensions.</returns>
    public static HashSet<string> GetSupportedExtensions() => new(AllSupportedExtensions, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks whether the specified file name corresponds to a supported media format.
    /// </summary>
    /// <param name="fileName">The file name or path to test.</param>
    /// <returns><c>true</c> if supported; otherwise, <c>false</c>.</returns>
    public static bool IsMediaFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        string ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && AllSupportedExtensions.Contains(ext);
    }

    /// <summary>
    /// Determines the <see cref="MediaType"/> based on file name and extension.
    /// </summary>
    /// <param name="fileName">The file name or relative path.</param>
    /// <returns>The categorized media type.</returns>
    public static MediaType DetermineMediaType(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return MediaType.Photo;
        }

        string ext = Path.GetExtension(fileName);
        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

        // Check Raw
        if (RawExtensions.Contains(ext))
        {
            return MediaType.Raw;
        }

        // Check Screenshot (PNG files or names containing screenshot)
        if (nameWithoutExt.Contains("screenshot", StringComparison.OrdinalIgnoreCase) ||
            (ext.Equals(".png", StringComparison.OrdinalIgnoreCase) && !nameWithoutExt.StartsWith("IMG_", StringComparison.OrdinalIgnoreCase)))
        {
            return MediaType.Screenshot;
        }

        // Check SlowMo / TimeLapse naming cues if present in file name
        if (nameWithoutExt.Contains("slowmo", StringComparison.OrdinalIgnoreCase) ||
            nameWithoutExt.Contains("slomo", StringComparison.OrdinalIgnoreCase))
        {
            return MediaType.SlowMo;
        }

        if (nameWithoutExt.Contains("timelapse", StringComparison.OrdinalIgnoreCase) ||
            nameWithoutExt.Contains("time_lapse", StringComparison.OrdinalIgnoreCase))
        {
            return MediaType.TimeLapse;
        }

        // Video
        if (VideoExtensions.Contains(ext))
        {
            return MediaType.Video;
        }

        // Default photo
        return MediaType.Photo;
    }

    /// <summary>
    /// Formats a byte count into a readable string with appropriate units (e.g. '2.4 GB', '350 MB', '12.5 KB').
    /// </summary>
    /// <param name="bytes">The number of bytes.</param>
    /// <returns>A formatted representation with units.</returns>
    public static string FormatFileSize(long bytes)
    {
        if (bytes < 0)
        {
            return "0 B";
        }

        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024m) >= 1m && counter < suffixes.Length - 1)
        {
            number /= 1024m;
            counter++;
        }

        if (counter == 0)
        {
            return $"{number:n0} {suffixes[counter]}";
        }

        return $"{number.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} {suffixes[counter]}";
    }

    /// <summary>
    /// Formats a duration into a human-friendly string (e.g., '2m 30s', '1h 15m').
    /// </summary>
    /// <param name="duration">The time duration to format.</param>
    /// <returns>A concise representation of time.</returns>
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
        {
            return "< 1s";
        }

        if (duration.TotalMinutes < 1)
        {
            return $"{duration.Seconds}s";
        }

        if (duration.TotalHours < 1)
        {
            return duration.Seconds > 0
                ? $"{duration.Minutes}m {duration.Seconds}s"
                : $"{duration.Minutes}m";
        }

        return duration.Minutes > 0
            ? $"{(int)duration.TotalHours}h {duration.Minutes}m"
            : $"{(int)duration.TotalHours}h";
    }
}
