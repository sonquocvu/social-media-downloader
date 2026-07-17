namespace SVVideoDownloader.Infrastructure.Updates;

public sealed record FfmpegUpdateResult(
    FfmpegUpdateStatus Status,
    string? FfmpegVersion = null,
    string? FfprobeVersion = null)
{
    public bool IsSuccess => Status == FfmpegUpdateStatus.Success;
}
