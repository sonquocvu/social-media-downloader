namespace SVVideoDownloader.Core.Media;

public enum MediaErrorCategory
{
    InvalidRequest,
    DependencyMissing,
    DependencyInaccessible,
    DependencyInvalid,
    SourceUnavailable,
    InvalidResponse,
    TimedOut,
    ExecutionFailed,
}
