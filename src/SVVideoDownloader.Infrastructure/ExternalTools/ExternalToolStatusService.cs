using System.Globalization;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Infrastructure.Processes;

namespace SVVideoDownloader.Infrastructure.ExternalTools;

public sealed class ExternalToolStatusService(
    ExternalToolOptions options,
    IProcessRunner processRunner) : IExternalToolStatusService
{
    private static readonly TimeSpan VersionTimeout = TimeSpan.FromSeconds(10);
    private readonly ExternalToolOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly IProcessRunner _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));

    public async Task<IReadOnlyList<ExternalToolStatus>> CheckAllAsync(
        CancellationToken cancellationToken = default)
    {
        var ytDlp = await CheckYtDlpAsync(_options.YtDlpPath, cancellationToken)
            .ConfigureAwait(false);
        var ffmpeg = await CheckAsync(
                ExternalToolKind.Ffmpeg,
                _options.FfmpegPath,
                "-version",
                "ffmpeg version ",
                cancellationToken)
            .ConfigureAwait(false);
        var ffprobe = await CheckAsync(
                ExternalToolKind.Ffprobe,
                _options.FfprobePath,
                "-version",
                "ffprobe version ",
                cancellationToken)
            .ConfigureAwait(false);
        return [ytDlp, ffmpeg, ffprobe];
    }

    public Task<ExternalToolStatus> CheckYtDlpAsync(
        string executablePath,
        CancellationToken cancellationToken = default) =>
        CheckAsync(
            ExternalToolKind.YtDlp,
            executablePath,
            "--version",
            expectedPrefix: null,
            cancellationToken);

    public Task<ExternalToolStatus> CheckFfmpegAsync(
        string executablePath,
        CancellationToken cancellationToken = default) =>
        CheckAsync(
            ExternalToolKind.Ffmpeg,
            executablePath,
            "-version",
            "ffmpeg version ",
            cancellationToken);

    public Task<ExternalToolStatus> CheckFfprobeAsync(
        string executablePath,
        CancellationToken cancellationToken = default) =>
        CheckAsync(
            ExternalToolKind.Ffprobe,
            executablePath,
            "-version",
            "ffprobe version ",
            cancellationToken);

    private async Task<ExternalToolStatus> CheckAsync(
        ExternalToolKind tool,
        string executablePath,
        string versionArgument,
        string? expectedPrefix,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _processRunner.RunAsync(
                    new ProcessRequest(
                        executablePath,
                        [versionArgument],
                        VersionTimeout,
                        maximumCapturedCharacters: 16 * 1024),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var firstLine = FirstNonEmptyLine(result.StandardOutput, result.StandardError);
            var version = ParseVersion(firstLine, expectedPrefix);
            return result.ExitCode == 0 && version is not null
                ? new ExternalToolStatus(tool, executablePath, true, version, null)
                : Unavailable(tool, executablePath, MediaErrorCategory.DependencyInvalid);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ExternalProcessStartException exception)
        {
            return Unavailable(tool, executablePath, MapStartFailure(exception.Kind));
        }
        catch (ExternalProcessTimeoutException)
        {
            return Unavailable(tool, executablePath, MediaErrorCategory.TimedOut);
        }
        catch (Exception)
        {
            return Unavailable(tool, executablePath, MediaErrorCategory.DependencyInvalid);
        }
    }

    private static string? ParseVersion(string? output, string? expectedPrefix)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        if (expectedPrefix is not null)
        {
            if (!output.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var remainder = output[expectedPrefix.Length..];
            return remainder.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

        var candidate = output.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        var segments = candidate.Split('.');
        return segments.Length >= 3 &&
            segments.Take(3).All(segment => int.TryParse(
                segment,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out _))
            ? candidate
            : null;
    }

    private static string? FirstNonEmptyLine(params string[] values)
    {
        foreach (var value in values)
        {
            using var reader = new StringReader(value);
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    return line.Trim();
                }
            }
        }

        return null;
    }

    private static ExternalToolStatus Unavailable(
        ExternalToolKind tool,
        string path,
        MediaErrorCategory category) =>
        new(tool, path, false, null, category);

    private static MediaErrorCategory MapStartFailure(ProcessStartFailureKind kind) =>
        kind switch
        {
            ProcessStartFailureKind.Missing => MediaErrorCategory.DependencyMissing,
            ProcessStartFailureKind.Inaccessible => MediaErrorCategory.DependencyInaccessible,
            ProcessStartFailureKind.InvalidExecutable => MediaErrorCategory.DependencyInvalid,
            _ => MediaErrorCategory.DependencyInvalid,
        };
}
