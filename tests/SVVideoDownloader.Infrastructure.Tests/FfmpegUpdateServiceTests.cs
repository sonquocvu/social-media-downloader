using System.IO.Compression;
using System.Security.Cryptography;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Infrastructure.Diagnostics;
using SVVideoDownloader.Infrastructure.ExternalTools;
using SVVideoDownloader.Infrastructure.Updates;

namespace SVVideoDownloader.Infrastructure.Tests;

public sealed class FfmpegUpdateServiceTests
{
    private static readonly byte[] NewFfmpegBytes = "new ffmpeg executable"u8.ToArray();
    private static readonly byte[] NewFfprobeBytes = "new ffprobe executable"u8.ToArray();

    [Fact]
    public async Task UpdateVerifiesArchiveAndReplacesBothExecutables()
    {
        using var directory = new TemporaryTestDirectory();
        var options = CreateOptions(directory.Path);
        await File.WriteAllTextAsync(options.FfmpegPath, "old ffmpeg");
        await File.WriteAllTextAsync(options.FfprobePath, "old ffprobe");
        var archive = CreateArchive();
        var downloader = new FakeDownloadClient(archive);
        var status = new FakeToolStatusService([true, true], [true, true]);
        var service = CreateService(options, downloader, status);

        var result = await service.UpdateAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("8.1.2", result.FfmpegVersion);
        Assert.Equal("8.1.2", result.FfprobeVersion);
        Assert.Equal(NewFfmpegBytes, await File.ReadAllBytesAsync(options.FfmpegPath));
        Assert.Equal(NewFfprobeBytes, await File.ReadAllBytesAsync(options.FfprobePath));
        Assert.Equal(2, downloader.RequestedSources.Count);
        Assert.All(downloader.RequestedSources, source => Assert.Equal("https", source.Scheme));
        AssertNoTemporaryFiles(directory.Path);
    }

    [Fact]
    public async Task ChecksumMismatchLeavesBothExistingExecutablesUntouched()
    {
        using var directory = new TemporaryTestDirectory();
        var options = CreateOptions(directory.Path);
        await File.WriteAllTextAsync(options.FfmpegPath, "old ffmpeg");
        await File.WriteAllTextAsync(options.FfprobePath, "old ffprobe");
        var downloader = new FakeDownloadClient(
            CreateArchive(),
            checksumOverride: new string('0', 64));
        var service = CreateService(options, downloader, new FakeToolStatusService([], []));

        var result = await service.UpdateAsync();

        Assert.Equal(FfmpegUpdateStatus.ChecksumMismatch, result.Status);
        Assert.Equal("old ffmpeg", await File.ReadAllTextAsync(options.FfmpegPath));
        Assert.Equal("old ffprobe", await File.ReadAllTextAsync(options.FfprobePath));
        AssertNoTemporaryFiles(directory.Path);
    }

    [Fact]
    public async Task ArchiveMissingFfprobeIsRejectedBeforeReplacement()
    {
        using var directory = new TemporaryTestDirectory();
        var options = CreateOptions(directory.Path);
        var archive = CreateArchive(includeFfprobe: false);
        var service = CreateService(
            options,
            new FakeDownloadClient(archive),
            new FakeToolStatusService([], []));

        var result = await service.UpdateAsync();

        Assert.Equal(FfmpegUpdateStatus.InvalidArchive, result.Status);
        Assert.False(File.Exists(options.FfmpegPath));
        Assert.False(File.Exists(options.FfprobePath));
        AssertNoTemporaryFiles(directory.Path);
    }

    [Fact]
    public async Task InvalidInstalledPairRollsBackBothOriginalExecutables()
    {
        using var directory = new TemporaryTestDirectory();
        var options = CreateOptions(directory.Path);
        await File.WriteAllTextAsync(options.FfmpegPath, "known ffmpeg");
        await File.WriteAllTextAsync(options.FfprobePath, "known ffprobe");
        var status = new FakeToolStatusService([true, false], [true, true]);
        var service = CreateService(options, new FakeDownloadClient(CreateArchive()), status);

        var result = await service.UpdateAsync();

        Assert.Equal(FfmpegUpdateStatus.ValidationFailedAndRolledBack, result.Status);
        Assert.Equal("known ffmpeg", await File.ReadAllTextAsync(options.FfmpegPath));
        Assert.Equal("known ffprobe", await File.ReadAllTextAsync(options.FfprobePath));
        AssertNoTemporaryFiles(directory.Path);
    }

    [Fact]
    public async Task LockedFfprobeRollsBackAlreadyReplacedFfmpeg()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var directory = new TemporaryTestDirectory();
        var options = CreateOptions(directory.Path);
        await File.WriteAllTextAsync(options.FfmpegPath, "known ffmpeg");
        await File.WriteAllTextAsync(options.FfprobePath, "known ffprobe");
        var status = new FakeToolStatusService([true], [true]);
        var service = CreateService(options, new FakeDownloadClient(CreateArchive()), status);

