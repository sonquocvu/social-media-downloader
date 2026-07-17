using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using SVVideoDownloader.Infrastructure.Diagnostics;
using SVVideoDownloader.Infrastructure.ExternalTools;

namespace SVVideoDownloader.Infrastructure.Updates;

public sealed class FfmpegUpdateService : IFfmpegUpdateService
{
    private const long MaximumArchiveBytes = 256L * 1024 * 1024;
    private const long MaximumChecksumBytes = 64L * 1024;
    private const long MaximumExecutableBytes = 300L * 1024 * 1024;
    private static readonly Uri ArchiveUri = new(
        "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip");
    private static readonly Uri ChecksumUri = new(
        "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip.sha256");

    private readonly string _ffmpegTargetPath;
    private readonly string _ffprobeTargetPath;
    private readonly IFileDownloadClient _downloadClient;
    private readonly IExternalToolStatusService _toolStatusService;
    private readonly IDiagnosticLogger _logger;

    public FfmpegUpdateService(
        ExternalToolOptions options,
        IFileDownloadClient downloadClient,
        IExternalToolStatusService toolStatusService,
        IDiagnosticLogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _ffmpegTargetPath = options.FfmpegPath;
        _ffprobeTargetPath = options.FfprobePath;
        _downloadClient = downloadClient ?? throw new ArgumentNullException(nameof(downloadClient));
        _toolStatusService = toolStatusService ??
            throw new ArgumentNullException(nameof(toolStatusService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FfmpegUpdateResult> UpdateAsync(
        CancellationToken cancellationToken = default)
    {
        var targetDirectory = Path.GetDirectoryName(_ffmpegTargetPath)!;
        var operationId = Guid.NewGuid().ToString("N");
        var archivePath = Path.Combine(targetDirectory, $".ffmpeg.{operationId}.zip");
        var checksumPath = Path.Combine(targetDirectory, $".ffmpeg.{operationId}.sha256");
        var ffmpegCandidatePath = Path.Combine(
            targetDirectory,
            $".ffmpeg.{operationId}.candidate.exe");
        var ffprobeCandidatePath = Path.Combine(
            targetDirectory,
            $".ffprobe.{operationId}.candidate.exe");
        var ffmpegBackupPath = Path.Combine(targetDirectory, $".ffmpeg.{operationId}.backup");
        var ffprobeBackupPath = Path.Combine(targetDirectory, $".ffprobe.{operationId}.backup");
        var ffmpegReplaced = false;
        var ffprobeReplaced = false;
        var ffmpegOriginalExisted = false;
        var ffprobeOriginalExisted = false;
        var preserveBackups = false;

        try
        {
            await Task.Run(
                    () => Directory.CreateDirectory(targetDirectory),
                    cancellationToken)
                .ConfigureAwait(false);
            await _logger.LogAsync(
                    DiagnosticLogLevel.Information,
                    "Bắt đầu cài đặt hoặc cập nhật FFmpeg thủ công.",
                    cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await _downloadClient.DownloadAsync(
                        ChecksumUri,
                        checksumPath,
                        MaximumChecksumBytes,
                        cancellationToken)
                    .ConfigureAwait(false);
                await _downloadClient.DownloadAsync(
                        ArchiveUri,
                        archivePath,
                        MaximumArchiveBytes,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (IsDownloadException(exception))
            {
                await LogFailureAsync("Tải gói FFmpeg thất bại.", cancellationToken)
                    .ConfigureAwait(false);
                return new FfmpegUpdateResult(FfmpegUpdateStatus.DownloadFailed);
            }

            var expectedChecksum = await Task.Run(
                    () => ReadExpectedChecksum(checksumPath),
                    cancellationToken)
                .ConfigureAwait(false);
            if (expectedChecksum is null)
            {
                return new FfmpegUpdateResult(FfmpegUpdateStatus.ChecksumUnavailable);
            }

            var actualChecksum = await CalculateSha256Async(archivePath, cancellationToken)
                .ConfigureAwait(false);
            if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(expectedChecksum),
                    Convert.FromHexString(actualChecksum)))
            {
                await LogFailureAsync("Checksum gói FFmpeg không khớp.", cancellationToken)
                    .ConfigureAwait(false);
                return new FfmpegUpdateResult(FfmpegUpdateStatus.ChecksumMismatch);
            }

            try
            {
                await ExtractRequiredExecutablesAsync(
                        archivePath,
                        ffmpegCandidatePath,
                        ffprobeCandidatePath,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (IsArchiveException(exception))
            {
                await LogFailureAsync("Gói FFmpeg không hợp lệ.", cancellationToken)
                    .ConfigureAwait(false);
                return new FfmpegUpdateResult(FfmpegUpdateStatus.InvalidArchive);
            }

            var candidateFfmpegStatus = await _toolStatusService.CheckFfmpegAsync(
                    ffmpegCandidatePath,
                    cancellationToken)
                .ConfigureAwait(false);
            var candidateFfprobeStatus = await _toolStatusService.CheckFfprobeAsync(
                    ffprobeCandidatePath,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!candidateFfmpegStatus.IsAvailable || !candidateFfprobeStatus.IsAvailable)
            {
                return new FfmpegUpdateResult(FfmpegUpdateStatus.InvalidDownloadedExecutables);
            }

            try
            {
                ffmpegOriginalExisted = ReplaceAtomically(
                    ffmpegCandidatePath,
                    _ffmpegTargetPath,
                    ffmpegBackupPath);
                ffmpegReplaced = true;
                ffprobeOriginalExisted = ReplaceAtomically(
                    ffprobeCandidatePath,
                    _ffprobeTargetPath,
                    ffprobeBackupPath);
                ffprobeReplaced = true;
            }
            catch (Exception exception) when (IsReplacementException(exception))
            {
                try
                {
                    RollBackPair(
                        ffmpegReplaced,
                        ffmpegOriginalExisted,
                        _ffmpegTargetPath,
                        ffmpegBackupPath,
                        ffprobeReplaced,
                        ffprobeOriginalExisted,
                        _ffprobeTargetPath,
                        ffprobeBackupPath);
                    return new FfmpegUpdateResult(FfmpegUpdateStatus.ReplacementFailed);
                }
                catch (Exception rollbackException) when (IsReplacementException(rollbackException))
                {
                    preserveBackups = true;
                    await LogFailureAsync("Rollback gói FFmpeg thất bại.", CancellationToken.None)
                        .ConfigureAwait(false);
                    return new FfmpegUpdateResult(FfmpegUpdateStatus.RollbackFailed);
                }
            }

            var installedFfmpegStatus = await _toolStatusService.CheckFfmpegAsync(
                    _ffmpegTargetPath,
                    CancellationToken.None)
                .ConfigureAwait(false);
            var installedFfprobeStatus = await _toolStatusService.CheckFfprobeAsync(
                    _ffprobeTargetPath,
                    CancellationToken.None)
                .ConfigureAwait(false);
            if (!installedFfmpegStatus.IsAvailable || !installedFfprobeStatus.IsAvailable)
            {
                try
                {
                    RollBackPair(
                        ffmpegReplaced,
                        ffmpegOriginalExisted,
                        _ffmpegTargetPath,
                        ffmpegBackupPath,
                        ffprobeReplaced,
                        ffprobeOriginalExisted,
                        _ffprobeTargetPath,
                        ffprobeBackupPath);
                    return new FfmpegUpdateResult(
                        FfmpegUpdateStatus.ValidationFailedAndRolledBack);
                }
                catch (Exception exception) when (IsReplacementException(exception))
                {
                    preserveBackups = true;
                    await LogFailureAsync("Rollback gói FFmpeg thất bại.", CancellationToken.None)
                        .ConfigureAwait(false);
                    return new FfmpegUpdateResult(FfmpegUpdateStatus.RollbackFailed);
                }
            }

            await _logger.LogAsync(
                    DiagnosticLogLevel.Information,
                    $"Cập nhật FFmpeg thành công: {installedFfmpegStatus.Version}",
                    CancellationToken.None)
                .ConfigureAwait(false);
            return new FfmpegUpdateResult(
                FfmpegUpdateStatus.Success,
                installedFfmpegStatus.Version,
                installedFfprobeStatus.Version);
        }
        finally
        {
            await Task.Run(
                    () =>
                    {
                        TryDelete(archivePath);
                        TryDelete(checksumPath);
                        TryDelete(ffmpegCandidatePath);
                        TryDelete(ffprobeCandidatePath);
                        if (!preserveBackups)
                        {
                            TryDelete(ffmpegBackupPath);
                            TryDelete(ffprobeBackupPath);
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

    private static async Task ExtractRequiredExecutablesAsync(
        string archivePath,
        string ffmpegDestinationPath,
        string ffprobeDestinationPath,
        CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var ffmpegEntry = FindUniqueExecutableEntry(archive, "ffmpeg.exe");
        var ffprobeEntry = FindUniqueExecutableEntry(archive, "ffprobe.exe");
        await ExtractEntryAsync(ffmpegEntry, ffmpegDestinationPath, cancellationToken)
            .ConfigureAwait(false);
        await ExtractEntryAsync(ffprobeEntry, ffprobeDestinationPath, cancellationToken)
            .ConfigureAwait(false);
    }

    private static ZipArchiveEntry FindUniqueExecutableEntry(
        ZipArchive archive,
        string executableName)
    {
        var expectedSuffix = $"/bin/{executableName}";
        var matches = archive.Entries
            .Where(entry =>
            {
                var normalizedName = entry.FullName.Replace('\\', '/');
                return normalizedName.Equals(
                           $"bin/{executableName}",
                           StringComparison.OrdinalIgnoreCase) ||
                    normalizedName.EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase);
            })
            .Take(2)
            .ToArray();
        if (matches.Length != 1 ||
            matches[0].Length <= 0 ||
            matches[0].Length > MaximumExecutableBytes)
        {
            throw new InvalidDataException("The FFmpeg archive has invalid executable entries.");
        }

        return matches[0];
    }

    private static async Task ExtractEntryAsync(
        ZipArchiveEntry entry,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        await using var input = entry.Open();
        await using var output = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var buffer = new byte[64 * 1024];
        long totalBytes = 0;
        int bytesRead;
        while ((bytesRead = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            totalBytes += bytesRead;
            if (totalBytes > MaximumExecutableBytes)
            {
                throw new InvalidDataException("An FFmpeg executable exceeds the allowed size.");
            }

            await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken)
                .ConfigureAwait(false);
        }

        if (totalBytes != entry.Length)
        {
            throw new InvalidDataException("An FFmpeg executable has an invalid length.");
        }

        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool ReplaceAtomically(
        string candidatePath,
        string targetPath,
        string backupPath)
    {
        var originalExisted = File.Exists(targetPath);
        if (originalExisted)
        {
            File.Replace(candidatePath, targetPath, backupPath, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(candidatePath, targetPath);
        }

        return originalExisted;
    }

    private static void RollBackPair(
        bool ffmpegReplaced,
        bool ffmpegOriginalExisted,
        string ffmpegTargetPath,
        string ffmpegBackupPath,
        bool ffprobeReplaced,
        bool ffprobeOriginalExisted,
        string ffprobeTargetPath,
        string ffprobeBackupPath)
    {
        Exception? firstFailure = null;
        TryRollBack(
            ffprobeReplaced,
            ffprobeTargetPath,
            ffprobeBackupPath,
            ffprobeOriginalExisted,
            ref firstFailure);
        TryRollBack(
            ffmpegReplaced,
            ffmpegTargetPath,
            ffmpegBackupPath,
            ffmpegOriginalExisted,
            ref firstFailure);
        if (firstFailure is not null)
        {
            throw new IOException("One or more FFmpeg executables could not be restored.", firstFailure);
        }
    }

    private static void TryRollBack(
        bool wasReplaced,
        string targetPath,
        string backupPath,
        bool originalExisted,
        ref Exception? firstFailure)
    {
        if (!wasReplaced)
        {
            return;
        }

        try
        {
            if (originalExisted)
            {
                if (!File.Exists(backupPath))
                {
                    throw new IOException("An FFmpeg rollback file is unavailable.");
                }

                File.Replace(backupPath, targetPath, destinationBackupFileName: null);
            }
            else
            {
                File.Delete(targetPath);
            }
        }
        catch (Exception exception) when (IsReplacementException(exception))
        {
            firstFailure ??= exception;
        }
    }

    private static string? ReadExpectedChecksum(string checksumPath)
    {
        var firstLine = File.ReadLines(checksumPath)
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
        var candidate = firstLine?
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return candidate is { Length: 64 } && candidate.All(Uri.IsHexDigit)
            ? candidate
            : null;
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

    private static bool IsArchiveException(Exception exception) =>
        exception is IOException or
            UnauthorizedAccessException or
            InvalidDataException or
            NotSupportedException;

    private static bool IsReplacementException(Exception exception) =>
        exception is IOException or UnauthorizedAccessException;

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
