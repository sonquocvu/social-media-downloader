using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Videos;
using SVVideoDownloader.Infrastructure.ApplicationData;

namespace SVVideoDownloader.App.Services;

public sealed record AppUiOptions(
    string DefaultOutputFolder,
    QualityPreset DefaultQuality,
    ApplicationTheme DefaultTheme = ApplicationTheme.Light,
    DownloadMediaFormat DefaultFormat = DownloadMediaFormat.VideoMp4);
