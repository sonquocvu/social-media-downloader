namespace SVVideoDownloader.Infrastructure.Updates;

public interface IYtDlpUpdateService
{
    Task<YtDlpUpdateResult> UpdateAsync(
        CancellationToken cancellationToken = default);
}
