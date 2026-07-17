using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Infrastructure.ExternalTools;
using SVVideoDownloader.Infrastructure.Processes;
using SVVideoDownloader.Infrastructure.Tests.Fakes;

namespace SVVideoDownloader.Infrastructure.Tests;

public sealed class ExternalToolStatusServiceTests
{
    [Fact]
    public async Task CheckAllReturnsAvailabilityAndMachineVersions()
    {
        var runner = new FakeProcessRunner();
        runner.EnqueueResult(new ProcessRunResult(0, "2026.07.17\n", string.Empty));
        runner.EnqueueResult(new ProcessRunResult(0, "ffmpeg version 8.0 Copyright", string.Empty));
        runner.EnqueueResult(new ProcessRunResult(0, "ffprobe version 8.0 Copyright", string.Empty));
        var service = new ExternalToolStatusService(TestData.CreateOptions(), runner);

        var statuses = await service.CheckAllAsync();

        Assert.All(statuses, status => Assert.True(status.IsAvailable));
        Assert.Equal("2026.07.17", statuses[0].Version);
        Assert.Equal("8.0", statuses[1].Version);
        Assert.Equal("8.0", statuses[2].Version);
        Assert.Equal(new[] { "--version" }, runner.Requests[0].ArgumentList);
        Assert.Equal(new[] { "-version" }, runner.Requests[1].ArgumentList);
    }

    [Fact]
    public async Task MissingToolReturnsStructuredStatusWithoutDiagnostics()
    {
        var runner = new FakeProcessRunner();
        runner.EnqueueException(
            new ExternalProcessStartException(ProcessStartFailureKind.Missing));
        var service = new ExternalToolStatusService(TestData.CreateOptions(), runner);

        var status = await service.CheckYtDlpAsync(TestData.YtDlpPath);

        Assert.False(status.IsAvailable);
        Assert.Null(status.Version);
        Assert.Equal(MediaErrorCategory.DependencyMissing, status.ErrorCategory);
    }
}
