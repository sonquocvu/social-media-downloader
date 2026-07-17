using SVVideoDownloader.App.Services;
using SVVideoDownloader.Infrastructure.ExternalTools;
using SVVideoDownloader.Infrastructure.Updates;

namespace SVVideoDownloader.App.Tests;

public sealed class EngineOperationGateTests
{
    [Fact]
    public void UpdateCannotStartDuringDownloadAndDownloadCannotStartDuringUpdate()
    {
        var gate = new EngineOperationGate();
        using var download = gate.TryEnterDownload();

        Assert.NotNull(download);
        Assert.Equal(1, gate.ActiveDownloadCount);
        Assert.Null(gate.TryEnterUpdate());

        download.Dispose();
        using var update = gate.TryEnterUpdate();

        Assert.NotNull(update);
        Assert.True(gate.IsUpdateActive);
        Assert.Null(gate.TryEnterDownload());
        Assert.Null(gate.TryEnterMetadataOperation());
    }

    [Fact]
    public async Task ToolManagementDoesNotCallUpdaterWhileEngineIsInUse()
    {
        var gate = new EngineOperationGate();
        var updater = new RecordingUpdater();
        var service = new ToolManagementService(
            new EmptyStatusService(),
            updater,
            gate);
        using var download = gate.TryEnterDownload();

        var result = await service.UpdateYtDlpAsync();

        Assert.True(result.WasBlocked);
        Assert.Equal(0, updater.CallCount);
    }

    private sealed class RecordingUpdater : IYtDlpUpdateService
    {
        public int CallCount { get; private set; }

        public Task<YtDlpUpdateResult> UpdateAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(
                new YtDlpUpdateResult(YtDlpUpdateStatus.Success, "2026.07.17"));
        }
    }

    private sealed class EmptyStatusService : IExternalToolStatusService
    {
        public Task<IReadOnlyList<ExternalToolStatus>> CheckAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExternalToolStatus>>([]);

        public Task<ExternalToolStatus> CheckYtDlpAsync(
            string executablePath,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
