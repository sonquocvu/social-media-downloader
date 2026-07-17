namespace SVVideoDownloader.Infrastructure.ExternalTools;

public interface IExternalToolStatusService
{
    Task<IReadOnlyList<ExternalToolStatus>> CheckAllAsync(
        CancellationToken cancellationToken = default);

    Task<ExternalToolStatus> CheckYtDlpAsync(
        string executablePath,
        CancellationToken cancellationToken = default);
}
