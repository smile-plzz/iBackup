using Microsoft.UI.Xaml.Controls;
using iBackup.App.ViewModels;

namespace iBackup.App.Views;

/// <summary>
/// Settings view allowing customization of destination directories, media format filters,
/// file layout schemes, integrity checking, and auto-start preferences.
/// </summary>
public sealed partial class SettingsPage : Page
{
    /// <summary>
    /// Gets the <see cref="SettingsViewModel"/> instance backing this view.
    /// </summary>
    public SettingsViewModel ViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPage"/> class.
    /// </summary>
    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<SettingsViewModel>();
    }
}
