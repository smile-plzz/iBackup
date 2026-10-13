namespace iBackup.Core.Services;

using System.Collections.Concurrent;
using iBackup.Core.Interfaces;
using iBackup.Core.Models;
using MediaDevices;
using Microsoft.Extensions.Logging;

/// <summary>
/// Monitors connected iOS devices using the Windows Portable Devices (WPD) MTP protocol via MediaDevices.
/// Periodically polls for attached and detached devices and raises corresponding events.
/// </summary>
public sealed class DeviceMonitorService : IDeviceMonitor
{
    private readonly ILogger<DeviceMonitorService> _logger;
    private readonly ConcurrentDictionary<string, DeviceInfo> _connectedDevices = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncRoot = new();

    private CancellationTokenSource? _cts;
    private Task? _monitoringTask;
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler<DeviceInfo>? DeviceConnected;

    /// <inheritdoc/>
    public event EventHandler<string>? DeviceDisconnected;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceMonitorService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public DeviceMonitorService(ILogger<DeviceMonitorService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task StartMonitoringAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_syncRoot)
        {
            if (_monitoringTask != null && !_monitoringTask.IsCompleted)
            {
                _logger.LogInformation("Device monitoring is already running.");
                return Task.CompletedTask;
            }

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _logger.LogInformation("Starting iOS device monitor polling loop.");
            _monitoringTask = Task.Run(() => PollDevicesLoopAsync(_cts.Token), CancellationToken.None);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopMonitoringAsync()
    {
        CancellationTokenSource? ctsToCancel;
        Task? taskToWait;

        lock (_syncRoot)
        {
            ctsToCancel = _cts;
            taskToWait = _monitoringTask;
            _cts = null;
            _monitoringTask = null;
        }

        if (ctsToCancel != null)
        {
            try
            {
                await ctsToCancel.CancelAsync();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Exception while requesting cancellation of device monitor.");
            }
        }

        if (taskToWait != null)
        {
            try
            {
                await taskToWait;
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while awaiting device monitor completion.");
            }
            finally
            {
                ctsToCancel?.Dispose();
            }
        }

        _logger.LogInformation("Device monitoring stopped.");
    }

    /// <inheritdoc/>
    public IReadOnlyList<DeviceInfo> GetConnectedDevices()
    {
        return _connectedDevices.Values.ToList().AsReadOnly();
    }

    private async Task PollDevicesLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

        // Initial scan on start
        try
        {
            ScanDevices();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during initial device scan.");
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(cancellationToken))
                {
                    break;
                }

                ScanDevices();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in device monitor poll cycle. Continuing monitoring.");
            }
        }
    }

    private void ScanDevices()
    {
        IEnumerable<MediaDevice> deviceList;
        try
        {
            deviceList = MediaDeviceManager.Instance.GetDevices() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate media devices.");
            return;
        }

        var currentDeviceMap = new Dictionary<string, DeviceInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var mediaDevice in deviceList)
        {
            if (!IsAppleMobileDevice(mediaDevice))
            {
                continue;
            }

            try
            {
                string udid = ResolveDeviceUdid(mediaDevice);
                // Do not repeatedly open and close the shared WPD connection
                // while the scanner or transfer service owns it.
                if (_connectedDevices.TryGetValue(udid, out var knownDevice))
                {
                    currentDeviceMap[udid] = knownDevice;
                    continue;
                }
                string friendlyName = !string.IsNullOrWhiteSpace(mediaDevice.FriendlyName)
                    ? mediaDevice.FriendlyName
                    : (!string.IsNullOrWhiteSpace(mediaDevice.Description) ? mediaDevice.Description : "Apple iPhone");

                string model = !string.IsNullOrWhiteSpace(mediaDevice.Model)
                    ? mediaDevice.Model
                    : (!string.IsNullOrWhiteSpace(mediaDevice.Description) ? mediaDevice.Description : "iOS Device");

                long totalBytes = 0;
                long usedBytes = 0;
                string? iosVersion = mediaDevice.FirmwareVersion;

                try
                {
                    mediaDevice.Connect();
                    try
                    {
                        var storageObjectIds = mediaDevice.FunctionalObjects(FunctionalCategory.Storage);
                        if (storageObjectIds != null)
                        {
                            foreach (var storageObjectId in storageObjectIds)
                            {
                                var storage = mediaDevice.GetStorageInfo(storageObjectId);
                                if (storage == null)
                                {
                                    continue;
                                }

                                long capacity = (long)storage.Capacity;
                                long freeSpace = (long)storage.FreeSpaceInBytes;
                                totalBytes += capacity;
                                usedBytes += Math.Max(0L, capacity - freeSpace);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not query storage information for device {FriendlyName}.", friendlyName);
                    }
                    finally
                    {
                        try
                        {
                            mediaDevice.Disconnect();
                        }
                        catch
                        {
                            // Disconnect cleanup error
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not connect to device {FriendlyName} to inspect storage details.", friendlyName);
                }

                var deviceInfo = new DeviceInfo(
                    Udid: udid,
                    Name: friendlyName,
                    Model: model,
                    IOSVersion: iosVersion,
                    TotalStorageBytes: totalBytes,
                    UsedStorageBytes: usedBytes,
                    IsConnected: true);

                currentDeviceMap[udid] = deviceInfo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to inspect individual media device.");
            }
        }

        // Identify newly connected devices
        foreach (var kvp in currentDeviceMap)
        {
            if (_connectedDevices.TryAdd(kvp.Key, kvp.Value))
            {
                _logger.LogInformation("New iOS device connected: {DeviceName} ({DeviceUdid})", kvp.Value.Name, kvp.Key);
                try
                {
                    DeviceConnected?.Invoke(this, kvp.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in DeviceConnected event handler for {DeviceUdid}.", kvp.Key);
                }
            }
            else
            {
                // Update existing record
                _connectedDevices[kvp.Key] = kvp.Value;
            }
        }

        // Identify disconnected devices
        var disconnectedKeys = _connectedDevices.Keys.Where(k => !currentDeviceMap.ContainsKey(k)).ToList();
        foreach (var disconnectedKey in disconnectedKeys)
        {
            if (_connectedDevices.TryRemove(disconnectedKey, out var removedDevice))
            {
                _logger.LogInformation("iOS device disconnected: {DeviceName} ({DeviceUdid})", removedDevice.Name, disconnectedKey);
                try
                {
                    DeviceDisconnected?.Invoke(this, disconnectedKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in DeviceDisconnected event handler for {DeviceUdid}.", disconnectedKey);
                }
            }
        }
    }

    private static string ResolveDeviceUdid(MediaDevice device)
    {
        if (!string.IsNullOrWhiteSpace(device.DeviceId))
        {
            return device.DeviceId;
        }

        if (!string.IsNullOrWhiteSpace(device.SerialNumber))
        {
            return device.SerialNumber;
        }

        if (!string.IsNullOrWhiteSpace(device.FriendlyName))
        {
            return device.FriendlyName;
        }

        return Guid.NewGuid().ToString("N");
    }

    private static bool IsAppleMobileDevice(MediaDevice device)
    {
        string identity = string.Join('|',
            device.DeviceId,
            device.FriendlyName,
            device.Description,
            device.Model,
            device.Manufacturer);

        return identity.Contains("vid_05ac", StringComparison.OrdinalIgnoreCase)
            || identity.Contains("iphone", StringComparison.OrdinalIgnoreCase)
            || identity.Contains("ipad", StringComparison.OrdinalIgnoreCase)
            || identity.Contains("apple", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _connectedDevices.Clear();
    }
}
