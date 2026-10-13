using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using iBackup.App.Helpers;
using iBackup.App.Views;

namespace iBackup.App;

/// <summary>
/// The main desktop window for iBackup hosting the NavigationView and page Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public ICommand ShowWindowCommand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        ShowWindowCommand = new RelayCommand(() => WindowHelper.ShowWindow(this));
        InitializeComponent();

        Title = "iBackup";
        WindowHelper.SetMinWindowSize(this, 600, 500);
        WindowHelper.SetWindowSize(this, 960, 680);
    }

    /// <summary>
    /// Handles the NavigationView Loaded event to set the initial page.
    /// </summary>
    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        if (NavView.MenuItems.FirstOrDefault() is NavigationViewItem homeItem)
        {
            NavView.SelectedItem = homeItem;
            if (ContentFrame.CurrentSourcePageType != typeof(MainPage))
            {
                ContentFrame.Navigate(typeof(MainPage));
            }
        }
    }

    /// <summary>
    /// Handles navigation selection changes and routes to the appropriate page.
    /// </summary>
    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var tag = args.SelectedItemContainer?.Tag?.ToString();

        switch (tag)
        {
            case "main":
                if (ContentFrame.CurrentSourcePageType != typeof(MainPage))
                {
                    ContentFrame.Navigate(typeof(MainPage));
                }
                break;

            case "history":
                if (ContentFrame.CurrentSourcePageType != typeof(HistoryPage))
                {
                    ContentFrame.Navigate(typeof(HistoryPage));
                }
                break;

            case "settings":
                if (ContentFrame.CurrentSourcePageType != typeof(SettingsPage))
                {
                    ContentFrame.Navigate(typeof(SettingsPage));
                }
                break;
        }
    }

    /// <summary>
    /// Restores the window when clicking on the system tray icon.
    /// </summary>
}
