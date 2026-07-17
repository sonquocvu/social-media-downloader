using System.Net.Http;
using System.Security.Cryptography;
using SVVideoDownloader.Infrastructure.Diagnostics;
using SVVideoDownloader.Infrastructure.ExternalTools;

namespace SVVideoDownloader.Infrastructure.Updates;

public sealed class YtDlpUpdateService : IYtDlpUpdateService
{
    private const long MaximumExecutableBytes = 100L * 1024 * 1024;
    private const long MaximumChecksumBytes = 1024 * 1024;
    private static readonly Uri ExecutableUri = new(
        "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe");
    private static readonly Uri ChecksumsUri = new(
        "https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS");

    private readonly string _targetPath;
    private readonly IFileDownloadClient _downloadClient;
    private readonly IExternalToolStatusService _toolStatusService;
    private readonly IDiagnosticLogger _logger;

    public YtDlpUpdateService(
        ExternalToolOptions options,
        IFileDownloadClient downloadClient,
        IExternalToolStatusService toolStatusService,
        IDiagnosticLogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _targetPath = options.YtDlpPath;
        _downloadClient = downloadClient ?? throw new ArgumentNullException(nameof(downloadClient));
        _toolStatusService = toolStatusService ??
            throw new ArgumentNullException(nameof(toolStatusService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<YtDlpUpdateResult> UpdateAsync(
        CancellationToken cancellationToken = default)
    {
        var targetDirectory = Path.GetDirectoryName(_targetPath)!;
        var operationId = Guid.NewGuid().ToString("N");
        var downloadedPath = Path.Combine(targetDirectory, $".yt-dlp.{operationId}.download");
        var checksumPath = Path.Combine(targetDirectory, $".yt-dlp.{operationId}.sha256");
        var backupPath = Path.Combine(targetDirectory, $".yt-dlp.{operationId}.backup");
        var replacementStarted = false;
        var originalExisted = false;
        var preserveBackup = false;

        try
        {
            await Task.Run(
                    () => Directory.CreateDirectory(targetDirectory),
                    cancellationToken)
                .ConfigureAwait(false);
            await _logger.LogAsync(
                    DiagnosticLogLevel.Information,
                    "Bắt đầu cập nhật yt-dlp thủ công.",
                    cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await _downloadClient.DownloadAsync(
                        ChecksumsUri,
                        checksumPath,
                        MaximumChecksumBytes,
                        cancellationToken)
                    .ConfigureAwait(false);
                await _downloadClient.DownloadAsync(
                        ExecutableUri,
                        downloadedPath,
                        MaximumExecutableBytes,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (IsDownloadException(exception))
            {
                await LogFailureAsync("Tải bản cập nhật yt-dlp thất bại.", cancellationToken)
                    .ConfigureAwait(false);
                return new YtDlpUpdateResult(YtDlpUpdateStatus.DownloadFailed);
            }

            var expectedChecksum = await Task.Run(
                    () => ReadExpectedChecksum(checksumPath),
                    cancellationToken)
                .ConfigureAwait(false);
            if (expectedChecksum is null)
            {
                return new YtDlpUpdateResult(YtDlpUpdateStatus.ChecksumUnavailable);
            }

            var actualChecksum = await CalculateSha256Async(downloadedPath, cancellationToken)
                .ConfigureAwait(false);
            if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(expectedChecksum),
                    Convert.FromHexString(actualChecksum)))
            {
                await LogFailureAsync("Checksum yt-dlp không khớp.", cancellationToken)
                    .ConfigureAwait(false);
                return new YtDlpUpdateResult(YtDlpUpdateStatus.ChecksumMismatch);
            }

            var downloadedStatus = await _toolStatusService.CheckYtDlpAsync(
                    downloadedPath,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!downloadedStatus.IsAvailable)
            {
                return new YtDlpUpdateResult(YtDlpUpdateStatus.InvalidDownloadedExecutable);
            }

            try
            {
                originalExisted = await Task.Run(
                        () => ReplaceAtomically(downloadedPath, _targetPath, backupPath),
                        cancellationToken)
                    .ConfigureAwait(false);
                replacementStarted = true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                await LogFailureAsync("Không thể thay thế yt-dlp.", cancellationToken)
                    .ConfigureAwait(false);
                return new YtDlpUpdateResult(YtDlpUpdateStatus.ReplacementFailed);
            }

            var installedStatus = await _toolStatusService.CheckYtDlpAsync(
                    _targetPath,
                    CancellationToken.None)
                .ConfigureAwait(false);
            if (!installedStatus.IsAvailable)
            {
                try
                {
                    await Task.Run(
                            () => RollBack(_targetPath, backupPath, originalExisted),
                            CancellationToken.None)
                        .ConfigureAwait(false);
                    return new YtDlpUpdateResult(
                        YtDlpUpdateStatus.ValidationFailedAndRolledBack);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    preserveBackup = true;
                    await LogFailureAsync("Rollback yt-dlp thất bại.", CancellationToken.None)
                        .ConfigureAwait(false);
                    return new YtDlpUpdateResult(YtDlpUpdateStatus.RollbackFailed);
                }
            }

            await _logger.LogAsync(
                    DiagnosticLogLevel.Information,
                    $"Cập nhật yt-dlp thành công: {installedStatus.Version}",
                    CancellationToken.None)
                .ConfigureAwait(false);
            return new YtDlpUpdateResult(
                YtDlpUpdateStatus.Success,
                installedStatus.Version);
        }
        finally
        {
            await Task.Run(
                    () =>
                    {
                        TryDelete(downloadedPath);
                        TryDelete(checksumPath);
                        if (!preserveBackup && (replacementStarted || File.Exists(backupPath)))
                        {
                            TryDelete(backupPath);
                        }
                    },
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
    }

    private async Task LogFailureAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            await _logger.LogAsync(DiagnosticLogLevel.Warning, message, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static bool ReplaceAtomically(
        string downloadedPath,
        string targetPath,
        string backupPath)
    {
        var originalExisted = File.Exists(targetPath);
        if (originalExisted)
        {
            File.Replace(downloadedPath, targetPath, backupPath, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(downloadedPath, targetPath);
        }

        return originalExisted;
    }

    private static void RollBack(string targetPath, string backupPath, bool originalExisted)
    {
        if (originalExisted)
        {
            if (!File.Exists(backupPath))
            {
                throw new IOException("The yt-dlp rollback file is unavailable.");
            }

            File.Replace(backupPath, targetPath, destinationBackupFileName: null);
        }
        else
        {
            File.Delete(targetPath);
        }
    }

    private static string? ReadExpectedChecksum(string checksumsPath)
    {
        foreach (var line in File.ReadLines(checksumsPath))
        {
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 &&
                string.Equals(parts[^1].TrimStart('*'), "yt-dlp.exe", StringComparison.Ordinal) &&
                parts[0].Length == 64 &&
                parts[0].All(Uri.IsHexDigit))
            {
                return parts[0];
            }
        }

        return null;
    }

    private static async Task<string> CalculateSha256Async(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }

    private static bool IsDownloadException(Exception exception) =>
        exception is HttpRequestException or
            IOException or
            UnauthorizedAccessException or
            InvalidDataException;

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
