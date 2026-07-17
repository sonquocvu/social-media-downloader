namespace SVVideoDownloader.Infrastructure.Processes;

public enum ProcessStartFailureKind
{
    Missing,
    Inaccessible,
    InvalidExecutable,
    Unknown,
}
