using System;

namespace SVVideoDownloader.Infrastructure.Processes;

public sealed class ExternalProcessStartException : Exception
{
    public ExternalProcessStartException(
        ProcessStartFailureKind kind,
        Exception? innerException = null)
        : base(null, innerException)
    {
        Kind = kind;
    }

    public ProcessStartFailureKind Kind { get; }
}

public sealed class ExternalProcessTimeoutException : TimeoutException
{
}
