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
    public void ReadOnlyViewModelBindings_AreOneWay()
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
                var applicationLogo = Assert.IsType<Image>(window.FindName("ApplicationLogo"));
                var themeBinding = BindingOperations.GetBinding(
                    themeButton,
                    Button.CommandProperty);
                var downloadQueue = Assert.IsType<ListBox>(window.FindName("DownloadQueueList"));
                var queueItem = Assert.IsAssignableFrom<FrameworkElement>(
                    downloadQueue.ItemTemplate.LoadContent());
                var progressBar = Assert.Single(FindDescendants<ProgressBar>(queueItem));
                var progressValueBinding = BindingOperations.GetBinding(
                    progressBar,
                    ProgressBar.ValueProperty);
                var indeterminateBinding = BindingOperations.GetBinding(
                    progressBar,
                    ProgressBar.IsIndeterminateProperty);
                var settingsScrollViewer = Assert.IsType<ScrollViewer>(
                    window.FindName("SettingsScrollViewer"));
                var updateActionsPanel = Assert.IsType<StackPanel>(
                    window.FindName("ToolUpdateActionsPanel"));
                var ytDlpUpdatePanel = Assert.IsType<Border>(
                    window.FindName("YtDlpUpdatePanel"));
                var ffmpegUpdatePanel = Assert.IsType<Border>(
                    window.FindName("FfmpegUpdatePanel"));

                Assert.NotNull(binding);
                Assert.Equal(BindingMode.OneWay, binding.Mode);
                Assert.NotNull(themeBinding);
                Assert.Equal("ToggleThemeCommand", themeBinding.Path.Path);
                Assert.EndsWith(
                    "Assets/SVVideoDownloader.png",
                    applicationLogo.Source.ToString(),
                    StringComparison.Ordinal);
                Assert.NotNull(progressValueBinding);
                Assert.Equal("PercentageValue", progressValueBinding.Path.Path);
                Assert.Equal(BindingMode.OneWay, progressValueBinding.Mode);
                Assert.NotNull(indeterminateBinding);
                Assert.Equal("IsProgressIndeterminate", indeterminateBinding.Path.Path);
                Assert.Equal(BindingMode.OneWay, indeterminateBinding.Mode);
                Assert.Equal(
                    ScrollBarVisibility.Disabled,
                    settingsScrollViewer.HorizontalScrollBarVisibility);
                Assert.Equal(Orientation.Vertical, updateActionsPanel.Orientation);
                Assert.Equal(
                    new UIElement[] { ytDlpUpdatePanel, ffmpegUpdatePanel },
                    updateActionsPanel.Children.Cast<UIElement>());

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

    private static IEnumerable<T> FindDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindDescendants<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
