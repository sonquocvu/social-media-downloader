using System;
using System.Threading;
using System.Threading.Tasks;
using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Core.Videos;
using SVVideoDownloader.Infrastructure.ExternalTools;
using SVVideoDownloader.Infrastructure.Processes;

namespace SVVideoDownloader.Infrastructure.YtDlp;

public sealed class YtDlpMediaService : IVideoMetadataProvider, IVideoDownloadService
{
    private readonly ExternalToolOptions _options;
    private readonly IProcessRunner _processRunner;
    private readonly ExternalToolVerifier _toolVerifier;
    private readonly YtDlpMetadataParser _metadataParser = new();
    private readonly YtDlpProgressParser _progressParser = new();
    private readonly YtDlpOutputParser _outputParser = new();

    public YtDlpMediaService(ExternalToolOptions options)
        : this(options, new SystemProcessRunner())
    {
    }

    public YtDlpMediaService(
        ExternalToolOptions options,
        IProcessRunner processRunner)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _toolVerifier = new ExternalToolVerifier(processRunner);
    }

    public async Task<MediaOperationResult<VideoInfo>> GetVideoInfoAsync(
        VideoSource source,
        CancellationToken cancellationToken = default)
    {
        if (source is null)
        {
            return MediaOperationResult<VideoInfo>.Failure(
                new MediaOperationError(
                    MediaErrorCategory.InvalidRequest,
                    MediaComponent.Source));
        }

        var toolError = await _toolVerifier
            .VerifyYtDlpAsync(_options.YtDlpPath, cancellationToken)
            .ConfigureAwait(false);
        if (toolError is not null)
        {
            return MediaOperationResult<VideoInfo>.Failure(toolError);
        }

        try
        {
            var processResult = await _processRunner
                .RunAsync(
                    YtDlpCommandBuilder.BuildMetadataRequest(_options, source),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (processResult.ExitCode != 0)
            {
                return MediaOperationResult<VideoInfo>.Failure(
                    new MediaOperationError(
                        MediaErrorCategory.SourceUnavailable,
                        MediaComponent.Source));
            }

            return _metadataParser.Parse(source, processResult.StandardOutput);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ExternalProcessTimeoutException)
        {
            return MediaOperationResult<VideoInfo>.Failure(
                new MediaOperationError(
                    MediaErrorCategory.TimedOut,
                    MediaComponent.MetadataExtractor));
        }
        catch (ExternalProcessStartException exception)
        {
            return MediaOperationResult<VideoInfo>.Failure(
                MapStartFailure(exception.Kind, MediaComponent.MetadataExtractor));
        }
        catch (Exception)
        {
            return MediaOperationResult<VideoInfo>.Failure(
                new MediaOperationError(
                    MediaErrorCategory.ExecutionFailed,
                    MediaComponent.MetadataExtractor));
        }
    }

    public async Task<MediaOperationResult<DownloadResult>> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return MediaOperationResult<DownloadResult>.Failure(
                new MediaOperationError(
                    MediaErrorCategory.InvalidRequest,
                    MediaComponent.Source));
        }

        var toolError = await VerifyDownloadToolsAsync(cancellationToken).ConfigureAwait(false);
        if (toolError is not null)
        {
            return MediaOperationResult<DownloadResult>.Failure(toolError);
        }

        try
        {
            DownloadResult? downloadResult = null;
            var processResult = await _processRunner
                .RunAsync(
                    YtDlpCommandBuilder.BuildDownloadRequest(_options, request),
                    line => HandleDownloadOutput(line, progress, ref downloadResult),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (processResult.ExitCode != 0)
            {
                return MediaOperationResult<DownloadResult>.Failure(
                    new MediaOperationError(
                        MediaErrorCategory.ExecutionFailed,
                        MediaComponent.MetadataExtractor));
            }

            return downloadResult is not null
                ? MediaOperationResult<DownloadResult>.Success(downloadResult)
                : MediaOperationResult<DownloadResult>.Failure(
                    new MediaOperationError(
                        MediaErrorCategory.InvalidResponse,
                        MediaComponent.MetadataExtractor));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ExternalProcessTimeoutException)
        {
            return MediaOperationResult<DownloadResult>.Failure(
                new MediaOperationError(
                    MediaErrorCategory.TimedOut,
                    MediaComponent.MetadataExtractor));
        }
        catch (ExternalProcessStartException exception)
        {
            return MediaOperationResult<DownloadResult>.Failure(
                MapStartFailure(exception.Kind, MediaComponent.MetadataExtractor));
        }
        catch (Exception)
        {
            return MediaOperationResult<DownloadResult>.Failure(
                new MediaOperationError(
                    MediaErrorCategory.ExecutionFailed,
                    MediaComponent.MetadataExtractor));
        }
    }

    private async Task<MediaOperationError?> VerifyDownloadToolsAsync(
        CancellationToken cancellationToken)
    {
        var ytDlpError = await _toolVerifier
            .VerifyYtDlpAsync(_options.YtDlpPath, cancellationToken)
            .ConfigureAwait(false);
        if (ytDlpError is not null)
        {
            return ytDlpError;
        }

        var ffmpegError = await _toolVerifier
            .VerifyFfmpegAsync(_options.FfmpegPath, cancellationToken)
            .ConfigureAwait(false);
        if (ffmpegError is not null)
        {
            return ffmpegError;
        }

        return await _toolVerifier
            .VerifyFfprobeAsync(_options.FfprobePath, cancellationToken)
            .ConfigureAwait(false);
    }

    private void HandleDownloadOutput(
        string line,
        IProgress<DownloadProgress>? progress,
        ref DownloadResult? downloadResult)
    {
        if (progress is not null &&
            _progressParser.TryParse(line, out var parsedProgress) &&
            parsedProgress is not null)
        {
            progress.Report(parsedProgress);
        }

        if (_outputParser.TryParse(line, _options.OutputDirectory, out var parsedResult))
        {
            downloadResult = parsedResult;
        }
    }

    private static MediaOperationError MapStartFailure(
        ProcessStartFailureKind kind,
        MediaComponent component) =>
        new(
            kind switch
            {
                ProcessStartFailureKind.Missing => MediaErrorCategory.DependencyMissing,
                ProcessStartFailureKind.Inaccessible => MediaErrorCategory.DependencyInaccessible,
                ProcessStartFailureKind.InvalidExecutable => MediaErrorCategory.DependencyInvalid,
                ProcessStartFailureKind.Unknown => MediaErrorCategory.ExecutionFailed,
                _ => MediaErrorCategory.ExecutionFailed,
            },
            component);
}
