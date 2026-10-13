namespace iBackup.Core.Data;

using Microsoft.Data.Sqlite;

/// <summary>
/// Provides SQLite database initialization, connection string generation, and table management.
/// </summary>
public static class ManifestDatabase
{
    private const string DatabaseFileName = "manifest.db";
    private const string AppFolderName = "iBackup";

    private const string CreateTablesSql = """
        PRAGMA journal_mode = WAL;
        PRAGMA synchronous = NORMAL;

        CREATE TABLE IF NOT EXISTS backed_up_files (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            device_udid TEXT NOT NULL,
            source_path TEXT NOT NULL,
            dest_path TEXT NOT NULL,
            file_name TEXT NOT NULL,
            file_size INTEGER NOT NULL,
            creation_date TEXT NOT NULL,
            modification_date TEXT,
            sha256_hash TEXT,
            media_type TEXT NOT NULL,
            backup_date TEXT NOT NULL,
            status TEXT NOT NULL DEFAULT 'complete',
            UNIQUE(device_udid, source_path)
        );

        CREATE INDEX IF NOT EXISTS idx_fingerprint ON backed_up_files(device_udid, file_name, file_size, creation_date);

        CREATE TABLE IF NOT EXISTS backup_sessions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            device_udid TEXT NOT NULL,
            device_name TEXT NOT NULL,
            started_at TEXT NOT NULL,
            completed_at TEXT,
            total_files INTEGER DEFAULT 0,
            new_files INTEGER DEFAULT 0,
            skipped_files INTEGER DEFAULT 0,
            failed_files INTEGER DEFAULT 0,
            total_bytes INTEGER DEFAULT 0,
            status TEXT NOT NULL DEFAULT 'in_progress'
        );

        CREATE TABLE IF NOT EXISTS device_settings (
            device_udid TEXT PRIMARY KEY,
            device_name TEXT NOT NULL,
            destination_path TEXT,
            auto_backup INTEGER DEFAULT 1,
            organization_mode TEXT DEFAULT 'YearMonth',
            include_photos INTEGER DEFAULT 1,
            include_videos INTEGER DEFAULT 1,
            include_raw INTEGER DEFAULT 1,
            include_screenshots INTEGER DEFAULT 1,
            verify_integrity INTEGER DEFAULT 0,
            minimize_to_tray INTEGER DEFAULT 1,
            last_seen TEXT
        );
        """;

    /// <summary>
    /// Gets the absolute filesystem path to the SQLite manifest database file.
    /// </summary>
    /// <returns>The full path to manifest.db located in the user's LocalApplicationData folder.</returns>
    public static string GetDatabasePath()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, AppFolderName, DatabaseFileName);
    }

    /// <summary>
    /// Gets or sets an optional custom connection string (primarily used for unit/integration testing).
    /// </summary>
    public static string? CustomConnectionString { get; set; }

    /// <summary>
    /// Gets the standard SQLite connection string pointing to the manifest database.
    /// </summary>
    /// <returns>A connection string string suitable for <see cref="SqliteConnection"/>.</returns>
    public static string GetConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(CustomConnectionString))
        {
            return CustomConnectionString;
        }

        string dbPath = GetDatabasePath();
        return $"Data Source={dbPath};";
    }

    /// <summary>
    /// Creates a new <see cref="SqliteConnection"/> using the default connection string.
    /// </summary>
    /// <returns>An un-opened <see cref="SqliteConnection"/> instance.</returns>
    public static SqliteConnection GetConnection()
    {
        return new SqliteConnection(GetConnectionString());
    }

    /// <summary>
    /// Ensures the database directory, database file, and schema tables and indexes exist.
    /// </summary>
    public static void CreateDatabase()
    {
        if (string.IsNullOrWhiteSpace(CustomConnectionString))
        {
            string dbPath = GetDatabasePath();
            string? directory = Path.GetDirectoryName(dbPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        using var connection = GetConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = CreateTablesSql;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Asynchronously ensures the database directory, database file, and schema tables and indexes exist.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the initialization operation.</returns>
    public static async Task CreateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(CustomConnectionString))
        {
            string dbPath = GetDatabasePath();
            string? directory = Path.GetDirectoryName(dbPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        using var connection = GetConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = CreateTablesSql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
