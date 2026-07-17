using SVVideoDownloader.Infrastructure.ApplicationData;

namespace SVVideoDownloader.App.Services;

public interface IThemeService
{
    ApplicationTheme CurrentTheme { get; }

    void Apply(ApplicationTheme theme);
}
