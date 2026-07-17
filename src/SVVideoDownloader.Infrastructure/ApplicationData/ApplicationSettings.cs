using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.Infrastructure.ApplicationData;

public sealed record ApplicationSettings(
    string DownloadDirectory,
    QualityPreset DefaultQuality);
