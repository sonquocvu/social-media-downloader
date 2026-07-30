using System;
using SVVideoDownloader.Core.Files;
using SVVideoDownloader.Core.Validation;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.Core.Downloads;

public sealed record DownloadOptions
{
    private DownloadOptions(
        DownloadMediaFormat mediaFormat,
        QualityPreset qualityPreset,
        string outputFileName)
    {
        MediaFormat = mediaFormat;
        QualityPreset = qualityPreset;
        OutputFileName = outputFileName;
    }

    public DownloadMediaFormat MediaFormat { get; }

    public QualityPreset QualityPreset { get; }

    public string OutputFileName { get; }

    public static ValidationResult<DownloadOptions> Create(
        QualityPreset qualityPreset,
        string? outputFileName) =>
        Create(
            qualityPreset == QualityPreset.AudioMp3
                ? DownloadMediaFormat.AudioMp3
                : DownloadMediaFormat.VideoMp4,
            qualityPreset,
            outputFileName);

    public static ValidationResult<DownloadOptions> Create(
        DownloadMediaFormat mediaFormat,
        QualityPreset qualityPreset,
        string? outputFileName)
    {
        if (!Enum.IsDefined(mediaFormat))
        {
            return ValidationResult<DownloadOptions>.Failure(
                new ValidationError(ValidationErrorCode.InvalidValue, "MediaFormat"));
        }

        if (!Enum.IsDefined(qualityPreset))
        {
            return ValidationResult<DownloadOptions>.Failure(
                new ValidationError(ValidationErrorCode.InvalidValue, "QualityPreset"));
        }

        if (!mediaFormat.IsCompatibleWith(qualityPreset))
        {
            return ValidationResult<DownloadOptions>.Failure(
                new ValidationError(ValidationErrorCode.InvalidValue, "MediaFormat"));
        }

        var fileNameResult = WindowsFileNameSanitizer.Sanitize(outputFileName);
        if (!fileNameResult.IsSuccess)
        {
            return ValidationResult<DownloadOptions>.Failure(fileNameResult.Errors);
        }

        return ValidationResult<DownloadOptions>.Success(
            new DownloadOptions(mediaFormat, qualityPreset, fileNameResult.Value!));
    }
}
