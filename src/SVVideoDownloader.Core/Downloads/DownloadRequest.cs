using System.Collections.Generic;
using SVVideoDownloader.Core.Validation;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.Core.Downloads;

public sealed record DownloadRequest
{
    private DownloadRequest(
        VideoSource source,
        DownloadOptions options,
        bool rightsConfirmed)
    {
        Source = source;
        Options = options;
        RightsConfirmed = rightsConfirmed;
    }

    public VideoSource Source { get; }

    public DownloadOptions Options { get; }

    public bool RightsConfirmed { get; }

    public static ValidationResult<DownloadRequest> Create(
        VideoSource? source,
        DownloadOptions? options,
        bool rightsConfirmed)
    {
        var errors = new List<ValidationError>();

        if (source is null)
        {
            errors.Add(new ValidationError(ValidationErrorCode.Required, "Source"));
        }

        if (options is null)
        {
            errors.Add(new ValidationError(ValidationErrorCode.Required, "Options"));
        }

        if (!rightsConfirmed)
        {
            errors.Add(
                new ValidationError(
                    ValidationErrorCode.RightsConfirmationRequired,
                    "RightsConfirmed"));
        }

        if (errors.Count > 0)
        {
            return ValidationResult<DownloadRequest>.Failure(errors);
        }

        return ValidationResult<DownloadRequest>.Success(
            new DownloadRequest(source!, options!, rightsConfirmed));
    }
}
