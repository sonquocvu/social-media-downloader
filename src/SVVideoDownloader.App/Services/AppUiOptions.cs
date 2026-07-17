using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.App.Services;

public sealed record AppUiOptions(
    string DefaultOutputFolder,
    QualityPreset DefaultQuality);
