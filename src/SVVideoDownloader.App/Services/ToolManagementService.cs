using SVVideoDownloader.Infrastructure.ExternalTools;
using SVVideoDownloader.Infrastructure.Updates;

namespace SVVideoDownloader.App.Services;

public sealed class ToolManagementService(
    IExternalToolStatusService statusService,
    IYtDlpUpdateService updateService,
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

        var result = await updateService.UpdateAsync(cancellationToken);
        return new ToolUpdateOperationResult(false, result);
    }
}
