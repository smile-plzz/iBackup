namespace iBackup.Core.Interfaces;

using iBackup.Core.Models;

/// <summary>
/// Transfers media files from an attached iOS device to the local file system.
/// </summary>
public interface IFileTransferService
{
    /// <summary>
    /// Transfers an individual file from the connected device to a local destination file path.
    /// </summary>
    /// <param name="deviceUdid">The unique device identifier or friendly identifier.</param>
    /// <param name="sourcePath">The source file path on the device.</param>
    /// <param name="destinationPath">The destination local file path.</param>
    /// <param name="progress">An optional progress reporter for incremental bytes transferred.</param>
    /// <param name="ct">A cancellation token to observe.</param>
    /// <returns>A <see cref="TransferResult"/> indicating success, transfer duration, and byte count.</returns>
    Task<TransferResult> TransferFileAsync(
        string deviceUdid,
        string sourcePath,
        string destinationPath,
        IProgress<long>? progress = null,
        CancellationToken ct = default);
}
