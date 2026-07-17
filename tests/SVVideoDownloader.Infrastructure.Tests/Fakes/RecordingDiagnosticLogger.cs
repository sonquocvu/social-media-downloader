using SVVideoDownloader.Infrastructure.Diagnostics;

namespace SVVideoDownloader.Infrastructure.Tests.Fakes;

internal sealed class RecordingDiagnosticLogger : IDiagnosticLogger
{
    public List<string> Messages { get; } = [];

    public Task LogAsync(
        DiagnosticLogLevel level,
        string message,
        CancellationToken cancellationToken = default)
    {
        Messages.Add($"{level}:{message}");
        return Task.CompletedTask;
    }
}
