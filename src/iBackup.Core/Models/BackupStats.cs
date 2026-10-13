namespace iBackup.Core.Models;

/// <summary>
/// Aggregates historical backup metrics and statistics for a device.
/// </summary>
/// <param name="TotalBackedUpFiles">The cumulative count of unique files successfully backed up.</param>
/// <param name="TotalBackedUpBytes">The cumulative size in bytes of files successfully backed up.</param>
/// <param name="LastBackupDate">The timestamp of the most recent completed backup session, if any.</param>
/// <param name="TotalSessions">The total number of backup sessions recorded for the device.</param>
public record BackupStats(
    int TotalBackedUpFiles,
    long TotalBackedUpBytes,
    DateTime? LastBackupDate,
    int TotalSessions);
