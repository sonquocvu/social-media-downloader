using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.App.ViewModels;

public sealed record QualityOptionViewModel(
    QualityPreset Preset,
    string DisplayName);
