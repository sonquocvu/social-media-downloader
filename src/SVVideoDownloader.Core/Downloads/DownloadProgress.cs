using System;
using System.Collections.Generic;
using SVVideoDownloader.Core.Validation;

namespace SVVideoDownloader.Core.Downloads;

public sealed record DownloadProgress
{
    private DownloadProgress(
        double? percentage,
        long downloadedBytes,
        long? totalBytes,
        double? bytesPerSecond)
    {
        Percentage = percentage;
        DownloadedBytes = downloadedBytes;
        TotalBytes = totalBytes;
        BytesPerSecond = bytesPerSecond;
    }

    public static DownloadProgress Empty { get; } = new(null, 0, null, null);

    public double? Percentage { get; }

    public long DownloadedBytes { get; }

    public long? TotalBytes { get; }

    public double? BytesPerSecond { get; }

    public static ValidationResult<DownloadProgress> Create(
        double? percentage,
        long downloadedBytes,
        long? totalBytes = null,
        double? bytesPerSecond = null)
    {
        var errors = new List<ValidationError>();

        if (percentage is { } knownPercentage &&
            (!double.IsFinite(knownPercentage) || knownPercentage is < 0 or > 100))
        {
            errors.Add(new ValidationError(ValidationErrorCode.ValueOutOfRange, "Percentage"));
        }

        if (downloadedBytes < 0)
        {
            errors.Add(new ValidationError(ValidationErrorCode.ValueOutOfRange, "DownloadedBytes"));
        }

        if (totalBytes is < 0)
        {
            errors.Add(new ValidationError(ValidationErrorCode.ValueOutOfRange, "TotalBytes"));
        }

        if (bytesPerSecond is { } knownSpeed &&
            (!double.IsFinite(knownSpeed) || knownSpeed < 0))
        {
            errors.Add(new ValidationError(ValidationErrorCode.ValueOutOfRange, "BytesPerSecond"));
        }

        if (errors.Count > 0)
        {
            return ValidationResult<DownloadProgress>.Failure(errors);
        }

        return ValidationResult<DownloadProgress>.Success(
            new DownloadProgress(percentage, downloadedBytes, totalBytes, bytesPerSecond));
    }
}
