namespace iBackup.Core.Models;

/// <summary>
/// Represents the completion or execution status of a backup operation, session, or individual file.
/// </summary>
public enum BackupStatus
{
    /// <summary>
    /// The backup operation completed successfully.
    /// </summary>
    Complete,

    /// <summary>
    /// The backup operation failed due to an error.
    /// </summary>
    Failed,

    /// <summary>
    /// The backup operation completed partially with some errors or skipped items.
    /// </summary>
    Partial,

    /// <summary>
    /// The item was skipped because it already exists or does not match backup criteria.
    /// </summary>
    Skipped,

    /// <summary>
    /// The backup operation is currently executing.
    /// </summary>
    InProgress
}
