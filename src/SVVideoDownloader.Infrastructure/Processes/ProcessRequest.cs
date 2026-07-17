using System;
using System.Collections.Generic;
using System.Linq;

namespace SVVideoDownloader.Infrastructure.Processes;

public sealed class ProcessRequest
{
    public const int DefaultMaximumCapturedCharacters = 4 * 1024 * 1024;

    public ProcessRequest(
        string executablePath,
        IEnumerable<string> argumentList,
        TimeSpan? timeout = null,
        int maximumCapturedCharacters = DefaultMaximumCapturedCharacters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(argumentList);

        if (timeout is { } knownTimeout && knownTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        if (maximumCapturedCharacters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCapturedCharacters));
        }

        ExecutablePath = executablePath.Trim();
        ArgumentList = Array.AsReadOnly(argumentList.ToArray());
        Timeout = timeout;
        MaximumCapturedCharacters = maximumCapturedCharacters;
    }

    public string ExecutablePath { get; }

    public IReadOnlyList<string> ArgumentList { get; }

    public TimeSpan? Timeout { get; }

    public int MaximumCapturedCharacters { get; }
}
