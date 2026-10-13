namespace iBackup.Core.Models;

/// <summary>
/// Represents connected iOS device metadata and storage status.
/// </summary>
/// <param name="Udid">The unique identifier (UDID or persistent device ID) of the device.</param>
/// <param name="Name">The friendly user-assigned name of the device.</param>
/// <param name="Model">The hardware model or description of the device.</param>
/// <param name="IOSVersion">The iOS operating system version string, if available.</param>
/// <param name="TotalStorageBytes">The total storage capacity of the device in bytes.</param>
/// <param name="UsedStorageBytes">The amount of used storage space on the device in bytes.</param>
/// <param name="IsConnected">A value indicating whether the device is currently connected.</param>
public record DeviceInfo(
    string Udid,
    string Name,
    string Model,
    string? IOSVersion,
    long TotalStorageBytes,
    long UsedStorageBytes,
    bool IsConnected)
{
    /// <summary>
    /// Gets the estimated available free storage capacity on the device in bytes.
    /// </summary>
    public long FreeStorageBytes => Math.Max(0L, TotalStorageBytes - UsedStorageBytes);

    /// <summary>
    /// Gets a human-friendly summary of used storage relative to total capacity (e.g., '47.2 GB / 128 GB').
    /// </summary>
    public string FriendlyStorageSummary
    {
        get
        {
            if (TotalStorageBytes <= 0)
            {
                return "Unknown";
            }

            const double gigabyte = 1024.0 * 1024.0 * 1024.0;
            double usedGb = UsedStorageBytes / gigabyte;
            double totalGb = TotalStorageBytes / gigabyte;

            // If total GB is close to an integer (e.g. 64, 128, 256, 512), format total as whole or 1 decimal
            string totalFormat = totalGb >= 10 && Math.Abs(totalGb - Math.Round(totalGb)) < 0.15 ? $"{Math.Round(totalGb):F0}" : $"{totalGb:F1}";
            return $"{usedGb:F1} GB / {totalFormat} GB";
        }
    }
}
