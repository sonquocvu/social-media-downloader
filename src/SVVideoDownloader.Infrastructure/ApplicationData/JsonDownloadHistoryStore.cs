using System.Text.Json;
using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Infrastructure.Diagnostics;

namespace SVVideoDownloader.Infrastructure.ApplicationData;

public sealed class JsonDownloadHistoryStore(
    string historyFilePath,
    IDiagnosticLogger logger,
    int maximumEntries = 500) : IDownloadHistoryStore, IDisposable
{
    private readonly string _historyFilePath = Path.GetFullPath(historyFilePath);
    private readonly IDiagnosticLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly int _maximumEntries = maximumEntries > 0
        ? maximumEntries
        : throw new ArgumentOutOfRangeException(nameof(maximumEntries));
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IReadOnlyList<DownloadHistoryEntry>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(LoadCore, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            await _logger.LogAsync(
                    DiagnosticLogLevel.Warning,
                    $"Không thể đọc lịch sử tải xuống. {exception.GetType().Name}",
                    cancellationToken)
                .ConfigureAwait(false);
            return Array.Empty<DownloadHistoryEntry>();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddAsync(
        DownloadHistoryEntry entry,
        CancellationToken cancellationToken = default)
    {
        Validate(entry);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await Task.Run(
                    () =>
                    {
                        List<DownloadHistoryEntry> entries;
                        try
                        {
                            entries = LoadCore().ToList();
                        }
                        catch (Exception exception) when (exception is JsonException or NotSupportedException)
                        {
                            entries = [];
                        }
                        entries.RemoveAll(existing => existing.Id == entry.Id);
                        entries.Insert(0, entry);
                        if (entries.Count > _maximumEntries)
                        {
                            entries.RemoveRange(_maximumEntries, entries.Count - _maximumEntries);
                        }

                        JsonStoreHelpers.WriteAtomic(_historyFilePath, entries);
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await Task.Run(
                    () => JsonStoreHelpers.WriteAtomic(
                        _historyFilePath,
                        Array.Empty<DownloadHistoryEntry>()),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    private IReadOnlyList<DownloadHistoryEntry> LoadCore()
    {
        if (!File.Exists(_historyFilePath))
        {
            return Array.Empty<DownloadHistoryEntry>();
        }

        var json = File.ReadAllText(_historyFilePath);
        var entries = JsonSerializer.Deserialize<List<DownloadHistoryEntry>>(
            json,
            JsonStoreHelpers.SerializerOptions);
        return entries is null
            ? Array.Empty<DownloadHistoryEntry>()
            : entries.Where(IsValid).Take(_maximumEntries).ToArray();
    }

    private static void Validate(DownloadHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (!IsValid(entry))
        {
            throw new ArgumentException(null, nameof(entry));
        }
    }

    private static bool IsValid(DownloadHistoryEntry entry) =>
        entry.Id != Guid.Empty &&
        !string.IsNullOrWhiteSpace(entry.Title) &&
        !string.IsNullOrWhiteSpace(entry.FilePath) &&
        Path.IsPathFullyQualified(entry.FilePath) &&
        Enum.IsDefined(entry.Platform) &&
        Enum.IsDefined(entry.Quality) &&
        (entry.Format is null ||
            Enum.IsDefined(entry.Format.Value) &&
            entry.Format.Value.IsCompatibleWith(entry.Quality)) &&
        entry.CompletedAtUtc != default;

    private static bool IsStorageException(Exception exception) =>
        exception is IOException or
            UnauthorizedAccessException or
            JsonException or
            NotSupportedException;
}
