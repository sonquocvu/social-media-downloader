namespace SVVideoDownloader.Infrastructure.ApplicationData;

public interface IApplicationSettingsStore
{
    Task<ApplicationSettings> LoadAsync(
        ApplicationSettings defaults,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        ApplicationSettings settings,
        CancellationToken cancellationToken = default);
}
