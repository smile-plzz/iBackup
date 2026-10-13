namespace iBackup.Core.Services;

using System.Diagnostics;
using System.Security.Cryptography;
using iBackup.Core.Interfaces;
using iBackup.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates device media scanning, deduplication, incremental file transfer,
/// database manifest tracking, progress reporting, pause/resume, and error handling.
/// </summary>
public sealed class BackupEngine : IBackupEngine
{
    private readonly IMediaScanner _mediaScanner;
    private readonly IFileTransferService _fileTransferService;
    private readonly IManifestRepository _manifestRepository;
    private readonly ILogger<BackupEngine> _logger;

    private readonly ManualResetEventSlim _pauseEvent = new(initialState: true);
    private readonly object _syncRoot = new();

    private CancellationTokenSource? _engineCts;
    private bool _isRunning;
    private bool _isPaused;

    /// <inheritdoc/>
    public event EventHandler<BackupProgress>? ProgressChanged;

    /// <inheritdoc/>
    public event EventHandler<BackupSession>? BackupCompleted;

    /// <inheritdoc/>
    public event EventHandler<Exception>? BackupFailed;

    /// <inheritdoc/>
    public bool IsRunning
    {
        get
        {
            lock (_syncRoot)
            {
                return _isRunning;
            }
        }
        private set
        {
            lock (_syncRoot)
            {
                _isRunning = value;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsPaused
    {
        get
        {
            lock (_syncRoot)
            {
                return _isPaused;
            }
        }
        private set
        {
            lock (_syncRoot)
            {
                _isPaused = value;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupEngine"/> class.
    /// </summary>
    /// <param name="mediaScanner">The media scanner service.</param>
    /// <param name="fileTransferService">The file transfer service.</param>
    /// <param name="manifestRepository">The manifest repository.</param>
    /// <param name="logger">The logger instance.</param>
    public BackupEngine(
        IMediaScanner mediaScanner,
        IFileTransferService fileTransferService,
        IManifestRepository manifestRepository,
        ILogger<BackupEngine> logger)
    {
        _mediaScanner = mediaScanner ?? throw new ArgumentNullException(nameof(mediaScanner));
        _fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
        _manifestRepository = manifestRepository ?? throw new ArgumentNullException(nameof(manifestRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<BackupSession> StartBackupAsync(
        DeviceInfo device,
        BackupSettings settings,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(settings);

        lock (_syncRoot)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("A backup operation is already running.");
            }

            _isRunning = true;
            _isPaused = false;
            _pauseEvent.Set();
            _engineCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        }

        var cancellationToken = _engineCts.Token;
        BackupSession? session = null;
        var overallStopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Initializing backup engine for device {DeviceName} ({DeviceUdid}).", device.Name, device.Udid);
            await _manifestRepository.InitializeAsync();
            await _manifestRepository.CleanupPartialRecordsAsync(device.Udid);

            // Create new backup session
            session = await _manifestRepository.CreateSessionAsync(device.Udid, device.Name);

            // Ensure destination root path exists
            if (!string.IsNullOrWhiteSpace(settings.DestinationPath))
            {
                Directory.CreateDirectory(settings.DestinationPath);
            }

            // Step 1: Scan and discover all candidate files on device
            _logger.LogInformation("Scanning media files on {DeviceName}...", device.Name);
            var candidateFiles = new List<ScannedFile>();

            await foreach (var scanned in _mediaScanner.ScanMediaFilesAsync(device, cancellationToken))
            {
                _pauseEvent.Wait(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (ShouldIncludeFile(scanned, settings))
                {
                    candidateFiles.Add(scanned);
                    ReportProgress(
                        currentFileName: scanned.FileName,
                        filesCompleted: candidateFiles.Count,
                        totalFiles: 0,
                        bytesCompleted: 0,
                        totalBytes: 0,
                        speedBytesPerSecond: 0,
                        estimatedSecondsRemaining: null,
                        status: BackupStatus.InProgress,
                        isScanning: true);
                }
            }

            int totalFiles = candidateFiles.Count;
            _logger.LogInformation("Scanned {Total} candidate media files matching backup criteria.", totalFiles);

            if (totalFiles == 0)
            {
                throw new InvalidOperationException(
                    "The iPhone photo library is accessible, but no supported photos or videos were found.");
            }

            // Step 2: Pre-check candidate file sizes and disk free space
            long totalEstimatedBytes = candidateFiles.Sum(f => f.FileSize);
            if (!HasSufficientDiskSpace(settings.DestinationPath, totalEstimatedBytes, out long availableBytes))
            {
                _logger.LogWarning("Destination drive may not have sufficient free space ({Available} available vs {Needed} estimated).",
                    MediaTypeHelper.FormatFileSize(availableBytes),
                    MediaTypeHelper.FormatFileSize(totalEstimatedBytes));
            }

            int filesCompleted = 0;
            int newFiles = 0;
            int skippedFiles = 0;
            int failedFiles = 0;
            long bytesCompleted = 0;

            var speedWindow = new Queue<(DateTime Time, long Bytes)>();

            // Step 3: Process files incrementally
            foreach (var file in candidateFiles)
            {
                _pauseEvent.Wait(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // Check if already backed up
                bool alreadyBackedUp = await _manifestRepository.IsFileBackedUpAsync(
                    device.Udid,
                    file.FileName,
                    file.FileSize,
                    file.CreationDate);

                if (alreadyBackedUp)
                {
                    skippedFiles++;
                    filesCompleted++;

                    ReportProgress(
                        currentFileName: file.FileName,
                        filesCompleted: filesCompleted,
                        totalFiles: totalFiles,
                        bytesCompleted: bytesCompleted,
                        totalBytes: totalEstimatedBytes,
                        speedBytesPerSecond: 0.0,
                        estimatedSecondsRemaining: null,
                        status: BackupStatus.InProgress);

                    continue;
                }

                // Compute organized destination file path
                string destinationPath = BuildDestinationPath(
                    settings.DestinationPath,
                    device.Name,
                    settings.OrganizationMode,
                    file.FileName,
                    file.CreationDate);
                destinationPath = ResolveAvailableDestinationPath(destinationPath);

                // Transfer file with chunk-level progress
                long fileBytesSoFar = 0;
                var progressReporter = new InlineProgress(chunkWritten =>
                {
                    long delta = chunkWritten - fileBytesSoFar;
                    fileBytesSoFar = chunkWritten;

                    lock (speedWindow)
                    {
                        var now = DateTime.UtcNow;
                        speedWindow.Enqueue((now, delta));
                        while (speedWindow.Count > 0 && (now - speedWindow.Peek().Time).TotalSeconds > 3.0)
                        {
                            speedWindow.Dequeue();
                        }
                    }

                    double currentSpeed = CalculateCurrentSpeed(speedWindow);
                    double? eta = CalculateEta(totalEstimatedBytes - (bytesCompleted + chunkWritten), currentSpeed);

                    ReportProgress(
                        currentFileName: file.FileName,
                        filesCompleted: filesCompleted,
                        totalFiles: totalFiles,
                        bytesCompleted: bytesCompleted + chunkWritten,
                        totalBytes: totalEstimatedBytes,
                        speedBytesPerSecond: currentSpeed,
                        estimatedSecondsRemaining: eta,
                        status: BackupStatus.InProgress);
                });

                var transferResult = await _fileTransferService.TransferFileAsync(
                    device.Udid,
                    file.SourcePath,
                    destinationPath,
                    progressReporter,
                    cancellationToken);

                if (transferResult.Success)
                {
                    string? sha256Hash = null;
                    if (settings.VerifyIntegrity)
                    {
                        sha256Hash = await ComputeFileHashAsync(destinationPath, cancellationToken);
                    }

                    var record = new BackupFileRecord
                    {
                        DeviceUdid = device.Udid,
                        SourcePath = file.SourcePath,
                        DestPath = destinationPath,
                        FileName = file.FileName,
                        FileSize = transferResult.BytesTransferred,
                        CreationDate = file.CreationDate,
                        ModificationDate = file.ModificationDate,
                        Sha256Hash = sha256Hash,
                        MediaType = file.MediaType,
                        BackupDate = DateTime.UtcNow,
                        Status = BackupStatus.Complete
                    };

                    await _manifestRepository.RecordBackupAsync(record);

                    newFiles++;
                    bytesCompleted += transferResult.BytesTransferred;
                }
                else
                {
                    _logger.LogWarning("Transfer failed for {File}: {Error}", file.FileName, transferResult.ErrorMessage);
                    failedFiles++;
                }

                filesCompleted++;

                double fileEndSpeed = CalculateCurrentSpeed(speedWindow);
                double? fileEndEta = CalculateEta(totalEstimatedBytes - bytesCompleted, fileEndSpeed);

                ReportProgress(
                    currentFileName: file.FileName,
                    filesCompleted: filesCompleted,
                    totalFiles: totalFiles,
                    bytesCompleted: bytesCompleted,
                    totalBytes: totalEstimatedBytes,
                    speedBytesPerSecond: fileEndSpeed,
                    estimatedSecondsRemaining: fileEndEta,
                    status: BackupStatus.InProgress);
            }

            // Determine final session status
            BackupStatus finalStatus;
            if (failedFiles > 0)
            {
                finalStatus = newFiles > 0 ? BackupStatus.Partial : BackupStatus.Failed;
            }
            else
            {
                finalStatus = BackupStatus.Complete;
            }

            await _manifestRepository.CompleteSessionAsync(
                session.Id,
                finalStatus,
                totalFiles,
                newFiles,
                skippedFiles,
                failedFiles,
                bytesCompleted);

            session.Status = finalStatus;
            session.CompletedAt = DateTime.UtcNow;
            session.TotalFiles = totalFiles;
            session.NewFiles = newFiles;
            session.SkippedFiles = skippedFiles;
            session.FailedFiles = failedFiles;
            session.TotalBytes = bytesCompleted;

            overallStopwatch.Stop();
            _logger.LogInformation("Backup session {SessionId} completed with status {Status} in {Duration}.",
                session.Id, finalStatus, overallStopwatch.Elapsed);

            BackupCompleted?.Invoke(this, session);
            return session;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Backup operation was cancelled by user.");
            if (session != null)
            {
                await _manifestRepository.CompleteSessionAsync(
                    session.Id,
                    BackupStatus.Failed,
                    session.TotalFiles,
                    session.NewFiles,
                    session.SkippedFiles,
                    session.FailedFiles + 1,
                    session.TotalBytes);

                session.Status = BackupStatus.Failed;
                session.CompletedAt = DateTime.UtcNow;
            }

            var ex = new OperationCanceledException("Backup was canceled.");
            BackupFailed?.Invoke(this, ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup operation encountered an unhandled failure.");
            if (session != null)
            {
                await _manifestRepository.CompleteSessionAsync(
                    session.Id,
                    BackupStatus.Failed,
                    session.TotalFiles,
                    session.NewFiles,
                    session.SkippedFiles,
                    session.FailedFiles + 1,
                    session.TotalBytes);

                session.Status = BackupStatus.Failed;
                session.CompletedAt = DateTime.UtcNow;
            }

            BackupFailed?.Invoke(this, ex);
            throw;
        }
        finally
        {
            IsRunning = false;
            IsPaused = false;
            _pauseEvent.Set();
            _engineCts?.Dispose();
            _engineCts = null;
        }
    }

    /// <inheritdoc/>
    public void Pause()
    {
        lock (_syncRoot)
        {
            if (!_isRunning || _isPaused)
            {
                return;
            }

            _pauseEvent.Reset();
            _isPaused = true;
            _logger.LogInformation("Backup engine paused.");
        }
    }

    /// <inheritdoc/>
    public void Resume()
    {
        lock (_syncRoot)
        {
            if (!_isRunning || !_isPaused)
            {
                return;
            }

            _pauseEvent.Set();
            _isPaused = false;
            _logger.LogInformation("Backup engine resumed.");
        }
    }

    /// <inheritdoc/>
    public void Cancel()
    {
        lock (_syncRoot)
        {
            if (!_isRunning)
            {
                return;
            }

            _logger.LogInformation("Cancelling backup engine execution.");
            _pauseEvent.Set(); // Unblock paused state so thread can observe cancellation
            _engineCts?.Cancel();
        }
    }

    /// <summary>
    /// Computes the structured destination directory and file path based on the selected organization mode.
    /// </summary>
    /// <param name="baseDestinationPath">The root directory for backups.</param>
    /// <param name="deviceName">The friendly device name.</param>
    /// <param name="mode">The folder organization mode.</param>
    /// <param name="fileName">The file name with extension.</param>
    /// <param name="creationDate">The creation timestamp of the media.</param>
    /// <returns>The combined destination filesystem path.</returns>
    public static string BuildDestinationPath(
        string baseDestinationPath,
        string deviceName,
        FileOrganizationMode mode,
        string fileName,
        DateTime creationDate)
    {
        string safeDeviceName = SanitizeFolderName(deviceName);

        return mode switch
        {
            FileOrganizationMode.ByYearMonthDay => Path.Combine(
                baseDestinationPath,
                safeDeviceName,
                creationDate.Year.ToString("D4"),
                creationDate.Month.ToString("D2"),
                creationDate.Day.ToString("D2"),
                fileName),

            FileOrganizationMode.Flat => Path.Combine(
                baseDestinationPath,
                safeDeviceName,
                fileName),

            _ => Path.Combine(
                baseDestinationPath,
                safeDeviceName,
                creationDate.Year.ToString("D4"),
                creationDate.Month.ToString("D2"),
                fileName)
        };
    }

    /// <summary>
    /// Returns a non-destructive destination path, adding a numeric suffix when a file already exists.
    /// </summary>
    public static string ResolveAvailableDestinationPath(string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        if (!File.Exists(destinationPath))
        {
            return destinationPath;
        }

        string? directory = Path.GetDirectoryName(destinationPath);
        string baseName = Path.GetFileNameWithoutExtension(destinationPath);
        string extension = Path.GetExtension(destinationPath);

        for (int suffix = 1; suffix < int.MaxValue; suffix++)
        {
            string candidateName = $"{baseName}-{suffix}{extension}";
            string candidate = string.IsNullOrEmpty(directory)
                ? candidateName
                : Path.Combine(directory, candidateName);

            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException($"Could not find an available destination name for '{destinationPath}'.");
    }

    /// <summary>
    /// Checks available free space on the drive hosting the destination folder.
    /// </summary>
    /// <param name="destinationPath">The local destination directory path.</param>
    /// <param name="requiredBytes">The estimated required bytes.</param>
    /// <param name="availableBytes">Outputs the currently available free space in bytes.</param>
    /// <returns><c>true</c> if sufficient free space exists; otherwise, <c>false</c>.</returns>
    public static bool HasSufficientDiskSpace(string destinationPath, long requiredBytes, out long availableBytes)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                availableBytes = 0;
                return false;
            }

            string fullPath = Path.GetFullPath(destinationPath);
            string root = Path.GetPathRoot(fullPath) ?? fullPath;
            var driveInfo = new DriveInfo(root);

            availableBytes = driveInfo.AvailableFreeSpace;
            return availableBytes >= requiredBytes;
        }
        catch
        {
            // Fallback for network paths or virtual drives where DriveInfo is unavailable
            availableBytes = long.MaxValue;
            return true;
        }
    }

    private static bool ShouldIncludeFile(ScannedFile file, BackupSettings settings)
    {
        return file.MediaType switch
        {
            MediaType.Photo => settings.IncludePhotos,
            MediaType.Video => settings.IncludeVideos,
            MediaType.LivePhoto => settings.IncludePhotos || settings.IncludeVideos,
            MediaType.Raw => settings.IncludeRaw,
            MediaType.Screenshot => settings.IncludeScreenshots,
            MediaType.SlowMo => settings.IncludeVideos,
            MediaType.TimeLapse => settings.IncludeVideos,
            _ => true
        };
    }

    private static string SanitizeFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Device";
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        char[] chars = name.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (invalidChars.Contains(chars[i]))
            {
                chars[i] = '_';
            }
        }

        return new string(chars).Trim();
    }

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536, useAsync: true);
        byte[] hash = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static double CalculateCurrentSpeed(Queue<(DateTime Time, long Bytes)> speedWindow)
    {
        lock (speedWindow)
        {
            if (speedWindow.Count < 2)
            {
                return 0.0;
            }

            var oldest = speedWindow.Peek();
            var newest = speedWindow.Last();
            double seconds = (newest.Time - oldest.Time).TotalSeconds;

            if (seconds <= 0.05)
            {
                return 0.0;
            }

            long totalBytes = speedWindow.Sum(x => x.Bytes);
            return totalBytes / seconds;
        }
    }

    private static double? CalculateEta(long bytesRemaining, double currentSpeedBytesPerSecond)
    {
        if (bytesRemaining <= 0)
        {
            return 0.0;
        }

        if (currentSpeedBytesPerSecond <= 1.0)
        {
            return null;
        }

        return bytesRemaining / currentSpeedBytesPerSecond;
    }

    private void ReportProgress(
        string currentFileName,
        int filesCompleted,
        int totalFiles,
        long bytesCompleted,
        long totalBytes,
        double speedBytesPerSecond,
        double? estimatedSecondsRemaining,
        BackupStatus status,
        bool isScanning = false)
    {
        var progress = new BackupProgress(
            CurrentFileName: currentFileName,
            FilesCompleted: filesCompleted,
            TotalFiles: totalFiles,
            BytesCompleted: bytesCompleted,
            TotalBytes: totalBytes,
            CurrentSpeedBytesPerSecond: speedBytesPerSecond,
            EstimatedSecondsRemaining: estimatedSecondsRemaining,
            Status: status)
        {
            IsScanning = isScanning
        };

        try
        {
            ProgressChanged?.Invoke(this, progress);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error occurred in ProgressChanged event handler.");
        }
    }

    private sealed class InlineProgress(Action<long> report) : IProgress<long>
    {
        public void Report(long value) => report(value);
    }
}
