using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Infrastructure.ExternalTools;
using SVVideoDownloader.Infrastructure.Processes;
using SVVideoDownloader.Infrastructure.Tests.Fakes;

namespace SVVideoDownloader.Infrastructure.Tests;

public sealed class ExternalToolVerifierTests
{
    [Theory]
    [InlineData(ProcessStartFailureKind.Missing, MediaErrorCategory.DependencyMissing)]
    [InlineData(ProcessStartFailureKind.Inaccessible, MediaErrorCategory.DependencyInaccessible)]
    [InlineData(ProcessStartFailureKind.InvalidExecutable, MediaErrorCategory.DependencyInvalid)]
    public async Task VerifyYtDlpMapsStartFailures(
        ProcessStartFailureKind failureKind,
        MediaErrorCategory expectedCategory)
    {
        var runner = new FakeProcessRunner();
        runner.EnqueueException(new ExternalProcessStartException(failureKind));
        var verifier = new ExternalToolVerifier(runner);

        var error = await verifier.VerifyYtDlpAsync(TestData.YtDlpPath, CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal(expectedCategory, error.Category);
        Assert.Equal(MediaComponent.MetadataExtractor, error.Component);
    }

    [Fact]
    public async Task VerifyYtDlpAcceptsDateBasedVersionOutput()
    {
        var runner = new FakeProcessRunner();
        runner.EnqueueResult(new ProcessRunResult(0, "2026.07.17\n", string.Empty));
        var verifier = new ExternalToolVerifier(runner);

        var error = await verifier.VerifyYtDlpAsync(TestData.YtDlpPath, CancellationToken.None);

        Assert.Null(error);
        var request = Assert.Single(runner.Requests);
        Assert.Equal(new[] { "--version" }, request.ArgumentList);
        Assert.Equal(TimeSpan.FromSeconds(10), request.Timeout);
    }

    [Theory]
    [InlineData("ffmpeg version 8.0 Copyright", MediaComponent.MediaProcessor)]
    [InlineData("ffprobe version 8.0 Copyright", MediaComponent.MediaProbe)]
    public async Task VerifyFfmpegToolsAcceptExpectedMachineIdentity(
        string versionOutput,
        MediaComponent expectedComponent)
    {
        var runner = new FakeProcessRunner();
        runner.EnqueueResult(new ProcessRunResult(0, versionOutput, string.Empty));
        var verifier = new ExternalToolVerifier(runner);

        var error = expectedComponent == MediaComponent.MediaProcessor
            ? await verifier.VerifyFfmpegAsync(TestData.FfmpegPath, CancellationToken.None)
            : await verifier.VerifyFfprobeAsync(TestData.FfprobePath, CancellationToken.None);

        Assert.Null(error);
    }

    [Fact]
    public async Task VerifyFfmpegRejectsWrongExecutableIdentity()
    {
        var runner = new FakeProcessRunner();
        runner.EnqueueResult(new ProcessRunResult(0, "unrelated tool 1.0", string.Empty));
        var verifier = new ExternalToolVerifier(runner);

        var error = await verifier.VerifyFfmpegAsync(TestData.FfmpegPath, CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal(MediaErrorCategory.DependencyInvalid, error.Category);
        Assert.Equal(MediaComponent.MediaProcessor, error.Component);
    }

    [Fact]
    public async Task VerificationTimeoutReturnsStructuredCategory()
    {
        var runner = new FakeProcessRunner();
        runner.EnqueueException(new ExternalProcessTimeoutException());
        var verifier = new ExternalToolVerifier(runner);

        var error = await verifier.VerifyYtDlpAsync(TestData.YtDlpPath, CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal(MediaErrorCategory.TimedOut, error.Category);
    }

    [Fact]
    public async Task VerificationErrorDoesNotExposeCapturedSecrets()
    {
        const string secret = "cookie=private-token";
        var runner = new FakeProcessRunner();
        runner.EnqueueResult(new ProcessRunResult(1, string.Empty, secret));
        var verifier = new ExternalToolVerifier(runner);

        var error = await verifier.VerifyYtDlpAsync(TestData.YtDlpPath, CancellationToken.None);

        Assert.NotNull(error);
        Assert.DoesNotContain(secret, error.ToString(), StringComparison.Ordinal);
    }
}
