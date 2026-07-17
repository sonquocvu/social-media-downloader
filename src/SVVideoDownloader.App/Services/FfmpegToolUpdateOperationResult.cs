using SVVideoDownloader.Infrastructure.Updates;

namespace SVVideoDownloader.App.Services;

public sealed record FfmpegToolUpdateOperationResult(
    bool WasBlocked,
    FfmpegUpdateResult? UpdateResult);
