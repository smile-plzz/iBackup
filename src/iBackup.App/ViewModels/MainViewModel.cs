using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using iBackup.App.Converters;
using iBackup.Core.Interfaces;
using iBackup.Core.Models;

namespace iBackup.App.ViewModels;

/// <summary>
/// ViewModel for the main dashboard view, managing device connection status,
/// backup execution lifecycle, and live transfer progress reporting.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IDeviceMonitor _deviceMonitor;
    private readonly IBackupEngine _backupEngine;
    private readonly IManifestRepository _manifestRepository;
    private readonly DispatcherQueue? _dispatcherQueue;
    private CancellationTokenSource? _backupCts;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartBackupCommand))]
    private DeviceInfo? _connectedDevice;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartBackupCommand))]
    private string _destinationPath = string.Empty;

    [ObservableProperty]
    private string _statusText = "Waiting for device...";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartBackupCommand))]
    [NotifyCanExecuteChangedFor(nameof(PauseResumeCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelBackupCommand))]
    private bool _isBackingUp;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartBackupCommand))]
    private bool _isDeviceConnected;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _filesProgress = "0 / 0";

    [ObservableProperty]
    private string _bytesProgress = "0 B / 0 B";

    [ObservableProperty]
    private string _estimatedTimeRemaining = "--:--";

    [ObservableProperty]
    private string _lastBackupSummary = "No backups recorded yet.";

    [ObservableProperty]
    private bool _autoBackupOnConnect = true;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    /// <summary>
    /// Event fired when the user requests folder selection via the Browse command.
    /// </summary>
    public event Action? RequestBrowseFolder;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="deviceMonitor">The device monitor service for detecting iOS devices.</param>
    /// <param name="backupEngine">The backup engine coordinating media transfers.</param>
    /// <param name="manifestRepository">The SQLite manifest repository for historical backup metadata.</param>
    public MainViewModel(
        IDeviceMonitor deviceMonitor,
        IBackupEngine backupEngine,
        IManifestRepository manifestRepository)
    {
        _deviceMonitor = deviceMonitor ?? throw new ArgumentNullException(nameof(deviceMonitor));
        _backupEngine = backupEngine ?? throw new ArgumentNullException(nameof(backupEngine));
        _manifestRepository = manifestRepository ?? throw new ArgumentNullException(nameof(manifestRepository));
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // Default backup destination to Pictures\iBackup
        var defaultPictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        DestinationPath = Path.Combine(defaultPictures, "iBackup");

        // Subscribe to device monitor events
        _deviceMonitor.DeviceConnected += OnDeviceConnected;
        _deviceMonitor.DeviceDisconnected += OnDeviceDisconnected;

        // Subscribe to backup engine events
        _backupEngine.ProgressChanged += OnBackupProgressChanged;
        _backupEngine.BackupCompleted += OnBackupCompleted;
        _backupEngine.BackupFailed += OnBackupFailed;

        // Check for already connected devices on startup
        CheckInitialDeviceConnection();

        // Load historical stats
        _ = LoadInitialStatsAsync();
    }

    /// <summary>
    /// Checks if a device is already connected at launch time.
    /// </summary>
    private void CheckInitialDeviceConnection()
    {
        try
        {
            var connectedDevices = _deviceMonitor.GetConnectedDevices();
            if (connectedDevices != null && connectedDevices.Count > 0)
            {
                var device = connectedDevices[0];
                ConnectedDevice = device;
                IsDeviceConnected = true;
                StatusText = $"Connected to {device.Name}";
                _ = LoadDeviceSettingsAsync(device.Udid);
            }
        }
        catch
        {
            // Fallback to default disconnected state
            IsDeviceConnected = false;
        }
    }

    /// <summary>
    /// Loads saved settings for the specified device.
    /// </summary>
    private async Task LoadDeviceSettingsAsync(string udid)
    {
        try
        {
            var settings = await _manifestRepository.GetDeviceSettingsAsync(udid);
            if (settings != null)
            {
                RunOnUIThread(() =>
                {
                    if (!string.IsNullOrWhiteSpace(settings.DestinationPath))
                    {
                        DestinationPath = settings.DestinationPath;
                    }
                    AutoBackupOnConnect = settings.AutoBackup;
                });
            }
        }
        catch
        {
            // Ignored - use defaults
        }
    }

    /// <summary>
    /// Loads initial backup statistics from the manifest repository.
    /// </summary>
    private async Task LoadInitialStatsAsync()
    {
        try
        {
            var stats = await _manifestRepository.GetStatsAsync();
            if (stats != null && stats.TotalBackups > 0)
            {
                RunOnUIThread(() =>
                {
                    LastBackupSummary = $"Total backups: {stats.TotalBackups:N0} | {stats.TotalFiles:N0} files ({FileSizeConverter.FormatBytes(stats.TotalBytes)})";
                });
            }
        }
        catch
        {
            // Ignored - use default summary
        }
    }

    private bool CanStartBackup => IsDeviceConnected && !IsBackingUp && !string.IsNullOrWhiteSpace(DestinationPath);

    private bool CanPauseResume => IsBackingUp;

    private bool CanCancelBackup => IsBackingUp;

    /// <summary>
    /// Starts the backup operation for the currently connected device.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStartBackup))]
    public async Task StartBackupAsync()
    {
        if (ConnectedDevice == null || string.IsNullOrWhiteSpace(DestinationPath))
        {
            return;
        }

        try
        {
            IsBackingUp = true;
            IsPaused = false;
            IsScanning = true;
            ProgressPercent = 0;
            FilesProgress = "Scanning...";
            BytesProgress = "--";
            EstimatedTimeRemaining = "Calculating...";
            StatusText = $"Scanning media on {ConnectedDevice.Name}...";

            _backupCts = new CancellationTokenSource();

            var settings = new BackupSettings
            {
                DestinationPath = DestinationPath,
                AutoBackup = AutoBackupOnConnect,
                OrganizationMode = FileOrganizationMode.ByYearMonth,
                BackupPhotos = true,
                BackupVideos = true,
                BackupRaw = true,
                BackupScreenshots = true,
                VerifyIntegrity = true,
                MinimizeToTray = MinimizeToTray
            };

            // Save device settings
            try
            {
                await _manifestRepository.SaveDeviceSettingsAsync(ConnectedDevice.Udid, settings);
            }
            catch
            {
                // Non-fatal
            }

            await _backupEngine.StartBackupAsync(ConnectedDevice, settings, _backupCts.Token);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Backup was cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Backup error: {ex.Message}";
        }
        finally
        {
            IsBackingUp = false;
            IsScanning = false;
            IsPaused = false;
        }
    }

    /// <summary>
    /// Toggles between pausing and resuming the current backup session.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPauseResume))]
    public void PauseResume()
    {
        if (!IsBackingUp)
        {
            return;
        }

        if (IsPaused)
        {
            _backupEngine.Resume();
            IsPaused = false;
            StatusText = "Resuming backup...";
        }
        else
        {
            _backupEngine.Pause();
            IsPaused = true;
            StatusText = "Backup paused";
        }
    }

    /// <summary>
    /// Cancels the currently active backup operation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancelBackup))]
    public void CancelBackup()
    {
        if (!IsBackingUp)
        {
            return;
        }

        StatusText = "Cancelling backup...";
        _backupCts?.Cancel();
        _backupEngine.Cancel();
    }

    /// <summary>
    /// Triggers folder picker dialog in the View.
    /// </summary>
    [RelayCommand]
    public void BrowseDestination()
    {
        RequestBrowseFolder?.Invoke();
    }

    private void OnDeviceConnected(DeviceInfo device)
    {
        RunOnUIThread(() =>
        {
            ConnectedDevice = device;
            IsDeviceConnected = true;
            StatusText = $"Connected to {device.Name}";

            _ = LoadDeviceSettingsAsync(device.Udid);

            // Auto-backup on connect if enabled
            if (AutoBackupOnConnect && CanStartBackup)
            {
                _ = StartBackupAsync();
            }
        });
    }

    private void OnDeviceDisconnected(string udid)
    {
        RunOnUIThread(() =>
        {
            if (ConnectedDevice?.Udid == udid || !IsBackingUp)
            {
                ConnectedDevice = null;
                IsDeviceConnected = false;
                StatusText = "Device disconnected. Waiting for device...";

                if (IsBackingUp)
                {
                    IsBackingUp = false;
                    IsScanning = false;
                    IsPaused = false;
                    StatusText = "Device disconnected during backup.";
                }
            }
        });
    }

    private void OnBackupProgressChanged(BackupProgress progress)
    {
        RunOnUIThread(() =>
        {
            IsScanning = false;
            ProgressPercent = progress.Percent;
            FilesProgress = $"{progress.ProcessedFiles:N0} / {progress.TotalFiles:N0} files";
            BytesProgress = $"{FileSizeConverter.FormatBytes(progress.ProcessedBytes)} / {FileSizeConverter.FormatBytes(progress.TotalBytes)}";

            if (progress.EstimatedRemaining.HasValue)
            {
                var remaining = progress.EstimatedRemaining.Value;
                EstimatedTimeRemaining = remaining.TotalHours >= 1
                    ? $"{remaining.Hours}h {remaining.Minutes}m remaining"
                    : $"{remaining.Minutes}m {remaining.Seconds}s remaining";
            }
            else
            {
                EstimatedTimeRemaining = "--:--";
            }

            if (!string.IsNullOrWhiteSpace(progress.CurrentFileName))
            {
                StatusText = $"Backing up: {progress.CurrentFileName}";
            }
        });
    }

    private void OnBackupCompleted(BackupSession session)
    {
        RunOnUIThread(() =>
        {
            IsBackingUp = false;
            IsScanning = false;
            IsPaused = false;
            ProgressPercent = 100;
            StatusText = $"Backup complete! {session.NewFiles:N0} new files backed up.";
            LastBackupSummary = $"Last backup: {session.StartedAt:g} - {session.NewFiles:N0} files ({FileSizeConverter.FormatBytes(session.TotalBytes)})";
            _ = LoadInitialStatsAsync();
        });
    }

    private void OnBackupFailed(Exception ex)
    {
        RunOnUIThread(() =>
        {
            IsBackingUp = false;
            IsScanning = false;
            IsPaused = false;
            StatusText = $"Backup failed: {ex.Message}";
        });
    }

    private void RunOnUIThread(Action action)
    {
        if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
        {
            _dispatcherQueue.TryEnqueue(() => action());
        }
        else
        {
            action();
        }
    }
}
