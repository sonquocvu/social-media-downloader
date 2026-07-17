using System.Globalization;
using System.Text;

namespace SVVideoDownloader.Infrastructure.Diagnostics;

public sealed class RotatingDiagnosticLogger : IDiagnosticLogger, IDisposable
{
    private readonly string _logFilePath;
    private readonly long _maximumFileBytes;
    private readonly int _retainedFileCount;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public RotatingDiagnosticLogger(
        string logFilePath,
        long maximumFileBytes = 1_048_576,
        int retainedFileCount = 5)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);
        if (maximumFileBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumFileBytes));
        }

        if (retainedFileCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(retainedFileCount));
        }

        _logFilePath = Path.GetFullPath(logFilePath);
        _maximumFileBytes = maximumFileBytes;
        _retainedFileCount = retainedFileCount;
    }

    public async Task LogAsync(
        DiagnosticLogLevel level,
        string message,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(message);
        var safeMessage = SecretRedactor.Redact(message)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
        var maximumMessageCharacters = (int)Math.Min(
            64 * 1024,
            Math.Max(1, _maximumFileBytes / 4));
        if (safeMessage.Length > maximumMessageCharacters)
        {
            safeMessage = safeMessage[..maximumMessageCharacters] + "…";
        }

        var line = string.Create(
            CultureInfo.InvariantCulture,
            $"{DateTimeOffset.UtcNow:O} [{level}] {safeMessage}{Environment.NewLine}");

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await Task.Run(
                    () => WriteAndRotate(line),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    private void WriteAndRotate(string line)
    {
        var directory = Path.GetDirectoryName(_logFilePath)!;
        Directory.CreateDirectory(directory);
        var bytesToAppend = Encoding.UTF8.GetByteCount(line);
        if (File.Exists(_logFilePath) &&
            new FileInfo(_logFilePath).Length + bytesToAppend > _maximumFileBytes)
        {
            RotateFiles();
        }

        File.AppendAllText(_logFilePath, line, new UTF8Encoding(false));
    }

    private void RotateFiles()
    {
        if (_retainedFileCount == 1)
        {
            File.Delete(_logFilePath);
            return;
        }

        for (var index = _retainedFileCount - 1; index >= 1; index--)
        {
            var source = GetArchivePath(index);
            if (!File.Exists(source))
            {
                continue;
            }

            if (index == _retainedFileCount - 1)
            {
                File.Delete(source);
            }
            else
            {
                File.Move(source, GetArchivePath(index + 1), overwrite: true);
            }
        }

        File.Move(_logFilePath, GetArchivePath(1), overwrite: true);
    }

    private string GetArchivePath(int index) => $"{_logFilePath}.{index}";
}
