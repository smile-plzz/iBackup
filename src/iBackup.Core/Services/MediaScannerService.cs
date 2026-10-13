namespace iBackup.Core.Services;

using System.Runtime.CompilerServices;
using iBackup.Core.Interfaces;
using iBackup.Core.Models;
using MediaDevices;
using Microsoft.Extensions.Logging;

/// <summary>
/// Scans iOS devices for media files within DCIM directory hierarchies and classifies them.
/// </summary>
public sealed class MediaScannerService : IMediaScanner
{
    private readonly ILogger<MediaScannerService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaScannerService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public MediaScannerService(ILogger<MediaScannerService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ScannedFile> ScanMediaFilesAsync(
        DeviceInfo device,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        var mediaDevice = FindDevice(device);
        if (mediaDevice == null)
        {
            _logger.LogError("Media device {DeviceName} ({DeviceUdid}) not found among connected devices.", device.Name, device.Udid);
            yield break;
        }

        try
        {
            mediaDevice.Connect();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to device {DeviceName}.", device.Name);
            yield break;
        }

        try
        {
            var dcimRoots = FindDcimDirectories(mediaDevice);
            if (dcimRoots.Count == 0)
            {
                _logger.LogWarning("No DCIM directory found on device {DeviceName}.", device.Name);
                yield break;
            }

            var dirQueue = new Queue<string>(dcimRoots);
            var visitedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (dirQueue.Count > 0)
            {
                ct.ThrowIfCancellationRequested();

                string currentDir = dirQueue.Dequeue();
                if (!visitedDirs.Add(currentDir))
                {
                    continue;
                }

                // Discover subdirectories
                string[] subDirs = Array.Empty<string>();
                try
                {
                    subDirs = mediaDevice.GetDirectories(currentDir);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not list subdirectories for {CurrentDir}.", currentDir);
                }

                foreach (var subDir in subDirs)
                {
                    dirQueue.Enqueue(subDir);
                }

                // Enumerate files in current directory
                List<string> filePaths = new();
                try
                {
                    var enumerated = mediaDevice.EnumerateFiles(currentDir);
                    if (enumerated != null)
                    {
                        filePaths.AddRange(enumerated);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to enumerate files in directory {CurrentDir}.", currentDir);
                    continue;
                }

                // Index files by name without extension to identify Live Photo pairs
                var baseNameGroups = filePaths
                    .Where(p => MediaTypeHelper.IsMediaFile(p))
                    .GroupBy(p => Path.GetFileNameWithoutExtension(p), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                foreach (var filePath in filePaths)
                {
                    ct.ThrowIfCancellationRequested();

                    string fileName = Path.GetFileName(filePath);
                    if (!MediaTypeHelper.IsMediaFile(fileName))
                    {
                        continue;
                    }

                    MediaFileInfo? fileInfo = null;
                    try
                    {
                        fileInfo = mediaDevice.GetFileInfo(filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not retrieve file info for {FilePath}.", filePath);
                    }

                    long fileSize = fileInfo != null ? (long)fileInfo.Length : 0L;
                    DateTime creationDate = fileInfo?.CreationTime ?? DateTime.Now;
                    DateTime? modificationDate = fileInfo?.LastWriteTime;

                    string baseName = Path.GetFileNameWithoutExtension(fileName);
                    string ext = Path.GetExtension(fileName);

                    // Determine media type with Live Photo pairing awareness
                    MediaType mediaType = DetermineLiveOrMediaType(baseName, ext, baseNameGroups);

                    var scanned = new ScannedFile(
                        SourcePath: filePath,
                        FileName: fileName,
                        FileSize: fileSize,
                        CreationDate: creationDate,
                        ModificationDate: modificationDate,
                        MediaType: mediaType);

                    // Yield to consumer asynchronously
                    await Task.Yield();
                    yield return scanned;
                }
            }
        }
        finally
        {
            try
            {
                mediaDevice.Disconnect();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error while disconnecting device {DeviceName}.", device.Name);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetMediaCountAsync(DeviceInfo device, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        int count = 0;
        await foreach (var _ in ScanMediaFilesAsync(device, ct))
        {
            count++;
        }

        return count;
    }

    private static MediaType DetermineLiveOrMediaType(
        string baseName,
        string ext,
        Dictionary<string, List<string>> baseNameGroups)
    {
        // Live Photo detection: check if paired .heic/.jpg and .mov share the same base name
        if (baseNameGroups.TryGetValue(baseName, out var siblings) && siblings.Count > 1)
        {
            bool hasPhoto = siblings.Any(s =>
                s.EndsWith(".heic", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));

            bool hasMov = siblings.Any(s => s.EndsWith(".mov", StringComparison.OrdinalIgnoreCase));

            if (hasPhoto && hasMov)
            {
                return MediaType.LivePhoto;
            }
        }

        return MediaTypeHelper.DetermineMediaType(baseName + ext);
    }

    private static MediaDevice? FindDevice(DeviceInfo device)
    {
        var devices = MediaDeviceManager.Instance.GetDevices() ?? [];
        return devices.FirstOrDefault(d =>
            string.Equals(d.DeviceId, device.Udid, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(d.SerialNumber, device.Udid, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(d.FriendlyName, device.Name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(d.FriendlyName, device.Udid, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> FindDcimDirectories(MediaDevice mediaDevice)
    {
        var results = new List<string>();

        // Common locations for iOS DCIM
        string[] candidateRoots = { @"\Internal Storage\DCIM", @"\DCIM", @"Internal Storage\DCIM", "DCIM" };
        foreach (var candidate in candidateRoots)
        {
            try
            {
                if (mediaDevice.DirectoryExists(candidate))
                {
                    results.Add(candidate);
                    return results;
                }
            }
            catch
            {
                // Ignore probe errors
            }
        }

        // Fallback: check top-level directories
        try
        {
            var topLevel = mediaDevice.GetDirectories(@"\");
            foreach (var dir in topLevel)
            {
                string dirName = Path.GetFileName(dir);
                if (string.Equals(dirName, "DCIM", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(dir);
                }
                else
                {
                    // Check subfolders of root e.g. \Internal Storage
                    try
                    {
                        var secondLevel = mediaDevice.GetDirectories(dir);
                        foreach (var sub in secondLevel)
                        {
                            if (string.Equals(Path.GetFileName(sub), "DCIM", StringComparison.OrdinalIgnoreCase))
                            {
                                results.Add(sub);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }
        }
        catch
        {
            // Ignore
        }

        return results;
    }
}
