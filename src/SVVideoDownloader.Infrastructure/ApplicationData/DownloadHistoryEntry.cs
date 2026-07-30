using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.Infrastructure.ApplicationData;

public sealed record DownloadHistoryEntry(
    Guid Id,
    string Title,
    SupportedPlatform Platform,
    QualityPreset Quality,
    string FilePath,
    DateTimeOffset CompletedAtUtc,
    DownloadMediaFormat? Format = null);
