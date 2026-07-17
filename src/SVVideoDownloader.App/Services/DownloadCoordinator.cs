using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Infrastructure.ExternalTools;
using SVVideoDownloader.Infrastructure.Processes;
using SVVideoDownloader.Infrastructure.YtDlp;

namespace SVVideoDownloader.App.Services;

public sealed class DownloadCoordinator(
    ExternalToolOptions baseOptions,
    IProcessRunner processRunner) : IDownloadCoordinator
{
    public Task<MediaOperationResult<DownloadResult>> DownloadAsync(
        DownloadRequest request,
        string outputFolder,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var options = new ExternalToolOptions(
            baseOptions.YtDlpPath,
            baseOptions.FfmpegPath,
            baseOptions.FfprobePath,
            outputFolder,
            baseOptions.MetadataTimeout,
            baseOptions.DownloadTimeout);
        var service = new YtDlpMediaService(options, processRunner);
        return service.DownloadAsync(request, progress, cancellationToken);
    }
}
