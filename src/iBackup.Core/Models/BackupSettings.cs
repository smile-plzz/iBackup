namespace iBackup.Core.Models;

/// <summary>
/// Configures backup preferences and target storage options for a specific iOS device.
/// </summary>
public class BackupSettings
{
    /// <summary>
    /// Gets or sets the unique identifier of the device these settings apply to.
    /// </summary>
    public string DeviceUdid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base local directory path where backups will be stored.
    /// </summary>
    public string DestinationPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether automatic backup should trigger when the device connects.
    /// </summary>
    public bool AutoBackup { get; set; } = true;

    /// <summary>
    /// Gets or sets the directory organization structure for backed up files.
    /// </summary>
    public FileOrganizationMode OrganizationMode { get; set; } = FileOrganizationMode.ByYearMonth;

    /// <summary>
    /// Gets or sets a value indicating whether regular photos should be backed up.
    /// </summary>
    public bool IncludePhotos { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether videos should be backed up.
    /// </summary>
    public bool IncludeVideos { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether RAW/DNG media files should be backed up.
    /// </summary>
    public bool IncludeRaw { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether screenshots should be included in the backup.
    /// </summary>
    public bool IncludeScreenshots { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether file integrity should be verified using SHA-256 after transfer.
    /// </summary>
    public bool VerifyIntegrity { get; set; } = false;
}
