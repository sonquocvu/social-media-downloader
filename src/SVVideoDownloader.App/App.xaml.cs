using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SVVideoDownloader.App.Services;
using SVVideoDownloader.App.ViewModels;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Infrastructure.ExternalTools;
using SVVideoDownloader.Infrastructure.Processes;
using SVVideoDownloader.Infrastructure.YtDlp;

namespace SVVideoDownloader.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var defaultOutputFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
        var services = new ServiceCollection();
        services.AddSingleton(new AppUiOptions(defaultOutputFolder));
        services.AddSingleton(ExternalToolOptions.CreateDefault(defaultOutputFolder));
        services.AddSingleton<IProcessRunner, SystemProcessRunner>();
        services.AddSingleton<YtDlpMediaService>();
        services.AddSingleton<IVideoMetadataProvider>(provider =>
            provider.GetRequiredService<YtDlpMediaService>());
        services.AddSingleton<IDownloadCoordinator, DownloadCoordinator>();
        services.AddSingleton<IFolderPickerService, WindowsFolderPickerService>();
        services.AddSingleton<IFileActionService, WindowsFileActionService>();
        services.AddSingleton<IUiDispatcher, WpfUiDispatcher>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();
        var window = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        _serviceProvider = null;
        base.OnExit(e);
    }
}
