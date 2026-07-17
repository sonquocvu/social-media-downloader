using SVVideoDownloader.Core.Media;

namespace SVVideoDownloader.Infrastructure.ExternalTools;

public sealed record ExternalToolStatus(
    ExternalToolKind Tool,
    string ExecutablePath,
    bool IsAvailable,
    string? Version,
    MediaErrorCategory? ErrorCategory);
