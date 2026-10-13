namespace iBackup.Core.Exceptions;

/// <summary>
/// Raised when Windows can see an Apple device but cannot access its photo library.
/// </summary>
public sealed class DeviceMediaAccessException : Exception
{
    public DeviceMediaAccessException(string message)
        : base(message)
    {
    }

    public DeviceMediaAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
