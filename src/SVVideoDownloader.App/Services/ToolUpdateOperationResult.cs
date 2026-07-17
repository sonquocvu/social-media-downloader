using SVVideoDownloader.Infrastructure.Updates;

namespace SVVideoDownloader.App.Services;

public sealed record ToolUpdateOperationResult(
    bool WasBlocked,
    YtDlpUpdateResult? UpdateResult);
