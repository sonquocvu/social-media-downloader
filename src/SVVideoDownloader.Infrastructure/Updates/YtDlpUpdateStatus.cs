namespace SVVideoDownloader.Infrastructure.Updates;

public enum YtDlpUpdateStatus
{
    Success,
    DownloadFailed,
    ChecksumUnavailable,
    ChecksumMismatch,
    InvalidDownloadedExecutable,
    ReplacementFailed,
    ValidationFailedAndRolledBack,
    RollbackFailed,
}
