using System;
using System.Threading;
using System.Threading.Tasks;
using SVVideoDownloader.Core.Downloads;

namespace SVVideoDownloader.Core.Media;

public interface IVideoDownloadService
{
    Task<MediaOperationResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
