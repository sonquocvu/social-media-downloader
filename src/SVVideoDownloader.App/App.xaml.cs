using System.Windows;
using SVVideoDownloader.App.ViewModels;

namespace SVVideoDownloader.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new MainWindow
        {
            DataContext = new MainWindowViewModel(),
        };

        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
