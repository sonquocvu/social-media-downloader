using System;
using System.IO;
using SVVideoDownloader.Infrastructure.Processes;

namespace SVVideoDownloader.Infrastructure.ExternalTools;

public sealed class ExternalToolOptions
{
    public ExternalToolOptions(
        string ytDlpPath,
        string ffmpegPath,
        string ffprobePath,
        string outputDirectory,
        TimeSpan? metadataTimeout = null,
        TimeSpan? downloadTimeout = null)
    {
        YtDlpPath = ValidateExecutablePath(ytDlpPath, nameof(ytDlpPath));
        FfmpegPath = ValidateExecutablePath(ffmpegPath, nameof(ffmpegPath));
        FfprobePath = ValidateExecutablePath(ffprobePath, nameof(ffprobePath));
        OutputDirectory = ValidateAbsolutePath(outputDirectory, nameof(outputDirectory));

        if (!string.Equals(
                Path.GetDirectoryName(FfmpegPath),
                Path.GetDirectoryName(FfprobePath),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(null, nameof(ffprobePath));
        }

        MetadataTimeout = ValidateTimeout(
            metadataTimeout ?? TimeSpan.FromMinutes(2),
            nameof(metadataTimeout));
        DownloadTimeout = ValidateTimeout(
            downloadTimeout ?? TimeSpan.FromHours(12),
            nameof(downloadTimeout));
    }

    public string YtDlpPath { get; }

    public string FfmpegPath { get; }

    public string FfprobePath { get; }

    public string OutputDirectory { get; }

    public TimeSpan MetadataTimeout { get; }

    public TimeSpan DownloadTimeout { get; }

    public static ExternalToolOptions CreateDefault(string outputDirectory)
    {
        var toolsDirectory = Path.Combine(AppContext.BaseDirectory, "tools");
        return new ExternalToolOptions(
            Path.Combine(toolsDirectory, ExternalToolNames.YtDlp),
            Path.Combine(toolsDirectory, ExternalToolNames.Ffmpeg),
            Path.Combine(toolsDirectory, ExternalToolNames.Ffprobe),
            outputDirectory);
    }

    private static string ValidateExecutablePath(string path, string parameterName)
    {
        var validatedPath = ValidateAbsolutePath(path, parameterName);
        if (ExternalExecutablePolicy.IsBlockedShell(validatedPath))
        {
            throw new ArgumentException(null, parameterName);
        }

        return validatedPath;
    }

    private static string ValidateAbsolutePath(string path, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, parameterName);
        var trimmedPath = path.Trim();

        if (!Path.IsPathFullyQualified(trimmedPath))
        {
            throw new ArgumentException(null, parameterName);
        }

        return trimmedPath;
    }

    private static TimeSpan ValidateTimeout(TimeSpan timeout, string parameterName)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return timeout;
    }
}
