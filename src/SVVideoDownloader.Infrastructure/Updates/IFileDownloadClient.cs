namespace SVVideoDownloader.Infrastructure.Updates;

public interface IFileDownloadClient
{
    Task DownloadAsync(
        Uri source,
        string destinationPath,
        long maximumBytes,
        CancellationToken cancellationToken = default);
}
