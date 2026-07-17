using SVVideoDownloader.Infrastructure.ExternalTools;

namespace SVVideoDownloader.App.Services;

public interface IToolManagementService
{
    Task<IReadOnlyList<ExternalToolStatus>> CheckStatusAsync(
        CancellationToken cancellationToken = default);

    Task<ToolUpdateOperationResult> UpdateYtDlpAsync(
        CancellationToken cancellationToken = default);
}
