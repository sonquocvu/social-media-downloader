using System;
using SVVideoDownloader.Core.Files;
using SVVideoDownloader.Core.Validation;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.Core.Downloads;

public sealed record DownloadOptions
{
    private DownloadOptions(QualityPreset qualityPreset, string outputFileName)
    {
        QualityPreset = qualityPreset;
        OutputFileName = outputFileName;
    }

    public QualityPreset QualityPreset { get; }

    public string OutputFileName { get; }

    public static ValidationResult<DownloadOptions> Create(
        QualityPreset qualityPreset,
        string? outputFileName)
    {
        if (!Enum.IsDefined(qualityPreset))
        {
            return ValidationResult<DownloadOptions>.Failure(
                new ValidationError(ValidationErrorCode.InvalidValue, "QualityPreset"));
        }

        var fileNameResult = WindowsFileNameSanitizer.Sanitize(outputFileName);
        if (!fileNameResult.IsSuccess)
        {
            return ValidationResult<DownloadOptions>.Failure(fileNameResult.Errors);
        }

        return ValidationResult<DownloadOptions>.Success(
            new DownloadOptions(qualityPreset, fileNameResult.Value!));
    }
}
