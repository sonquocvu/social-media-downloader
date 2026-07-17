using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Infrastructure.Processes;

namespace SVVideoDownloader.Infrastructure.ExternalTools;

internal sealed class ExternalToolVerifier(IProcessRunner processRunner)
{
    private static readonly TimeSpan VersionTimeout = TimeSpan.FromSeconds(10);

    public Task<MediaOperationError?> VerifyYtDlpAsync(
        string executablePath,
        CancellationToken cancellationToken) =>
        VerifyAsync(
            executablePath,
            "--version",
            MediaComponent.MetadataExtractor,
            IsYtDlpVersion,
            cancellationToken);

    public Task<MediaOperationError?> VerifyFfmpegAsync(
        string executablePath,
        CancellationToken cancellationToken) =>
        VerifyAsync(
            executablePath,
            "-version",
            MediaComponent.MediaProcessor,
            output => output.StartsWith("ffmpeg version ", StringComparison.OrdinalIgnoreCase),
            cancellationToken);

    public Task<MediaOperationError?> VerifyFfprobeAsync(
        string executablePath,
        CancellationToken cancellationToken) =>
        VerifyAsync(
            executablePath,
            "-version",
            MediaComponent.MediaProbe,
            output => output.StartsWith("ffprobe version ", StringComparison.OrdinalIgnoreCase),
            cancellationToken);

    private async Task<MediaOperationError?> VerifyAsync(
        string executablePath,
        string versionArgument,
        MediaComponent component,
        Func<string, bool> isExpectedVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new ProcessRequest(
                executablePath,
                new[] { versionArgument },
                VersionTimeout,
                maximumCapturedCharacters: 16 * 1024);
            var result = await processRunner
                .RunAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var versionOutput = FirstNonEmptyLine(result.StandardOutput, result.StandardError);
            if (result.ExitCode != 0 ||
                versionOutput is null ||
                !isExpectedVersion(versionOutput))
            {
                return new MediaOperationError(
                    MediaErrorCategory.DependencyInvalid,
                    component);
            }

            return null;
        }
        catch (ExternalProcessStartException exception)
        {
            return new MediaOperationError(MapStartFailure(exception.Kind), component);
        }
        catch (ExternalProcessTimeoutException)
        {
            return new MediaOperationError(MediaErrorCategory.TimedOut, component);
        }
    }

    private static bool IsYtDlpVersion(string output)
    {
        var versionPart = output.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        var segments = versionPart.Split('.');
        return segments.Length >= 3 &&
            segments.Take(3).All(
                segment => int.TryParse(
                    segment,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out _));
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

    private static MediaErrorCategory MapStartFailure(ProcessStartFailureKind kind) => kind switch
    {
        ProcessStartFailureKind.Missing => MediaErrorCategory.DependencyMissing,
        ProcessStartFailureKind.Inaccessible => MediaErrorCategory.DependencyInaccessible,
        ProcessStartFailureKind.InvalidExecutable => MediaErrorCategory.DependencyInvalid,
        ProcessStartFailureKind.Unknown => MediaErrorCategory.DependencyInvalid,
        _ => MediaErrorCategory.DependencyInvalid,
    };
}
