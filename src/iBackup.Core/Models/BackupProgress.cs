namespace iBackup.Core.Models;

/// <summary>
/// Provides realtime snapshot telemetry during an ongoing backup operation.
/// </summary>
/// <param name="CurrentFileName">The name of the file currently being copied or evaluated.</param>
/// <param name="FilesCompleted">The count of files processed so far.</param>
/// <param name="TotalFiles">The total number of candidate files to process.</param>
/// <param name="BytesCompleted">The cumulative volume of bytes transferred so far.</param>
/// <param name="TotalBytes">The total expected volume of bytes across all new files.</param>
/// <param name="CurrentSpeedBytesPerSecond">The instantaneous transfer speed in bytes per second.</param>
/// <param name="EstimatedSecondsRemaining">The estimated time to completion in seconds, or null if unknown.</param>
/// <param name="Status">The current state of the backup engine.</param>
public record BackupProgress(
    string CurrentFileName,
    int FilesCompleted,
    int TotalFiles,
    long BytesCompleted,
    long TotalBytes,
    double CurrentSpeedBytesPerSecond,
    double? EstimatedSecondsRemaining,
    BackupStatus Status)
{
    /// <summary>
    /// Gets the completion percentage (0.0 to 100.0) based on bytes or processed file count.
    /// </summary>
    public double PercentComplete
    {
        get
        {
            if (TotalBytes > 0)
            {
                return Math.Clamp((double)BytesCompleted / TotalBytes * 100.0, 0.0, 100.0);
            }

            if (TotalFiles > 0)
            {
                return Math.Clamp((double)FilesCompleted / TotalFiles * 100.0, 0.0, 100.0);
            }

            return 0.0;
        }
    }
}
