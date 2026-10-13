namespace iBackup.Core.Models;

/// <summary>
/// Specifies the folder organization strategy used when saving files to the destination path.
/// </summary>
public enum FileOrganizationMode
{
    /// <summary>
    /// Organizes files into directories structured by Year and Month (e.g., Destination/DeviceName/YYYY/MM/File.jpg).
    /// </summary>
    ByYearMonth,

    /// <summary>
    /// Organizes files into directories structured by Year, Month, and Day (e.g., Destination/DeviceName/YYYY/MM/DD/File.jpg).
    /// </summary>
    ByYearMonthDay,

    /// <summary>
    /// Places all files directly into the device root folder without date subfolders (e.g., Destination/DeviceName/File.jpg).
    /// </summary>
    Flat
}
