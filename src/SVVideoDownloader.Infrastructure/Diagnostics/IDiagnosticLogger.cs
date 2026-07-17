namespace SVVideoDownloader.Infrastructure.Diagnostics;

public interface IDiagnosticLogger
{
    Task LogAsync(
        DiagnosticLogLevel level,
        string message,
        CancellationToken cancellationToken = default);
}
