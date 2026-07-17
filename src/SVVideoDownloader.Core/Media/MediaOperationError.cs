namespace SVVideoDownloader.Core.Media;

public sealed record MediaOperationError(
    MediaErrorCategory Category,
    MediaComponent Component);
