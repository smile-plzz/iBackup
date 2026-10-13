namespace iBackup.Core.Interfaces;

using iBackup.Core.Models;

/// <summary>
/// Scans iOS device storage for media files such as photos, videos, and live photos.
/// </summary>
public interface IMediaScanner
{
    /// <summary>
    /// Asynchronously enumerates media files discovered on the specified device.
    /// </summary>
    /// <param name="device">The connected device to scan.</param>
    /// <param name="ct">A cancellation token to observe.</param>
    /// <returns>An asynchronous stream of discovered <see cref="ScannedFile"/> items.</returns>
    IAsyncEnumerable<ScannedFile> ScanMediaFilesAsync(DeviceInfo device, CancellationToken ct = default);

    /// <summary>
    /// Calculates the total count of media files located on the specified device.
    /// </summary>
    /// <param name="device">The connected device to scan.</param>
    /// <param name="ct">A cancellation token to observe.</param>
    /// <returns>The total number of media files found.</returns>
    Task<int> GetMediaCountAsync(DeviceInfo device, CancellationToken ct = default);
}
