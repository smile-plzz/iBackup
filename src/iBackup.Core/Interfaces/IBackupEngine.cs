namespace iBackup.Core.Interfaces;

using iBackup.Core.Models;

/// <summary>
/// Orchestrates and manages end-to-end media backup operations from an iOS device.
/// </summary>
public interface IBackupEngine
{
    /// <summary>
    /// Occurs when backup progress or throughput metrics are updated.
    /// </summary>
    event EventHandler<BackupProgress>? ProgressChanged;

    /// <summary>
    /// Occurs when a backup session finishes execution.
    /// </summary>
    event EventHandler<BackupSession>? BackupCompleted;

    /// <summary>
    /// Occurs when an unhandled error terminates the backup process.
    /// </summary>
    event EventHandler<Exception>? BackupFailed;

    /// <summary>
    /// Begins the backup execution for the specified device according to provided settings.
    /// </summary>
    /// <param name="device">The device to back up.</param>
    /// <param name="settings">The backup configuration settings.</param>
    /// <param name="ct">A cancellation token to observe.</param>
    /// <returns>A task that completes with the final <see cref="BackupSession"/> summary.</returns>
    Task<BackupSession> StartBackupAsync(DeviceInfo device, BackupSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Pauses an ongoing backup operation without cancelling it.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes an ongoing backup operation that was previously paused.
    /// </summary>
    void Resume();

    /// <summary>
    /// Cancels an active backup operation immediately.
    /// </summary>
    void Cancel();

    /// <summary>
    /// Gets a value indicating whether a backup operation is currently executing.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets a value indicating whether the active backup is currently paused.
    /// </summary>
    bool IsPaused { get; }
}
