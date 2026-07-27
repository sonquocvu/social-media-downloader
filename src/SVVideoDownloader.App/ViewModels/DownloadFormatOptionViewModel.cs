namespace SVVideoDownloader.App.ViewModels;

public enum DownloadFormat
{
    Video,
    Mp3,
}

public sealed record DownloadFormatOptionViewModel(
    DownloadFormat Format,
    string DisplayName);
