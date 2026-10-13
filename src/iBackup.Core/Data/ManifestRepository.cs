namespace iBackup.Core.Data;

using System.Data;
using System.Globalization;
using iBackup.Core.Interfaces;
using iBackup.Core.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implements <see cref="IManifestRepository"/> providing persistent SQLite storage
/// for backup files, sessions, and device configurations using parameterized queries.
/// </summary>
public sealed class ManifestRepository : IManifestRepository
{
    private const string Iso8601Format = "o";
    private readonly ILogger<ManifestRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestRepository"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ManifestRepository(ILogger<ManifestRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing manifest database schema.");
        await ManifestDatabase.CreateDatabaseAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> IsFileBackedUpAsync(
        string deviceUdid,
        string fileName,
        long fileSize,
        DateTime creationDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceUdid);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        const string sql = """
            SELECT COUNT(1)
            FROM backed_up_files
            WHERE device_udid = @device_udid
              AND file_name = @file_name
              AND file_size = @file_size
              AND creation_date = @creation_date
              AND LOWER(status) = 'complete';
            """;

        using var connection = ManifestDatabase.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@device_udid", deviceUdid);
        command.Parameters.AddWithValue("@file_name", fileName);
        command.Parameters.AddWithValue("@file_size", fileSize);
        command.Parameters.AddWithValue("@creation_date", creationDate.ToString(Iso8601Format));

        var result = await command.ExecuteScalarAsync();
        long count = result is long val ? val : Convert.ToInt64(result, CultureInfo.InvariantCulture);
        return count > 0;
    }

    /// <inheritdoc/>
    public async Task RecordBackupAsync(BackupFileRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        const string sql = """
            INSERT INTO backed_up_files (
                device_udid, source_path, dest_path, file_name, file_size,
                creation_date, modification_date, sha256_hash, media_type,
                backup_date, status
            ) VALUES (
                @device_udid, @source_path, @dest_path, @file_name, @file_size,
                @creation_date, @modification_date, @sha256_hash, @media_type,
                @backup_date, @status
            )
            ON CONFLICT(device_udid, source_path) DO UPDATE SET
                dest_path = excluded.dest_path,
                file_name = excluded.file_name,
                file_size = excluded.file_size,
                creation_date = excluded.creation_date,
                modification_date = excluded.modification_date,
                sha256_hash = excluded.sha256_hash,
                media_type = excluded.media_type,
                backup_date = excluded.backup_date,
                status = excluded.status;

            SELECT last_insert_rowid();
            """;

        using var connection = ManifestDatabase.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@device_udid", record.DeviceUdid);
        command.Parameters.AddWithValue("@source_path", record.SourcePath);
        command.Parameters.AddWithValue("@dest_path", record.DestPath);
        command.Parameters.AddWithValue("@file_name", record.FileName);
        command.Parameters.AddWithValue("@file_size", record.FileSize);
        command.Parameters.AddWithValue("@creation_date", record.CreationDate.ToString(Iso8601Format));
        command.Parameters.AddWithValue("@modification_date", record.ModificationDate.HasValue
            ? record.ModificationDate.Value.ToString(Iso8601Format)
            : (object)DBNull.Value);
        command.Parameters.AddWithValue("@sha256_hash", record.Sha256Hash as object ?? DBNull.Value);
        command.Parameters.AddWithValue("@media_type", record.MediaType.ToString());
        command.Parameters.AddWithValue("@backup_date", record.BackupDate.ToString(Iso8601Format));
        command.Parameters.AddWithValue("@status", record.Status.ToString());

        var rowId = await command.ExecuteScalarAsync();
        if (rowId != null && rowId != DBNull.Value)
        {
            record.Id = Convert.ToInt32(rowId, CultureInfo.InvariantCulture);
        }
    }

    /// <inheritdoc/>
    public async Task UpdateRecordStatusAsync(int recordId, BackupStatus status)
    {
        const string sql = "UPDATE backed_up_files SET status = @status WHERE id = @id;";

        using var connection = ManifestDatabase.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", recordId);
        command.Parameters.AddWithValue("@status", status.ToString());

        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc/>
    public async Task<List<BackupSession>> GetBackupHistoryAsync(string? deviceUdid = null, int limit = 50)
    {
        string sql = string.IsNullOrWhiteSpace(deviceUdid)
            ? """
              SELECT id, device_udid, device_name, started_at, completed_at,
                     total_files, new_files, skipped_files, failed_files,
                     total_bytes, status
              FROM backup_sessions
              ORDER BY id DESC
              LIMIT @limit;
              """
            : """
              SELECT id, device_udid, device_name, started_at, completed_at,
                     total_files, new_files, skipped_files, failed_files,
                     total_bytes, status
              FROM backup_sessions
              WHERE device_udid = @device_udid
              ORDER BY id DESC
              LIMIT @limit;
              """;

        var sessions = new List<BackupSession>();

        using var connection = ManifestDatabase.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@limit", limit);
        if (!string.IsNullOrWhiteSpace(deviceUdid))
        {
            command.Parameters.AddWithValue("@device_udid", deviceUdid);
        }

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            sessions.Add(ParseBackupSession(reader));
        }

        return sessions;
    }

    /// <inheritdoc/>
    public async Task<BackupStats> GetStatsAsync(string deviceUdid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceUdid);

        const string fileStatsSql = """
            SELECT COUNT(1) AS total_files, COALESCE(SUM(file_size), 0) AS total_bytes
            FROM backed_up_files
            WHERE device_udid = @device_udid AND LOWER(status) = 'complete';
            """;

        const string sessionStatsSql = """
            SELECT MAX(completed_at) AS last_backup, COUNT(1) AS total_sessions
            FROM backup_sessions
            WHERE device_udid = @device_udid;
            """;

        using var connection = ManifestDatabase.GetConnection();
        await connection.OpenAsync();

        int totalFiles = 0;
        long totalBytes = 0;
        DateTime? lastBackupDate = null;
        int totalSessions = 0;

        using (var command = connection.CreateCommand())
        {
            command.CommandText = fileStatsSql;
            command.Parameters.AddWithValue("@device_udid", deviceUdid);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                totalFiles = reader.GetInt32(0);
                totalBytes = reader.GetInt64(1);
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = sessionStatsSql;
            command.Parameters.AddWithValue("@device_udid", deviceUdid);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                {
                    string dateStr = reader.GetString(0);
                    if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDate))
                    {
                        lastBackupDate = parsedDate;
                    }
                }

                totalSessions = reader.GetInt32(1);
            }
        }

        return new BackupStats(totalFiles, totalBytes, lastBackupDate, totalSessions);
    }

    /// <inheritdoc/>
    public async Task<BackupSession> CreateSessionAsync(string deviceUdid, string deviceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceUdid);

        DateTime now = DateTime.UtcNow;
        const string sql = """
            INSERT INTO backup_sessions (
                device_udid, device_name, started_at, total_files,
                new_files, skipped_files, failed_files, total_bytes, status
            ) VALUES (
                @device_udid, @device_name, @started_at, 0, 0, 0, 0, 0, @status
            );
            SELECT last_insert_rowid();
            """;

        using var connection = ManifestDatabase.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@device_udid", deviceUdid);
        command.Parameters.AddWithValue("@device_name", deviceName ?? string.Empty);
        command.Parameters.AddWithValue("@started_at", now.ToString(Iso8601Format));
        command.Parameters.AddWithValue("@status", BackupStatus.InProgress.ToString());

        var rowId = await command.ExecuteScalarAsync();
        int id = Convert.ToInt32(rowId, CultureInfo.InvariantCulture);

        return new BackupSession
        {
            Id = id,
            DeviceUdid = deviceUdid,
            DeviceName = deviceName ?? string.Empty,
            StartedAt = now,
            Status = BackupStatus.InProgress
        };
    }

    /// <inheritdoc/>
    public async Task CompleteSessionAsync(
        int sessionId,
        BackupStatus status,
        int totalFiles,
        int newFiles,
        int skippedFiles,
        int failedFiles,
        long totalBytes)
    {
        DateTime now = DateTime.UtcNow;
        const string sql = """
            UPDATE backup_sessions SET
                status = @status,
                completed_at = @completed_at,
                total_files = @total_files,
                new_files = @new_files,
                skipped_files = @skipped_files,
                failed_files = @failed_files,
                total_bytes = @total_bytes
            WHERE id = @sessionId;
            """;

        using var connection = ManifestDatabase.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@sessionId", sessionId);
        command.Parameters.AddWithValue("@status", status.ToString());
        command.Parameters.AddWithValue("@completed_at", now.ToString(Iso8601Format));
        command.Parameters.AddWithValue("@total_files", totalFiles);
        command.Parameters.AddWithValue("@new_files", newFiles);
        command.Parameters.AddWithValue("@skipped_files", skippedFiles);
        command.Parameters.AddWithValue("@failed_files", failedFiles);
        command.Parameters.AddWithValue("@total_bytes", totalBytes);

        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc/>
    public async Task<BackupSettings?> GetDeviceSettingsAsync(string deviceUdid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceUdid);

        const string sql = """
            SELECT device_udid, destination_path, auto_backup, organization_mode,
                   include_photos, include_videos, include_raw, include_screenshots,
                   verify_integrity
            FROM device_settings
            WHERE device_udid = @device_udid;
            """;

        using var connection = ManifestDatabase.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@device_udid", deviceUdid);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            string udid = reader.GetString(0);
            string dest = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            bool autoBackup = reader.GetInt32(2) != 0;
            string orgModeStr = reader.IsDBNull(3) ? "ByYearMonth" : reader.GetString(3);
            bool incPhotos = reader.GetInt32(4) != 0;
            bool incVideos = reader.GetInt32(5) != 0;
            bool incRaw = reader.GetInt32(6) != 0;
            bool incScreenshots = reader.GetInt32(7) != 0;
            bool verifyIntegrity = reader.GetInt32(8) != 0;

            if (!Enum.TryParse<FileOrganizationMode>(orgModeStr, ignoreCase: true, out var orgMode))
            {
                orgMode = FileOrganizationMode.ByYearMonth;
            }

            return new BackupSettings
            {
                DeviceUdid = udid,
                DestinationPath = dest,
                AutoBackup = autoBackup,
                OrganizationMode = orgMode,
                IncludePhotos = incPhotos,
                IncludeVideos = incVideos,
                IncludeRaw = incRaw,
                IncludeScreenshots = incScreenshots,
                VerifyIntegrity = verifyIntegrity
            };
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task SaveDeviceSettingsAsync(BackupSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.DeviceUdid);

        const string sql = """
            INSERT INTO device_settings (
                device_udid, device_name, destination_path, auto_backup,
                organization_mode, include_photos, include_videos, include_raw,
                include_screenshots, verify_integrity, last_seen
            ) VALUES (
                @device_udid, @device_name, @destination_path, @auto_backup,
                @organization_mode, @include_photos, @include_videos, @include_raw,
                @include_screenshots, @verify_integrity, @last_seen
            )
            ON CONFLICT(device_udid) DO UPDATE SET
                destination_path = excluded.destination_path,
                auto_backup = excluded.auto_backup,
                organization_mode = excluded.organization_mode,
                include_photos = excluded.include_photos,
                include_videos = excluded.include_videos,
                include_raw = excluded.include_raw,
                include_screenshots = excluded.include_screenshots,
                verify_integrity = excluded.verify_integrity,
                last_seen = excluded.last_seen;
            """;

        using var connection = ManifestDatabase.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@device_udid", settings.DeviceUdid);
        command.Parameters.AddWithValue("@device_name", settings.DeviceUdid);
        command.Parameters.AddWithValue("@destination_path", settings.DestinationPath ?? string.Empty);
        command.Parameters.AddWithValue("@auto_backup", settings.AutoBackup ? 1 : 0);
        command.Parameters.AddWithValue("@organization_mode", settings.OrganizationMode.ToString());
        command.Parameters.AddWithValue("@include_photos", settings.IncludePhotos ? 1 : 0);
        command.Parameters.AddWithValue("@include_videos", settings.IncludeVideos ? 1 : 0);
        command.Parameters.AddWithValue("@include_raw", settings.IncludeRaw ? 1 : 0);
        command.Parameters.AddWithValue("@include_screenshots", settings.IncludeScreenshots ? 1 : 0);
        command.Parameters.AddWithValue("@verify_integrity", settings.VerifyIntegrity ? 1 : 0);
        command.Parameters.AddWithValue("@last_seen", DateTime.UtcNow.ToString(Iso8601Format));

        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc/>
    public async Task CleanupPartialRecordsAsync(string deviceUdid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceUdid);

        const string sql = """
            DELETE FROM backed_up_files
            WHERE device_udid = @device_udid AND LOWER(status) != 'complete';
            """;

        using var connection = ManifestDatabase.GetConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@device_udid", deviceUdid);

        int rows = await command.ExecuteNonQueryAsync();
        _logger.LogInformation("Cleaned up {Count} non-complete records for device {DeviceUdid}.", rows, deviceUdid);
    }

    private static BackupSession ParseBackupSession(SqliteDataReader reader)
    {
        int id = reader.GetInt32(0);
        string deviceUdid = reader.GetString(1);
        string deviceName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
        DateTime startedAt = ParseIsoDateTime(reader.GetString(3));
        DateTime? completedAt = reader.IsDBNull(4) ? null : ParseIsoDateTime(reader.GetString(4));
        int totalFiles = reader.GetInt32(5);
        int newFiles = reader.GetInt32(6);
        int skippedFiles = reader.GetInt32(7);
        int failedFiles = reader.GetInt32(8);
        long totalBytes = reader.GetInt64(9);
        string statusStr = reader.GetString(10);

        if (!Enum.TryParse<BackupStatus>(statusStr, ignoreCase: true, out var status))
        {
            status = BackupStatus.InProgress;
        }

        return new BackupSession
        {
            Id = id,
            DeviceUdid = deviceUdid,
            DeviceName = deviceName,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            TotalFiles = totalFiles,
            NewFiles = newFiles,
            SkippedFiles = skippedFiles,
            FailedFiles = failedFiles,
            TotalBytes = totalBytes,
            Status = status
        };
    }

    private static DateTime ParseIsoDateTime(string value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed;
        }

        return DateTime.UtcNow;
    }
}
