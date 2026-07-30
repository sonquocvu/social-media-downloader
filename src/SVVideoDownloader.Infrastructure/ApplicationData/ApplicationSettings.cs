using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.Infrastructure.ApplicationData;

public sealed record ApplicationSettings(
    string DownloadDirectory,
    QualityPreset DefaultQuality,
    ApplicationTheme Theme = ApplicationTheme.Light,
    DownloadMediaFormat? DefaultFormat = null);
