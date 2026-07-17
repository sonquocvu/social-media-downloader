using System.Windows;
using SVVideoDownloader.Infrastructure.ApplicationData;

namespace SVVideoDownloader.App.Services;

public sealed class WpfThemeService(Application application) : IThemeService
{
    private readonly Application _application = application ??
        throw new ArgumentNullException(nameof(application));

    public ApplicationTheme CurrentTheme { get; private set; } = ApplicationTheme.Light;

    public void Apply(ApplicationTheme theme)
    {
        if (theme is not ApplicationTheme.Light and not ApplicationTheme.Dark)
        {
            throw new ArgumentOutOfRangeException(nameof(theme));
        }

        var source = theme == ApplicationTheme.Dark
            ? new Uri(
                "/SVVideoDownloader.App;component/Themes/DarkTheme.xaml",
                UriKind.Relative)
            : new Uri(
                "/SVVideoDownloader.App;component/Themes/LightTheme.xaml",
                UriKind.Relative);
        var palette = new ResourceDictionary { Source = source };
        var dictionaries = _application.Resources.MergedDictionaries;
        if (dictionaries.Count == 0)
        {
            dictionaries.Add(palette);
        }
        else
        {
            dictionaries[0] = palette;
        }

        CurrentTheme = theme;
    }
}
