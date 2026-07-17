namespace SVVideoDownloader.Infrastructure.ApplicationData;

public interface IDownloadHistoryStore
{
    Task<IReadOnlyList<DownloadHistoryEntry>> LoadAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        DownloadHistoryEntry entry,
        CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
