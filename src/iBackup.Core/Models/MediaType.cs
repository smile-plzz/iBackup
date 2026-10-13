namespace iBackup.Core.Models;

/// <summary>
/// Specifies the type of media discovered on an iOS device.
/// </summary>
public enum MediaType
{
    /// <summary>
    /// Standard still photo (JPEG, HEIC, PNG, etc.).
    /// </summary>
    Photo,

    /// <summary>
    /// Standard video recording (MOV, MP4, etc.).
    /// </summary>
    Video,

    /// <summary>
    /// Apple Live Photo pairing photo and motion component.
    /// </summary>
    LivePhoto,

    /// <summary>
    /// Apple ProRAW or digital camera raw photo format (DNG).
    /// </summary>
    Raw,

    /// <summary>
    /// Screen capture image captured on the device.
    /// </summary>
    Screenshot,

    /// <summary>
    /// High frame rate slow-motion video recording.
    /// </summary>
    SlowMo,

    /// <summary>
    /// Time-lapse video sequence.
    /// </summary>
    TimeLapse
}
