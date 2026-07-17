namespace SVVideoDownloader.Infrastructure.Processes;

public sealed record ProcessRunResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
