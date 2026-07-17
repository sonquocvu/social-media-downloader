using System.Threading;
using System.Threading.Tasks;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.Core.Media;

public interface IVideoMetadataProvider
{
    Task<MediaOperationResult<VideoInfo>> GetVideoInfoAsync(
        VideoSource source,
        CancellationToken cancellationToken = default);
}
