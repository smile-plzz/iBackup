namespace iBackup.Core.Interfaces;

using iBackup.Core.Models;

/// <summary>
/// Manages SQLite persistence for backed up file records, backup sessions, and device configurations.
/// </summary>
public interface IManifestRepository
{
    /// <summary>
    /// Initializes database schema, creating tables and indexes if they do not already exist.
    /// </summary>
    /// <returns>A task that represents the initialization operation.</returns>
    Task InitializeAsync();

    /// <summary>
    /// Checks whether a media file has already been successfully backed up for a device.
    /// </summary>
    /// <param name="deviceUdid">The unique identifier of the device.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="fileSize">The file size in bytes.</param>
    /// <param name="creationDate">The creation timestamp of the file.</param>
    /// <returns><c>true</c> if the file was previously backed up; otherwise, <c>false</c>.</returns>
    Task<bool> IsFileBackedUpAsync(string deviceUdid, string fileName, long fileSize, DateTime creationDate);

    /// <summary>
    /// Inserts or replaces a record for a backed-up file in the manifest.
    /// </summary>
    /// <param name="record">The file backup record to persist.</param>
    /// <returns>A task representing the database operation.</returns>
    Task RecordBackupAsync(BackupFileRecord record);

    /// <summary>
    /// Updates the backup status of an existing file record by ID.
    /// </summary>
    /// <param name="recordId">The database record identifier.</param>
    /// <param name="status">The updated status value.</param>
    /// <returns>A task representing the database operation.</returns>
    Task UpdateRecordStatusAsync(int recordId, BackupStatus status);

    /// <summary>
    /// Retrieves historical backup sessions, optionally filtered by device.
    /// </summary>
    /// <param name="deviceUdid">Optional device UDID filter.</param>
    /// <param name="limit">The maximum number of sessions to return.</param>
    /// <returns>A list of past <see cref="BackupSession"/> instances.</returns>
    Task<List<BackupSession>> GetBackupHistoryAsync(string? deviceUdid = null, int limit = 50);

    /// <summary>
    /// Computes aggregated backup statistics for the specified device.
    /// </summary>
    /// <param name="deviceUdid">The unique identifier of the device.</param>
    /// <returns>The aggregated <see cref="BackupStats"/> metrics.</returns>
    Task<BackupStats> GetStatsAsync(string deviceUdid);

    /// <summary>
    /// Creates and inserts a new backup session record with InProgress status.
    /// </summary>
    /// <param name="deviceUdid">The unique identifier of the device.</param>
    /// <param name="deviceName">The friendly name of the device.</param>
    /// <returns>The newly created <see cref="BackupSession"/> with its assigned ID.</returns>
    Task<BackupSession> CreateSessionAsync(string deviceUdid, string deviceName);

    /// <summary>
    /// Marks an existing backup session as finished and updates total counts.
    /// </summary>
    /// <param name="sessionId">The database session identifier.</param>
    /// <param name="status">The final session completion status.</param>
    /// <param name="totalFiles">The total number of files scanned or candidate files.</param>
    /// <param name="newFiles">The number of newly transferred files.</param>
    /// <param name="skippedFiles">The number of skipped files.</param>
    /// <param name="failedFiles">The number of failed file transfers.</param>
    /// <param name="totalBytes">The total bytes transferred.</param>
    /// <returns>A task representing the database operation.</returns>
    Task CompleteSessionAsync(
        int sessionId,
        BackupStatus status,
        int totalFiles,
        int newFiles,
        int skippedFiles,
        int failedFiles,
        long totalBytes);

    /// <summary>
    /// Retrieves the saved backup settings for a given device UDID.
    /// </summary>
    /// <param name="deviceUdid">The unique identifier of the device.</param>
    /// <returns>The stored settings, or null if no configuration exists yet.</returns>
    Task<BackupSettings?> GetDeviceSettingsAsync(string deviceUdid);

    /// <summary>
    /// Saves or updates the backup configuration for a device.
    /// </summary>
    /// <param name="settings">The backup settings to persist.</param>
    /// <returns>A task representing the database operation.</returns>
    Task SaveDeviceSettingsAsync(BackupSettings settings);

    /// <summary>
    /// Removes incomplete, failed, or partial file records for a device from previous runs.
    /// </summary>
    /// <param name="deviceUdid">The unique identifier of the device.</param>
    /// <returns>A task representing the database operation.</returns>
    Task CleanupPartialRecordsAsync(string deviceUdid);
}