        FfmpegUpdateResult result;
        await using (var lockedFile = new FileStream(
                         options.FfprobePath,
                         FileMode.Open,
                         FileAccess.Read,
                         FileShare.None))
        {
            result = await service.UpdateAsync();
        }

        Assert.Equal(FfmpegUpdateStatus.ReplacementFailed, result.Status);
        Assert.Equal("known ffmpeg", await File.ReadAllTextAsync(options.FfmpegPath));
        Assert.Equal("known ffprobe", await File.ReadAllTextAsync(options.FfprobePath));
        AssertNoTemporaryFiles(directory.Path);
    }

    [Fact]
    public async Task CancellationDuringDownloadLeavesExistingToolsUntouched()
    {
        using var directory = new TemporaryTestDirectory();
        var options = CreateOptions(directory.Path);
        await File.WriteAllTextAsync(options.FfmpegPath, "known ffmpeg");
        await File.WriteAllTextAsync(options.FfprobePath, "known ffprobe");
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var service = CreateService(
            options,
            new FakeDownloadClient(CreateArchive()),
            new FakeToolStatusService([], []));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.UpdateAsync(cancellationSource.Token));

        Assert.Equal("known ffmpeg", await File.ReadAllTextAsync(options.FfmpegPath));
        Assert.Equal("known ffprobe", await File.ReadAllTextAsync(options.FfprobePath));
        AssertNoTemporaryFiles(directory.Path);
    }

    private static FfmpegUpdateService CreateService(
        ExternalToolOptions options,
        IFileDownloadClient downloader,
        IExternalToolStatusService statusService) =>
        new(options, downloader, statusService, new NullLogger());

    private static ExternalToolOptions CreateOptions(string toolsDirectory) =>
        ExternalToolOptions.CreateForToolsDirectory(toolsDirectory, toolsDirectory);

    private static byte[] CreateArchive(bool includeFfprobe = true)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(
                archive,
                "ffmpeg-8.1.2-essentials_build/bin/ffmpeg.exe",
                NewFfmpegBytes);
            if (includeFfprobe)
            {
                WriteEntry(
                    archive,
                    "ffmpeg-8.1.2-essentials_build/bin/ffprobe.exe",
                    NewFfprobeBytes);
            }

            WriteEntry(archive, "ffmpeg-8.1.2-essentials_build/README.txt", "GPLv3"u8.ToArray());
        }

        return stream.ToArray();
    }

    private static void WriteEntry(ZipArchive archive, string name, byte[] content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.SmallestSize);
        using var output = entry.Open();
        output.Write(content);
    }

    private static void AssertNoTemporaryFiles(string directory) =>
        Assert.DoesNotContain(
            Directory.GetFiles(directory),
            path => Path.GetFileName(path).StartsWith(".ff", StringComparison.Ordinal));

    private sealed class FakeDownloadClient(
        byte[] archiveBytes,
        string? checksumOverride = null) : IFileDownloadClient
    {
        public List<Uri> RequestedSources { get; } = [];

        public async Task DownloadAsync(
            Uri source,
            string destinationPath,
            long maximumBytes,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestedSources.Add(source);
            if (source.AbsolutePath.EndsWith(".sha256", StringComparison.Ordinal))
            {
                var checksum = checksumOverride ??
                    Convert.ToHexString(SHA256.HashData(archiveBytes));
                await File.WriteAllTextAsync(destinationPath, checksum, cancellationToken);
            }
            else
            {
                await File.WriteAllBytesAsync(destinationPath, archiveBytes, cancellationToken);
            }
        }
    }

    private sealed class FakeToolStatusService(
        IEnumerable<bool> ffmpegAvailability,
        IEnumerable<bool> ffprobeAvailability) : IExternalToolStatusService
    {
        private readonly Queue<bool> _ffmpegAvailability = new(ffmpegAvailability);
        private readonly Queue<bool> _ffprobeAvailability = new(ffprobeAvailability);

        public Task<IReadOnlyList<ExternalToolStatus>> CheckAllAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ExternalToolStatus> CheckYtDlpAsync(
            string executablePath,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ExternalToolStatus> CheckFfmpegAsync(
            string executablePath,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateStatus(
                ExternalToolKind.Ffmpeg,
                executablePath,
                _ffmpegAvailability.Dequeue()));

        public Task<ExternalToolStatus> CheckFfprobeAsync(
            string executablePath,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateStatus(
                ExternalToolKind.Ffprobe,
                executablePath,
                _ffprobeAvailability.Dequeue()));

        private static ExternalToolStatus CreateStatus(
            ExternalToolKind tool,
            string path,
            bool available) =>
            available
                ? new ExternalToolStatus(tool, path, true, "8.1.2", null)
                : new ExternalToolStatus(
                    tool,
                    path,
                    false,
                    null,
                    MediaErrorCategory.DependencyInvalid);
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
