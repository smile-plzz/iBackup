using Microsoft.UI.Xaml.Controls;
using iBackup.App.ViewModels;

namespace iBackup.App.Views;

/// <summary>
/// History view presenting a chronological record of executed backup sessions and their status metrics.
/// </summary>
public sealed partial class HistoryPage : Page
{
    /// <summary>
    /// Gets the <see cref="HistoryViewModel"/> instance backing this view.
    /// </summary>
    public HistoryViewModel ViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryPage"/> class.
    /// </summary>
    public HistoryPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<HistoryViewModel>();
    }
}
