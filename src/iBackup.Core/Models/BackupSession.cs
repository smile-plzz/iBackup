namespace iBackup.Core.Models;

/// <summary>
/// Represents a discrete backup run session and its aggregated outcome.
/// </summary>
public class BackupSession
{
    /// <summary>
    /// Gets or sets the primary key identifier of the backup session.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the device backed up in this session.
    /// </summary>
    public string DeviceUdid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the friendly name of the device at the time of backup.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the session began.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the session completed or terminated, if finished.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the total number of candidate files evaluated during the session.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of newly transferred files during the session.
    /// </summary>
    public int NewFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of files skipped because they were previously backed up.
    /// </summary>
    public int SkippedFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of files that failed during transfer.
    /// </summary>
    public int FailedFiles { get; set; }

    /// <summary>
    /// Gets or sets the total volume of data in bytes transferred during the session.
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Gets or sets the final or current status of the backup session.
    /// </summary>
    public BackupStatus Status { get; set; } = BackupStatus.InProgress;
}
