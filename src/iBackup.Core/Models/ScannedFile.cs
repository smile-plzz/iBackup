namespace iBackup.Core.Models;

/// <summary>
/// Represents a media file discovered during device scanning.
/// </summary>
/// <param name="SourcePath">The full path of the file on the source device storage.</param>
/// <param name="FileName">The file name including its extension.</param>
/// <param name="FileSize">The size of the file in bytes.</param>
/// <param name="CreationDate">The creation timestamp of the file.</param>
/// <param name="ModificationDate">The last modification timestamp of the file, if available.</param>
/// <param name="MediaType">The categorized media type of the file.</param>
public record ScannedFile(
    string SourcePath,
    string FileName,
    long FileSize,
    DateTime CreationDate,
    DateTime? ModificationDate,
    MediaType MediaType)
{
    /// <summary>
    /// Gets a fast unique fingerprint string composed of file name, size, and creation timestamp for deduplication.
    /// </summary>
    public string Fingerprint => $"{FileName}_{FileSize}_{CreationDate:yyyyMMddHHmmss}";
}
