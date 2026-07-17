using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using SVVideoDownloader.App;
using SVVideoDownloader.App.Services;
using SVVideoDownloader.Infrastructure.ApplicationData;

namespace SVVideoDownloader.App.Tests;

public sealed class MainWindowXamlTests
{
    [Fact]
    public void OutputFolderBinding_IsOneWayForReadOnlyViewModelProperty()
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                var application = Application.Current as App ?? new App();
                application.InitializeComponent();
                using var viewModel = TestData.CreateMainViewModel();
                var window = new MainWindow(viewModel);
                var textBox = Assert.IsType<TextBox>(window.FindName("OutputFolderInput"));
                var binding = BindingOperations.GetBinding(textBox, TextBox.TextProperty);
                var themeButton = Assert.IsType<Button>(window.FindName("ThemeToggleButton"));
                var themeBinding = BindingOperations.GetBinding(
                    themeButton,
                    Button.CommandProperty);

                Assert.NotNull(binding);
                Assert.Equal(BindingMode.OneWay, binding.Mode);
                Assert.NotNull(themeBinding);
                Assert.Equal("ToggleThemeCommand", themeBinding.Path.Path);

                var themeService = new WpfThemeService(application);
                themeService.Apply(ApplicationTheme.Dark);
                var background = Assert.IsType<SolidColorBrush>(
                    application.Resources["AppBackgroundBrush"]);
                Assert.Equal(ApplicationTheme.Dark, themeService.CurrentTheme);
                Assert.Equal(Color.FromRgb(0x0D, 0x11, 0x1A), background.Color);
            }
            catch (Exception caught)
            {
                exception = caught;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(exception);
    }
}
