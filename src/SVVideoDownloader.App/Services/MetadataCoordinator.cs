using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Core.Videos;
using SVVideoDownloader.Infrastructure.YtDlp;

namespace SVVideoDownloader.App.Services;

public sealed class MetadataCoordinator(
    YtDlpMediaService mediaService,
    IEngineOperationGate operationGate) : IVideoMetadataProvider
{
    public async Task<MediaOperationResult<VideoInfo>> GetVideoInfoAsync(
        VideoSource source,
        CancellationToken cancellationToken = default)
    {
        using var operation = operationGate.TryEnterMetadataOperation();
        if (operation is null)
        {
            return MediaOperationResult<VideoInfo>.Failure(
                new MediaOperationError(
                    MediaErrorCategory.ExecutionFailed,
                    MediaComponent.MetadataExtractor));
        }

        return await mediaService.GetVideoInfoAsync(source, cancellationToken);
    }
}
