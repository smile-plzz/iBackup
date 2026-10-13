using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;
using iBackup.App.ViewModels;
using iBackup.Core.Data;
using iBackup.Core.Interfaces;
using iBackup.Core.Services;

namespace iBackup.App;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// Manages the application lifecycle, dependency injection host, logging, and top-level exception handling.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    /// <summary>
    /// Gets the main application window instance.
    /// </summary>
    public static MainWindow? MainWindow { get; private set; }

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        InitializeLogging();
        InitializeComponent();

        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    /// <summary>
    /// Configures Serilog to write rolling daily logs to %LOCALAPPDATA%\iBackup\logs\ibackup-.log.
    /// </summary>
    private static void InitializeLogging()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDirectory = Path.Combine(localAppData, "iBackup", "logs");
            Directory.CreateDirectory(logDirectory);

            var logFilePath = Path.Combine(logDirectory, "ibackup-.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: logFilePath,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("iBackup logging initialized successfully at {LogPath}", logFilePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize Serilog: {ex}");
        }
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        try
        {
            _host = CreateHostBuilder().Build();
            await _host.StartAsync();

            // Initialize manifest database on startup
            try
            {
                var manifestRepository = _host.Services.GetService<IManifestRepository>();
                if (manifestRepository != null)
                {
                    await manifestRepository.InitializeAsync();
                    Log.Information("Manifest database initialized successfully.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize ManifestDatabase on startup.");
            }

            // Start device monitor service in background
            try
            {
                var deviceMonitor = _host.Services.GetService<IDeviceMonitor>();
                if (deviceMonitor != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await deviceMonitor.StartMonitoringAsync();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error occurred while monitoring devices.");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to start IDeviceMonitor.");
            }

            // Create and activate MainWindow
            MainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow.Activate();

            Log.Information("iBackup MainWindow activated.");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Critical error during application launch.");
            throw;
        }
    }

    /// <summary>
    /// Configures the application host and service dependencies for dependency injection.
    /// </summary>
    /// <returns>A configured <see cref="IHostBuilder"/>.</returns>
    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, services) =>
            {
                // Core Data
                services.AddSingleton<IManifestRepository, ManifestRepository>();

                // Core Services
                services.AddSingleton<IDeviceMonitor, DeviceMonitorService>();
                services.AddSingleton<IMediaScanner, MediaScannerService>();
                services.AddSingleton<IBackupEngine, BackupEngine>();
                services.AddSingleton<IFileTransferService, FileTransferService>();

                // ViewModels (transient)
                services.AddSingleton<MainViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<HistoryViewModel>();

                // MainWindow
                services.AddSingleton<MainWindow>();
            });
    }

    /// <summary>
    /// Resolves a registered service instance from the DI container.
    /// </summary>
    /// <typeparam name="T">The type of service to resolve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the service cannot be resolved.</exception>
    public static T GetService<T>() where T : class
    {
        if ((Current as App)?._host?.Services.GetService(typeof(T)) is not T service)
        {
            throw new InvalidOperationException($"Service of type {typeof(T).FullName} not found in dependency injection container.");
        }

        return service;
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled WinUI XAML exception: {Message}", e.Message);
        // Prevent application crash if exception is recoverable
        e.Handled = true;
    }

    private void OnCurrentDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Log.Fatal(ex, "Unhandled AppDomain exception (isTerminating: {IsTerminating})", e.IsTerminating);
        }
        else
        {
            Log.Fatal("Unhandled AppDomain non-exception object: {Object}", e.ExceptionObject);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unobserved task exception encountered.");
        e.SetObserved();
    }
}
