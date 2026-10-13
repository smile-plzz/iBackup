namespace iBackup.Core.Interfaces;

using iBackup.Core.Models;

/// <summary>
/// Monitors connection and disconnection events of iOS devices attached via USB.
/// </summary>
public interface IDeviceMonitor : IDisposable
{
    /// <summary>
    /// Occurs when a compatible iOS device is connected and detected.
    /// </summary>
    event EventHandler<DeviceInfo>? DeviceConnected;

    /// <summary>
    /// Occurs when a previously connected device is disconnected, providing its UDID.
    /// </summary>
    event EventHandler<string>? DeviceDisconnected;

    /// <summary>
    /// Starts background polling to monitor for device connection state changes.
    /// </summary>
    /// <param name="ct">A cancellation token to observe while starting or executing monitoring.</param>
    /// <returns>A task that represents the startup of monitoring.</returns>
    Task StartMonitoringAsync(CancellationToken ct = default);

    /// <summary>
    /// Stops the background monitoring process.
    /// </summary>
    /// <returns>A task that represents the shutdown of monitoring.</returns>
    Task StopMonitoringAsync();

    /// <summary>
    /// Gets a current snapshot list of all actively connected devices.
    /// </summary>
    /// <returns>A read-only collection of <see cref="DeviceInfo"/> instances.</returns>
    IReadOnlyList<DeviceInfo> GetConnectedDevices();
}
