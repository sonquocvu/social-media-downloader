namespace SVVideoDownloader.Core.Downloads;

public enum DownloadStatus
{
    Pending,
    Analyzing,
    Ready,
    Downloading,
    Processing,
    Completed,
    Failed,
    Cancelled,
}
