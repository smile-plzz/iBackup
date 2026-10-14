using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using iBackup.Core.Interfaces;
using iBackup.Core.Models;

namespace iBackup.App.ViewModels;

/// <summary>
/// ViewModel for application and backup configuration settings.
/// Manages persistence of backup directory, file organization patterns, media filters, and startup behaviors.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private const string GlobalSettingsKey = "global_default";
    private readonly IManifestRepository _manifestRepository;
    private readonly DispatcherQueue? _dispatcherQueue;
    private bool _isInitialized;

    [ObservableProperty]
    private string _destinationPath = string.Empty;

    [ObservableProperty]
    private FileOrganizationMode _organizationMode = FileOrganizationMode.ByYearMonth;

    [ObservableProperty]
    private int _selectedOrganizationIndex;

    [ObservableProperty]
    private bool _backupPhotos = true;

    [ObservableProperty]
    private bool _backupVideos = true;

    [ObservableProperty]
    private bool _backupRaw = true;

    [ObservableProperty]
    private bool _backupScreenshots = true;

    [ObservableProperty]
    private bool _verifyIntegrity = true;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _autoBackup = true;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private string _appVersion = $"Version {typeof(SettingsViewModel).Assembly.GetName().Version?.ToString(3) ?? "unknown"} · Preview";

    [ObservableProperty]
    private string _gitHubUrl = "https://github.com/smile-plzz/iBackup";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="manifestRepository">The manifest repository for loading and saving settings.</param>
    public SettingsViewModel(IManifestRepository manifestRepository)
    {
        _manifestRepository = manifestRepository ?? throw new ArgumentNullException(nameof(manifestRepository));
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        var defaultPictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        DestinationPath = Path.Combine(defaultPictures, "iBackup");

        _ = LoadSettingsAsync();
    }

    /// <summary>
    /// Loads saved settings asynchronously from repository.
    /// </summary>
    public async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _manifestRepository.GetDeviceSettingsAsync(GlobalSettingsKey);
            if (settings != null)
            {
                RunOnUIThread(() =>
                {
                    if (!string.IsNullOrWhiteSpace(settings.DestinationPath))
                    {
                        DestinationPath = settings.DestinationPath;
                    }

                    OrganizationMode = settings.OrganizationMode;
                    SelectedOrganizationIndex = (int)settings.OrganizationMode;
                    BackupPhotos = settings.IncludePhotos;
                    BackupVideos = settings.IncludeVideos;
                    BackupRaw = settings.IncludeRaw;
                    BackupScreenshots = settings.IncludeScreenshots;
                    VerifyIntegrity = settings.VerifyIntegrity;
                    AutoBackup = settings.AutoBackup;
                    MinimizeToTray = settings.MinimizeToTray;
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load settings: {ex.Message}";
        }
        finally
        {
            _isInitialized = true;
        }
    }

    partial void OnSelectedOrganizationIndexChanged(int value)
    {
        OrganizationMode = value switch
        {
            0 => FileOrganizationMode.ByYearMonth,
            1 => FileOrganizationMode.ByYearMonthDay,
            2 => FileOrganizationMode.Flat,
            _ => FileOrganizationMode.ByYearMonth
        };

        AutoSaveIfInitialized();
    }

    partial void OnBackupPhotosChanged(bool value) => AutoSaveIfInitialized();
    partial void OnBackupVideosChanged(bool value) => AutoSaveIfInitialized();
    partial void OnBackupRawChanged(bool value) => AutoSaveIfInitialized();
    partial void OnBackupScreenshotsChanged(bool value) => AutoSaveIfInitialized();
    partial void OnVerifyIntegrityChanged(bool value) => AutoSaveIfInitialized();
    partial void OnStartWithWindowsChanged(bool value) => AutoSaveIfInitialized();
    partial void OnAutoBackupChanged(bool value) => AutoSaveIfInitialized();
    partial void OnMinimizeToTrayChanged(bool value) => AutoSaveIfInitialized();

    private void AutoSaveIfInitialized()
    {
        if (_isInitialized)
        {
            _ = SaveSettingsAsync();
        }
    }

    /// <summary>
    /// Explicitly saves the current settings to repository.
    /// </summary>
    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        try
        {
            var settings = new BackupSettings
            {
                DestinationPath = DestinationPath,
                OrganizationMode = OrganizationMode,
                DeviceUdid = GlobalSettingsKey,
                IncludePhotos = BackupPhotos,
                IncludeVideos = BackupVideos,
                IncludeRaw = BackupRaw,
                IncludeScreenshots = BackupScreenshots,
                VerifyIntegrity = VerifyIntegrity,
                AutoBackup = AutoBackup,
                MinimizeToTray = MinimizeToTray
            };

            await _manifestRepository.SaveDeviceSettingsAsync(settings);
            StatusMessage = "Settings saved successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving settings: {ex.Message}";
        }
    }

    private void RunOnUIThread(Action action)
    {
        if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
        {
            _dispatcherQueue.TryEnqueue(() => action());
        }
        else
        {
            action();
        }
    }
}
