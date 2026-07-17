using System.Security.Cryptography;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Infrastructure.Diagnostics;
using SVVideoDownloader.Infrastructure.ExternalTools;
using SVVideoDownloader.Infrastructure.Updates;

namespace SVVideoDownloader.Infrastructure.Tests;

public sealed class YtDlpUpdateServiceTests
{
    [Fact]
    public async Task UpdateVerifiesChecksumAndAtomicallyReplacesExistingExecutable()
    {
        using var directory = new TemporaryTestDirectory();
        var options = CreateOptions(directory.Path);
        await File.WriteAllTextAsync(options.YtDlpPath, "old executable");
        var newBytes = "new verified executable"u8.ToArray();
        var downloader = new FakeDownloadClient(newBytes);
        var status = new FakeToolStatusService(
            Available(options.YtDlpPath, "2026.07.17"),
            Available(options.YtDlpPath, "2026.07.17"));
        var service = new YtDlpUpdateService(
            options,
            downloader,
            status,
            new NullLogger());

        var result = await service.UpdateAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("2026.07.17", result.InstalledVersion);
        Assert.Equal(newBytes, await File.ReadAllBytesAsync(options.YtDlpPath));
        Assert.DoesNotContain(
            Directory.GetFiles(directory.Path),
            path => Path.GetFileName(path).StartsWith(".yt-dlp", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InvalidInstalledReplacementRollsBackOriginalExecutable()
    {
        using var directory = new TemporaryTestDirectory();
        var options = CreateOptions(directory.Path);
        const string original = "known working executable";
        await File.WriteAllTextAsync(options.YtDlpPath, original);
        var downloader = new FakeDownloadClient("new executable"u8.ToArray());
        var status = new FakeToolStatusService(
            Available(options.YtDlpPath, "2026.07.17"),
            new ExternalToolStatus(
                ExternalToolKind.YtDlp,
                options.YtDlpPath,
                false,
                null,
                MediaErrorCategory.DependencyInvalid));
        var service = new YtDlpUpdateService(
            options,
            downloader,
            status,
            new NullLogger());

        var result = await service.UpdateAsync();

        Assert.Equal(YtDlpUpdateStatus.ValidationFailedAndRolledBack, result.Status);
        Assert.Equal(original, await File.ReadAllTextAsync(options.YtDlpPath));
    }

    [Fact]
    public async Task ChecksumMismatchNeverChangesExistingExecutable()
    {
        using var directory = new TemporaryTestDirectory();
        var options = CreateOptions(directory.Path);
        const string original = "existing executable";
        await File.WriteAllTextAsync(options.YtDlpPath, original);
        var downloader = new FakeDownloadClient(
            "untrusted executable"u8.ToArray(),
            checksumOverride: new string('0', 64));
        var service = new YtDlpUpdateService(
            options,
            downloader,
            new FakeToolStatusService(),
            new NullLogger());

        var result = await service.UpdateAsync();

        Assert.Equal(YtDlpUpdateStatus.ChecksumMismatch, result.Status);
        Assert.Equal(original, await File.ReadAllTextAsync(options.YtDlpPath));
    }

    private static ExternalToolOptions CreateOptions(string toolsDirectory) =>
        ExternalToolOptions.CreateForToolsDirectory(toolsDirectory, toolsDirectory);

    private static ExternalToolStatus Available(string path, string version) =>
        new(ExternalToolKind.YtDlp, path, true, version, null);

    private sealed class FakeDownloadClient(
        byte[] executableBytes,
        string? checksumOverride = null) : IFileDownloadClient
    {
        public async Task DownloadAsync(
            Uri source,
            string destinationPath,
            long maximumBytes,
            CancellationToken cancellationToken = default)
        {
            if (source.AbsolutePath.EndsWith("SHA2-256SUMS", StringComparison.Ordinal))
            {
                var checksum = checksumOverride ??
                    Convert.ToHexString(SHA256.HashData(executableBytes));
                await File.WriteAllTextAsync(
                    destinationPath,
                    $"{checksum}  yt-dlp.exe\n",
                    cancellationToken);
            }
            else
            {
                await File.WriteAllBytesAsync(
                    destinationPath,
                    executableBytes,
                    cancellationToken);
            }
        }
    }

    private sealed class FakeToolStatusService(
        params ExternalToolStatus[] statuses) : IExternalToolStatusService
    {
        private readonly Queue<ExternalToolStatus> _statuses = new(statuses);

        public Task<IReadOnlyList<ExternalToolStatus>> CheckAllAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ExternalToolStatus> CheckYtDlpAsync(
            string executablePath,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_statuses.Dequeue());
    }

    private sealed class NullLogger : IDiagnosticLogger
    {
        public Task LogAsync(
            DiagnosticLogLevel level,
            string message,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
