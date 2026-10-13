namespace iBackup.Core.Tests.Tests;

using FluentAssertions;
using iBackup.Core.Data;
using iBackup.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class ManifestRepositoryTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly ManifestRepository _repository;

    public ManifestRepositoryTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"ibackup_test_{Guid.NewGuid():N}.db");
        ManifestDatabase.CustomConnectionString = $"Data Source={_tempDbPath};";

        _repository = new ManifestRepository(NullLogger<ManifestRepository>.Instance);
    }

    public void Dispose()
    {
        ManifestDatabase.CustomConnectionString = null;

        if (File.Exists(_tempDbPath))
        {
            try
            {
                File.Delete(_tempDbPath);
            }
            catch
            {
                // Ignore cleanup lock issues in tests
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreateDatabaseAndTablesWithoutError()
    {
        // Act
        var act = async () => await _repository.InitializeAsync();

        // Assert
        await act.Should().NotThrowAsync();
        File.Exists(_tempDbPath).Should().BeTrue();
    }

    [Fact]
    public async Task RecordBackupAsync_And_IsFileBackedUpAsync_ShouldFunctionCorrectly()
    {
        // Arrange
        await _repository.InitializeAsync();

        var record = new BackupFileRecord
        {
            DeviceUdid = "test-device-001",
            SourcePath = "/DCIM/100APPLE/IMG_0001.HEIC",
            DestPath = @"D:\Backup\iPhone\2026\10\IMG_0001.HEIC",
            FileName = "IMG_0001.HEIC",
            FileSize = 2048576,
            CreationDate = new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc),
            ModificationDate = new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc),
            Sha256Hash = "dummyhash123",
            MediaType = MediaType.Photo,
            BackupDate = DateTime.UtcNow,
            Status = BackupStatus.Complete
        };

        // Act & Assert before record
        var existsBefore = await _repository.IsFileBackedUpAsync(
            record.DeviceUdid,
            record.FileName,
            record.FileSize,
            record.CreationDate);
        existsBefore.Should().BeFalse();

        // Act record
        await _repository.RecordBackupAsync(record);

        // Act & Assert after record
        var existsAfter = await _repository.IsFileBackedUpAsync(
            record.DeviceUdid,
            record.FileName,
            record.FileSize,
            record.CreationDate);
        existsAfter.Should().BeTrue();
    }

    [Fact]
    public async Task CreateSessionAsync_And_CompleteSessionAsync_ShouldPersistHistory()
    {
        // Arrange
        await _repository.InitializeAsync();

        // Act
        var session = await _repository.CreateSessionAsync("test-device-001", "Ismail's iPhone");
        session.Id.Should().BeGreaterThan(0);

        await _repository.CompleteSessionAsync(
            sessionId: session.Id,
            status: BackupStatus.Complete,
            totalFiles: 100,
            newFiles: 20,
            skippedFiles: 80,
            failedFiles: 0,
            totalBytes: 52428800);

        var history = await _repository.GetBackupHistoryAsync("test-device-001");

        // Assert
        history.Should().NotBeEmpty();
        var completed = history.First();
        completed.Id.Should().Be(session.Id);
        completed.Status.Should().Be(BackupStatus.Complete);
        completed.TotalFiles.Should().Be(100);
        completed.NewFiles.Should().Be(20);
        completed.SkippedFiles.Should().Be(80);
    }

    [Fact]
    public async Task CleanupPartialRecordsAsync_ShouldRemoveIncompleteRecords()
    {
        // Arrange
        await _repository.InitializeAsync();

        var partialRecord = new BackupFileRecord
        {
            DeviceUdid = "test-device-001",
            SourcePath = "/DCIM/100APPLE/IMG_PARTIAL.MOV",
            DestPath = @"D:\Backup\iPhone\2026\10\IMG_PARTIAL.MOV",
            FileName = "IMG_PARTIAL.MOV",
            FileSize = 50000000,
            CreationDate = DateTime.UtcNow,
            MediaType = MediaType.Video,
            BackupDate = DateTime.UtcNow,
            Status = BackupStatus.Partial
        };

        await _repository.RecordBackupAsync(partialRecord);

        // Act
        await _repository.CleanupPartialRecordsAsync("test-device-001");

        // Assert
        var exists = await _repository.IsFileBackedUpAsync(
            partialRecord.DeviceUdid,
            partialRecord.FileName,
            partialRecord.FileSize,
            partialRecord.CreationDate);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeviceSettings_ShouldSaveAndRetrieveProperly()
    {
        // Arrange
        await _repository.InitializeAsync();

        var settings = new BackupSettings
        {
            DeviceUdid = "test-device-001",
            DestinationPath = @"D:\Photos\iPhoneBackup",
            AutoBackup = true,
            OrganizationMode = FileOrganizationMode.ByYearMonth,
            IncludePhotos = true,
            IncludeVideos = true,
            IncludeRaw = false,
            IncludeScreenshots = true,
            VerifyIntegrity = true,
            MinimizeToTray = false
        };

        // Act
        await _repository.SaveDeviceSettingsAsync(settings);
        var loaded = await _repository.GetDeviceSettingsAsync("test-device-001");

        // Assert
        loaded.Should().NotBeNull();
        loaded!.DestinationPath.Should().Be(@"D:\Photos\iPhoneBackup");
        loaded.IncludeRaw.Should().BeFalse();
        loaded.VerifyIntegrity.Should().BeTrue();
        loaded.MinimizeToTray.Should().BeFalse();
    }
}
