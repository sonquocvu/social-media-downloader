namespace SVVideoDownloader.Infrastructure.Updates;

public interface IFfmpegUpdateService
{
    Task<FfmpegUpdateResult> UpdateAsync(
        CancellationToken cancellationToken = default);
}
