using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using iBackup.Core.Interfaces;
using iBackup.Core.Models;

namespace iBackup.App.ViewModels;

/// <summary>
/// ViewModel for the backup history view, managing historical backup session logs,
/// session statistics, and list refresh actions.
/// </summary>
public partial class HistoryViewModel : ObservableObject
{
    private readonly IManifestRepository _manifestRepository;
    private readonly DispatcherQueue? _dispatcherQueue;

    [ObservableProperty]
    private ObservableCollection<BackupSession> _backupSessions = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasHistory;

    [ObservableProperty]
    private BackupSession? _selectedSession;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryViewModel"/> class.
    /// </summary>
    /// <param name="manifestRepository">The manifest repository providing session history.</param>
    public HistoryViewModel(IManifestRepository manifestRepository)
    {
        _manifestRepository = manifestRepository ?? throw new ArgumentNullException(nameof(manifestRepository));
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _ = RefreshAsync();
    }

    /// <summary>
    /// Refreshes the backup session history from the database.
    /// </summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;
            var history = await _manifestRepository.GetBackupHistoryAsync();

            RunOnUIThread(() =>
            {
                BackupSessions.Clear();
                if (history != null)
                {
                    foreach (var session in history)
                    {
                        BackupSessions.Add(session);
                    }
                }

                HasHistory = BackupSessions.Count > 0;
            });
        }
        catch (Exception)
        {
            // Set empty state on error
            RunOnUIThread(() =>
            {
                HasHistory = BackupSessions.Count > 0;
            });
        }
        finally
        {
            RunOnUIThread(() =>
            {
                IsLoading = false;
            });
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
