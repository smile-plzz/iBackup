namespace iBackup.Core.Models;

/// <summary>
/// Represents the outcome of an individual file transfer operation.
/// </summary>
/// <param name="Success">A value indicating whether the transfer succeeded.</param>
/// <param name="BytesTransferred">The number of bytes written to the destination file.</param>
/// <param name="ErrorMessage">The error description if the transfer failed, or null if successful.</param>
/// <param name="Duration">The elapsed duration of the transfer operation.</param>
public record TransferResult(
    bool Success,
    long BytesTransferred,
    string? ErrorMessage,
    TimeSpan Duration);
