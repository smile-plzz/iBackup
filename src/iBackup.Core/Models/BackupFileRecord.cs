namespace iBackup.Core.Models;

/// <summary>
/// Represents a persistent record of a file backed up to the local destination.
/// </summary>
public class BackupFileRecord
{
    /// <summary>
    /// Gets or sets the primary key identifier of the record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the source device.
    /// </summary>
    public string DeviceUdid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original file path on the device.
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the absolute local path where the file is stored.
    /// </summary>
    public string DestPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the file including its extension.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp of the file.
    /// </summary>
    public DateTime CreationDate { get; set; }

    /// <summary>
    /// Gets or sets the modification timestamp of the file, if available.
    /// </summary>
    public DateTime? ModificationDate { get; set; }

    /// <summary>
    /// Gets or sets the computed SHA-256 integrity hash of the backed up file, if integrity check was performed.
    /// </summary>
    public string? Sha256Hash { get; set; }

    /// <summary>
    /// Gets or sets the media classification of the file.
    /// </summary>
    public MediaType MediaType { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the backup was executed.
    /// </summary>
    public DateTime BackupDate { get; set; }

    /// <summary>
    /// Gets or sets the completion status of the backup for this file.
    /// </summary>
    public BackupStatus Status { get; set; } = BackupStatus.Complete;
}
