using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;
using iBackup.App.Helpers;
using iBackup.App.ViewModels;

namespace iBackup.App.Views;

/// <summary>
/// Main backup view displaying connected device metrics, destination directory, progress reporting, and backup controls.
/// </summary>
public sealed partial class MainPage : Page
{
    /// <summary>
    /// Gets the <see cref="MainViewModel"/> instance backing this view.
    /// </summary>
    public MainViewModel ViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainPage"/> class.
    /// </summary>
    public MainPage()
    {
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        InitializeComponent();
        ViewModel = App.GetService<MainViewModel>();
        ViewModel.RequestBrowseFolder += OnRequestBrowseFolder;
    }

    private async void OnRequestBrowseFolder()
    {
        await PickDestinationFolderAsync();
    }

    private async void BrowseDestinationButton_Click(object sender, RoutedEventArgs e)
    {
        await PickDestinationFolderAsync();
    }

    /// <summary>
    /// Opens the WinUI 3 FolderPicker dialog initialized with the parent window HWND.
    /// </summary>
    private async Task PickDestinationFolderAsync()
    {
        try
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                ViewMode = PickerViewMode.List
            };
            folderPicker.FileTypeFilter.Add("*");

            if (App.MainWindow != null)
            {
                var hwnd = WindowHelper.GetWindowHandle(App.MainWindow);
                InitializeWithWindow.Initialize(folderPicker, hwnd);
            }

            var selectedFolder = await folderPicker.PickSingleFolderAsync();
            if (selectedFolder != null)
            {
                ViewModel.DestinationPath = selectedFolder.Path;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error picking folder: {ex}");
        }
    }
}
