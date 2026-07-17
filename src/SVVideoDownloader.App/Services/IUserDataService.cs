using SVVideoDownloader.Infrastructure.ApplicationData;

namespace SVVideoDownloader.App.Services;

public interface IUserDataService
{
    Task SaveSettingsAsync(
        ApplicationSettings settings,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DownloadHistoryEntry>> LoadHistoryAsync(
        CancellationToken cancellationToken = default);

    Task AddHistoryAsync(
        DownloadHistoryEntry entry,
        CancellationToken cancellationToken = default);

    Task ClearHistoryAsync(CancellationToken cancellationToken = default);
}
