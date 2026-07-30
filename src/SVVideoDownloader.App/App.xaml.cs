using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using SVVideoDownloader.App.Services;
using SVVideoDownloader.App.ViewModels;
using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Core.Videos;
using SVVideoDownloader.Infrastructure.ApplicationData;
using SVVideoDownloader.Infrastructure.Diagnostics;
using SVVideoDownloader.Infrastructure.ExternalTools;
using SVVideoDownloader.Infrastructure.Processes;
using SVVideoDownloader.Infrastructure.Updates;
using SVVideoDownloader.Infrastructure.YtDlp;

namespace SVVideoDownloader.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private RotatingDiagnosticLogger? _logger;
    private JsonApplicationSettingsStore? _settingsStore;
    private JsonDownloadHistoryStore? _historyStore;
    private HttpClient? _httpClient;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var paths = ApplicationDataPaths.CreateDefault();
            _logger = new RotatingDiagnosticLogger(paths.LogFilePath);
            _settingsStore = new JsonApplicationSettingsStore(paths.SettingsFilePath, _logger);
            _historyStore = new JsonDownloadHistoryStore(paths.HistoryFilePath, _logger);
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10),
            };

            var defaultOutputFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
            var defaults = new ApplicationSettings(
                defaultOutputFolder,
                QualityPreset.Best,
                ApplicationTheme.Light,
                DownloadMediaFormat.VideoMp4);
            var settings = await _settingsStore.LoadAsync(defaults);
            var themeService = new WpfThemeService(this);
            themeService.Apply(settings.Theme);
            var externalToolOptions = ExternalToolOptions.CreateForToolsDirectory(
                paths.ToolsDirectory,
                settings.DownloadDirectory);

            var services = new ServiceCollection();
            services.AddSingleton(paths);
            services.AddSingleton(new AppUiOptions(
                settings.DownloadDirectory,
                settings.DefaultQuality,
                settings.Theme,
                settings.DefaultFormat!.Value));
            services.AddSingleton<IThemeService>(themeService);
            services.AddSingleton<IApplicationSettingsStore>(_settingsStore);
            services.AddSingleton<IDownloadHistoryStore>(_historyStore);
            services.AddSingleton<IDiagnosticLogger>(_logger);
            services.AddSingleton(externalToolOptions);
            services.AddSingleton(_httpClient);
            services.AddSingleton<IProcessRunner, SystemProcessRunner>();
            services.AddSingleton<IEngineOperationGate, EngineOperationGate>();
            services.AddSingleton<YtDlpMediaService>();
            services.AddSingleton<IVideoMetadataProvider, MetadataCoordinator>();
            services.AddSingleton<IDownloadCoordinator, DownloadCoordinator>();
            services.AddSingleton<IFolderPickerService, WindowsFolderPickerService>();
            services.AddSingleton<IFileActionService, WindowsFileActionService>();
            services.AddSingleton<IUiDispatcher, WpfUiDispatcher>();
            services.AddSingleton<IUserDataService, UserDataService>();
            services.AddSingleton<IFileDownloadClient, HttpFileDownloadClient>();
            services.AddSingleton<IExternalToolStatusService, ExternalToolStatusService>();
            services.AddSingleton<IYtDlpUpdateService, YtDlpUpdateService>();
            services.AddSingleton<IFfmpegUpdateService, FfmpegUpdateService>();
            services.AddSingleton<IToolManagementService, ToolManagementService>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            await _logger.LogAsync(
                DiagnosticLogLevel.Information,
                "Khởi động SVVideoDownloader.");
            var window = _serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.Show();
            await window.ViewModel.InitializeAsync();
        }
        catch (Exception exception)
        {
            if (_logger is not null)
            {
                try
                {
                    await _logger.LogAsync(
                        DiagnosticLogLevel.Error,
                        $"Khởi động ứng dụng thất bại. {exception}");
                }
                catch (Exception)
                {
                }
            }

            MessageBox.Show(
                "Không thể khởi động ứng dụng. Hãy xem nhật ký chẩn đoán trong dữ liệu cục bộ của SVVideoDownloader.",
                "Lỗi khởi động",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

        _serviceProvider?.Dispose();
        _serviceProvider = null;
        _settingsStore?.Dispose();
        _settingsStore = null;
        _historyStore?.Dispose();
        _historyStore = null;
        _httpClient?.Dispose();
        _httpClient = null;
        _logger?.Dispose();
        _logger = null;
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e) =>
        QueueExceptionLog("Lỗi giao diện chưa được xử lý.", e.Exception);

    private void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e) =>
        QueueExceptionLog("Lỗi tác vụ nền chưa được quan sát.", e.Exception);

    private void QueueExceptionLog(string context, Exception exception)
    {
        var logger = _logger;
        if (logger is not null)
        {
            _ = LogExceptionSafelyAsync(logger, context, exception);
        }
    }

    private static async Task LogExceptionSafelyAsync(
        IDiagnosticLogger logger,
        string context,
        Exception exception)
    {
        try
        {
            await logger.LogAsync(
                DiagnosticLogLevel.Error,
                $"{context} {exception}");
        }
        catch (Exception)
        {
        }
    }
}
