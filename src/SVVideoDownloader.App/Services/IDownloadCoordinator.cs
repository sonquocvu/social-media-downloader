using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Media;

namespace SVVideoDownloader.App.Services;

public interface IDownloadCoordinator
{
    Task<MediaOperationResult<DownloadResult>> DownloadAsync(
        DownloadRequest request,
        string outputFolder,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
