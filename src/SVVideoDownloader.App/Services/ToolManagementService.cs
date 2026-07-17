using SVVideoDownloader.Infrastructure.ExternalTools;
using SVVideoDownloader.Infrastructure.Updates;

namespace SVVideoDownloader.App.Services;

public sealed class ToolManagementService(
    IExternalToolStatusService statusService,
    IYtDlpUpdateService ytDlpUpdateService,
    IFfmpegUpdateService ffmpegUpdateService,
    IEngineOperationGate operationGate) : IToolManagementService
{
    public Task<IReadOnlyList<ExternalToolStatus>> CheckStatusAsync(
        CancellationToken cancellationToken = default) =>
        statusService.CheckAllAsync(cancellationToken);

    public async Task<ToolUpdateOperationResult> UpdateYtDlpAsync(
        CancellationToken cancellationToken = default)
    {
        using var updateLease = operationGate.TryEnterUpdate();
        if (updateLease is null)
        {
            return new ToolUpdateOperationResult(true, null);
        }

        var result = await ytDlpUpdateService.UpdateAsync(cancellationToken);
        return new ToolUpdateOperationResult(false, result);
    }

    public async Task<FfmpegToolUpdateOperationResult> UpdateFfmpegAsync(
        CancellationToken cancellationToken = default)
    {
        using var updateLease = operationGate.TryEnterUpdate();
        if (updateLease is null)
        {
            return new FfmpegToolUpdateOperationResult(true, null);
        }

        var result = await ffmpegUpdateService.UpdateAsync(cancellationToken);
        return new FfmpegToolUpdateOperationResult(false, result);
    }
}
