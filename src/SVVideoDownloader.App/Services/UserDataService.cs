using SVVideoDownloader.Infrastructure.ApplicationData;

namespace SVVideoDownloader.App.Services;

public sealed class UserDataService(
    IApplicationSettingsStore settingsStore,
    IDownloadHistoryStore historyStore) : IUserDataService
{
    public Task SaveSettingsAsync(
        ApplicationSettings settings,
        CancellationToken cancellationToken = default) =>
        settingsStore.SaveAsync(settings, cancellationToken);

    public Task<IReadOnlyList<DownloadHistoryEntry>> LoadHistoryAsync(
        CancellationToken cancellationToken = default) =>
        historyStore.LoadAsync(cancellationToken);

    public Task AddHistoryAsync(
        DownloadHistoryEntry entry,
        CancellationToken cancellationToken = default) =>
        historyStore.AddAsync(entry, cancellationToken);

    public Task ClearHistoryAsync(CancellationToken cancellationToken = default) =>
        historyStore.ClearAsync(cancellationToken);
}
