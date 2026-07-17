namespace SVVideoDownloader.Infrastructure.Updates;

public enum FfmpegUpdateStatus
{
    Success,
    DownloadFailed,
    ChecksumUnavailable,
    ChecksumMismatch,
    InvalidArchive,
    InvalidDownloadedExecutables,
    ReplacementFailed,
    ValidationFailedAndRolledBack,
    RollbackFailed,
}
