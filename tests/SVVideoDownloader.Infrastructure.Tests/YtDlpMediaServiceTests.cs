using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Infrastructure.Processes;
using SVVideoDownloader.Infrastructure.Tests.Fakes;
using SVVideoDownloader.Infrastructure.YtDlp;

namespace SVVideoDownloader.Infrastructure.Tests;

public sealed class YtDlpMediaServiceTests
{
    private const string MetadataJson = """
        {
          "title": "Video do tôi sở hữu",
          "uploader": "Tác giả",
          "duration": 30,
          "formats": [
            { "format_id": "best", "ext": "mp4", "vcodec": "h264", "acodec": "aac" }
          ]
        }
        """;

    [Fact]
    public async Task GetVideoInfoRunsVerifiedYtDlpAndReturnsCoreModel()
    {
        var runner = new FakeProcessRunner();
        runner.EnqueueResult(YtDlpVersion());
        runner.EnqueueResult(new ProcessRunResult(0, MetadataJson, string.Empty));
        var service = new YtDlpMediaService(TestData.CreateOptions(), runner);

        var result = await service.GetVideoInfoAsync(
            TestData.CreateSource(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Video do tôi sở hữu", result.Value!.Title);
        Assert.Equal(2, runner.Requests.Count);
        Assert.Equal(new[] { "--version" }, runner.Requests[0].ArgumentList);
        Assert.Contains("--dump-single-json", runner.Requests[1].ArgumentList);
    }

    [Fact]
    public async Task DownloadVerifiesToolchainAndReportsOnlyMachineProgress()
    {
        const string output = """
            [download] human-readable line that must be ignored
            SVVD_PROGRESS:{"downloaded_bytes":500,"total_bytes":1000,"speed":125}
            SVVD_OUTPUT:"C:\\svvd-test\\downloads\\video của tôi.mp4"
            """;
        var runner = new FakeProcessRunner();
        runner.EnqueueResult(YtDlpVersion());
        runner.EnqueueResult(FfmpegVersion());
        runner.EnqueueResult(FfprobeVersion());
        runner.EnqueueResult(new ProcessRunResult(0, output, string.Empty));
        var progress = new RecordingProgress();
        var service = new YtDlpMediaService(TestData.CreateOptions(), runner);

        var result = await service.DownloadAsync(
            TestData.CreateRequest(),
            progress,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("video của tôi.mp4", result.Value!.OutputFileName);
        Assert.Equal(4, runner.Requests.Count);
        var item = Assert.Single(progress.Values);
        Assert.Equal(50d, item.Percentage);
        Assert.Equal(500, item.DownloadedBytes);
    }

    [Theory]
    [InlineData(ProcessStartFailureKind.Missing, MediaErrorCategory.DependencyMissing)]
    [InlineData(ProcessStartFailureKind.Inaccessible, MediaErrorCategory.DependencyInaccessible)]
    [InlineData(ProcessStartFailureKind.InvalidExecutable, MediaErrorCategory.DependencyInvalid)]
    public async Task GetVideoInfoReturnsStructuredToolErrors(
        ProcessStartFailureKind startFailure,
        MediaErrorCategory expectedCategory)
    {
        var runner = new FakeProcessRunner();
        runner.EnqueueException(new ExternalProcessStartException(startFailure));
        var service = new YtDlpMediaService(TestData.CreateOptions(), runner);

        var result = await service.GetVideoInfoAsync(
            TestData.CreateSource(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCategory, result.Error!.Category);
        Assert.Equal(MediaComponent.MetadataExtractor, result.Error.Component);
    }

    [Fact]
    public async Task DownloadDetectsInaccessibleFfmpeg()
    {
        var runner = new FakeProcessRunner();
        runner.EnqueueResult(YtDlpVersion());
        runner.EnqueueException(
            new ExternalProcessStartException(ProcessStartFailureKind.Inaccessible));
        var service = new YtDlpMediaService(TestData.CreateOptions(), runner);

        var result = await service.DownloadAsync(
            TestData.CreateRequest(),
            cancellationToken: CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MediaErrorCategory.DependencyInaccessible, result.Error!.Category);
        Assert.Equal(MediaComponent.MediaProcessor, result.Error.Component);
    }

    [Fact]
    public async Task DownloadRejectsSuccessWithoutStructuredOutputPath()
    {
        var runner = new FakeProcessRunner();
        runner.EnqueueResult(YtDlpVersion());
        runner.EnqueueResult(FfmpegVersion());
        runner.EnqueueResult(FfprobeVersion());
        runner.EnqueueResult(
            new ProcessRunResult(0, "[download] finished", string.Empty));
        var service = new YtDlpMediaService(TestData.CreateOptions(), runner);

        var result = await service.DownloadAsync(
            TestData.CreateRequest(),
            cancellationToken: CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MediaErrorCategory.InvalidResponse, result.Error!.Category);
        Assert.Equal(MediaComponent.MetadataExtractor, result.Error.Component);
    }

    [Fact]
    public async Task GetVideoInfoDoesNotReturnRawStderr()
    {
        const string secret = "cookie=private-token&password=secret";
        var runner = new FakeProcessRunner();
        runner.EnqueueResult(YtDlpVersion());
        runner.EnqueueResult(new ProcessRunResult(1, string.Empty, secret));
        var service = new YtDlpMediaService(TestData.CreateOptions(), runner);

        var result = await service.GetVideoInfoAsync(
            TestData.CreateSource(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MediaErrorCategory.SourceUnavailable, result.Error!.Category);
        Assert.DoesNotContain(secret, result.Error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetVideoInfoMapsInvalidJsonWithoutParsingHumanOutput()
    {
        var runner = new FakeProcessRunner();
        runner.EnqueueResult(YtDlpVersion());
        runner.EnqueueResult(
            new ProcessRunResult(0, "[download] localized human output", string.Empty));
        var service = new YtDlpMediaService(TestData.CreateOptions(), runner);

        var result = await service.GetVideoInfoAsync(
            TestData.CreateSource(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MediaErrorCategory.InvalidResponse, result.Error!.Category);
    }

    [Fact]
    public async Task GetVideoInfoPropagatesCancellationToken()
    {
        var runner = new FakeProcessRunner();
        runner.EnqueueResult(YtDlpVersion());
        runner.EnqueueWaitForCancellation();
        var service = new YtDlpMediaService(TestData.CreateOptions(), runner);
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetVideoInfoAsync(
                TestData.CreateSource(),
                cancellationSource.Token));
    }

    [Fact]
    public async Task GetVideoInfoReturnsTimeoutCategoryWithoutDiagnostics()
    {
        var runner = new FakeProcessRunner();
        runner.EnqueueResult(YtDlpVersion());
        runner.EnqueueException(new ExternalProcessTimeoutException());
        var service = new YtDlpMediaService(TestData.CreateOptions(), runner);

        var result = await service.GetVideoInfoAsync(
            TestData.CreateSource(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MediaErrorCategory.TimedOut, result.Error!.Category);
        Assert.Equal(MediaComponent.MetadataExtractor, result.Error.Component);
    }

    private static ProcessRunResult YtDlpVersion() =>
        new(0, "2026.07.17\n", string.Empty);

    private static ProcessRunResult FfmpegVersion() =>
        new(0, "ffmpeg version 8.0\n", string.Empty);

    private static ProcessRunResult FfprobeVersion() =>
        new(0, "ffprobe version 8.0\n", string.Empty);

    private sealed class RecordingProgress : IProgress<DownloadProgress>
    {
        public List<DownloadProgress> Values { get; } = new();

        public void Report(DownloadProgress value) => Values.Add(value);
    }
}
