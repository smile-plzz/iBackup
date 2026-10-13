namespace iBackup.Core.Services;

using System.Diagnostics;
using iBackup.Core.Interfaces;
using iBackup.Core.Models;
using MediaDevices;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles binary file transfers from an attached iOS media device to local storage with progress reporting.
/// </summary>
public sealed class FileTransferService : IFileTransferService
{
    private const int BufferSize = 64 * 1024; // 64 KB buffer
    private readonly ILogger<FileTransferService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileTransferService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public FileTransferService(ILogger<FileTransferService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<TransferResult> TransferFileAsync(
        string deviceUdid,
        string sourcePath,
        string destinationPath,
        IProgress<long>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceUdid);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var stopwatch = Stopwatch.StartNew();
        long totalBytesWritten = 0;
        bool disconnectedByUs = false;
        MediaDevice? mediaDevice = null;

        try
        {
            mediaDevice = FindDevice(deviceUdid);
            if (mediaDevice == null)
            {
                stopwatch.Stop();
                return new TransferResult(
                    Success: false,
                    BytesTransferred: 0,
                    ErrorMessage: $"Device with UDID '{deviceUdid}' was not found or is disconnected.",
                    Duration: stopwatch.Elapsed);
            }

            if (!mediaDevice.IsConnected)
            {
                mediaDevice.Connect();
                disconnectedByUs = true;
            }

            // Ensure target directory exists
            string? destinationDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            // MediaDevices 2.x performs the WPD transfer asynchronously into the
            // destination stream. The stream remains owned by this service.
            using (var destinationStream = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: BufferSize,
                useAsync: true))
            {
                await mediaDevice.DownloadFileAsync(sourcePath, destinationStream, ct);
                await destinationStream.FlushAsync(ct);
                totalBytesWritten = destinationStream.Length;
                progress?.Report(totalBytesWritten);
            }

            stopwatch.Stop();
            _logger.LogDebug("Successfully transferred {SourcePath} to {DestinationPath} ({Bytes} bytes in {Duration})",
                sourcePath, destinationPath, totalBytesWritten, stopwatch.Elapsed);

            return new TransferResult(
                Success: true,
                BytesTransferred: totalBytesWritten,
                ErrorMessage: null,
                Duration: stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("Transfer of {SourcePath} was canceled.", sourcePath);
            CleanupPartialFile(destinationPath);

            return new TransferResult(
                Success: false,
                BytesTransferred: totalBytesWritten,
                ErrorMessage: "The transfer operation was canceled.",
                Duration: stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to transfer file {SourcePath} to {DestinationPath}.", sourcePath, destinationPath);
            CleanupPartialFile(destinationPath);

            return new TransferResult(
                Success: false,
                BytesTransferred: totalBytesWritten,
                ErrorMessage: ex.Message,
                Duration: stopwatch.Elapsed);
        }
        finally
        {
            if (disconnectedByUs && mediaDevice != null && mediaDevice.IsConnected)
            {
                try
                {
                    mediaDevice.Disconnect();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error disconnecting device after file transfer.");
                }
            }
        }
    }

    private static MediaDevice? FindDevice(string deviceUdid)
    {
        var devices = MediaDeviceManager.Instance.GetDevices() ?? [];
        return devices.FirstOrDefault(d =>
            string.Equals(d.DeviceId, deviceUdid, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(d.SerialNumber, deviceUdid, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(d.FriendlyName, deviceUdid, StringComparison.OrdinalIgnoreCase));
    }

    private static void CleanupPartialFile(string destinationPath)
    {
        try
        {
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
        }
        catch
        {
            // Ignore cleanup failure
        }
    }
}
