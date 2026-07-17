namespace SVVideoDownloader.Infrastructure.Updates;

public sealed record YtDlpUpdateResult(
    YtDlpUpdateStatus Status,
    string? InstalledVersion = null)
{
    public bool IsSuccess => Status == YtDlpUpdateStatus.Success;
}
