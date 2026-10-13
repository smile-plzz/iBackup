namespace iBackup.Core.Tests.Tests;

using FluentAssertions;
using iBackup.Core.Interfaces;
using iBackup.Core.Models;
using iBackup.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public class BackupEngineTests
{
    private readonly Mock<IMediaScanner> _mediaScannerMock;
    private readonly Mock<IFileTransferService> _fileTransferMock;
    private readonly Mock<IManifestRepository> _manifestRepoMock;
    private readonly BackupEngine _engine;
    private readonly string _tempDestination;

    public BackupEngineTests()
    {
        _mediaScannerMock = new Mock<IMediaScanner>();
        _fileTransferMock = new Mock<IFileTransferService>();
        _manifestRepoMock = new Mock<IManifestRepository>();

        _tempDestination = Path.Combine(Path.GetTempPath(), $"ibackup_dest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDestination);

        _engine = new BackupEngine(
            _mediaScannerMock.Object,
            _fileTransferMock.Object,
            _manifestRepoMock.Object,
            NullLogger<BackupEngine>.Instance);
    }

    private static async IAsyncEnumerable<ScannedFile> ToAsyncEnumerable(IEnumerable<ScannedFile> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }

    [Fact]
    public async Task StartBackupAsync_ShouldTransferNewFiles_AndSkipExistingOnes()
    {
        // Arrange
        var device = new DeviceInfo(
            Udid: "test-device-001",
            Name: "Ismail's iPhone",
            Model: "iPhone 16 Pro",
            IOSVersion: "18.0",
            TotalStorageBytes: 128_000_000_000,
            UsedStorageBytes: 64_000_000_000,
            IsConnected: true);

        var settings = new BackupSettings
        {
            DeviceUdid = device.Udid,
            DestinationPath = _tempDestination,
            AutoBackup = true,
            OrganizationMode = FileOrganizationMode.ByYearMonth,
            IncludePhotos = true,
            IncludeVideos = true
        };

        var file1 = new ScannedFile(
            SourcePath: "/DCIM/100APPLE/IMG_0001.JPG",
            FileName: "IMG_0001.JPG",
            FileSize: 2_000_000,
            CreationDate: new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
            ModificationDate: null,
            MediaType: MediaType.Photo);

        var file2 = new ScannedFile(
            SourcePath: "/DCIM/100APPLE/IMG_0002.JPG",
            FileName: "IMG_0002.JPG",
            FileSize: 3_000_000,
            CreationDate: new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc),
            ModificationDate: null,
            MediaType: MediaType.Photo);

        _mediaScannerMock
            .Setup(s => s.ScanMediaFilesAsync(device, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[] { file1, file2 }));

        // file1 is already backed up; file2 is new
        _manifestRepoMock
            .Setup(r => r.IsFileBackedUpAsync(device.Udid, file1.FileName, file1.FileSize, file1.CreationDate))
            .ReturnsAsync(true);

        _manifestRepoMock
            .Setup(r => r.IsFileBackedUpAsync(device.Udid, file2.FileName, file2.FileSize, file2.CreationDate))
            .ReturnsAsync(false);

        _manifestRepoMock
            .Setup(r => r.CreateSessionAsync(device.Udid, device.Name))
            .ReturnsAsync(new BackupSession
            {
                Id = 1,
                DeviceUdid = device.Udid,
                DeviceName = device.Name,
                StartedAt = DateTime.UtcNow
            });

        _fileTransferMock
            .Setup(t => t.TransferFileAsync(
                device.Udid,
                file2.SourcePath,
                It.IsAny<string>(),
                It.IsAny<IProgress<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransferResult(
                Success: true,
                BytesTransferred: file2.FileSize,
                ErrorMessage: null,
                Duration: TimeSpan.FromSeconds(1)));

        var progressEvents = new List<BackupProgress>();
        _engine.ProgressChanged += (_, progress) => progressEvents.Add(progress);

        // Act
        var session = await _engine.StartBackupAsync(device, settings);

        // Assert
        session.Should().NotBeNull();
        progressEvents.Should().NotBeEmpty();

        // Verify transfer was only attempted for file2
        _fileTransferMock.Verify(t => t.TransferFileAsync(
            device.Udid,
            file1.SourcePath,
            It.IsAny<string>(),
            It.IsAny<IProgress<long>>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _fileTransferMock.Verify(t => t.TransferFileAsync(
            device.Udid,
            file2.SourcePath,
            It.IsAny<string>(),
            It.IsAny<IProgress<long>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify session was marked complete with 1 new and 1 skipped
        _manifestRepoMock.Verify(r => r.CompleteSessionAsync(
            1,
            BackupStatus.Complete,
            2, // total
            1, // new
            1, // skipped
            0, // failed
            file2.FileSize), Times.Once);
    }

    [Fact]
    public async Task StartBackupAsync_WhenFileTransferFails_ShouldMarkFailedAndContinue()
    {
        // Arrange
        var device = new DeviceInfo(
            Udid: "test-device-001",
            Name: "Ismail's iPhone",
            Model: "iPhone 16 Pro",
            IOSVersion: "18.0",
            TotalStorageBytes: 128_000_000_000,
            UsedStorageBytes: 64_000_000_000,
            IsConnected: true);

        var settings = new BackupSettings
        {
            DeviceUdid = device.Udid,
            DestinationPath = _tempDestination,
            IncludePhotos = true
        };

        var failingFile = new ScannedFile(
            SourcePath: "/DCIM/100APPLE/IMG_CORRUPT.JPG",
            FileName: "IMG_CORRUPT.JPG",
            FileSize: 1_000_000,
            CreationDate: DateTime.UtcNow,
            ModificationDate: null,
            MediaType: MediaType.Photo);

        _mediaScannerMock
            .Setup(s => s.ScanMediaFilesAsync(device, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[] { failingFile }));

        _manifestRepoMock
            .Setup(r => r.IsFileBackedUpAsync(device.Udid, failingFile.FileName, failingFile.FileSize, failingFile.CreationDate))
            .ReturnsAsync(false);

        _manifestRepoMock
            .Setup(r => r.CreateSessionAsync(device.Udid, device.Name))
            .ReturnsAsync(new BackupSession { Id = 2, DeviceUdid = device.Udid, DeviceName = device.Name, StartedAt = DateTime.UtcNow });

        _fileTransferMock
            .Setup(t => t.TransferFileAsync(
                device.Udid,
                failingFile.SourcePath,
                It.IsAny<string>(),
                It.IsAny<IProgress<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransferResult(
                Success: false,
                BytesTransferred: 0,
                ErrorMessage: "Device I/O read failure",
                Duration: TimeSpan.FromMilliseconds(500)));

        // Act
        var session = await _engine.StartBackupAsync(device, settings);

        // Assert
        session.Should().NotBeNull();
        _manifestRepoMock.Verify(r => r.CompleteSessionAsync(
            2,
            BackupStatus.Failed,
            1, // total
            0, // new
            0, // skipped
            1, // failed
            0), Times.Once);
    }

    [Fact]
    public void ResolveAvailableDestinationPath_WhenNamesExist_ShouldUseNextSuffix()
    {
        string original = Path.Combine(_tempDestination, "IMG_0001.HEIC");
        string firstCollision = Path.Combine(_tempDestination, "IMG_0001-1.HEIC");
        File.WriteAllText(original, "original");
        File.WriteAllText(firstCollision, "first collision");

        string result = BackupEngine.ResolveAvailableDestinationPath(original);

        result.Should().Be(Path.Combine(_tempDestination, "IMG_0001-2.HEIC"));
    }
}
