using SVVideoDownloader.Core.Downloads;

namespace SVVideoDownloader.App.ViewModels;

public sealed record DownloadFormatOptionViewModel(
    DownloadMediaFormat Format,
    string DisplayName);
