using System;
using System.Collections.Generic;
using System.Linq;
using SVVideoDownloader.Core.Validation;

namespace SVVideoDownloader.Core.Videos;

public sealed record VideoInfo
{
    private VideoInfo(
        VideoSource source,
        string title,
        string? author,
        TimeSpan? duration,
        IReadOnlyList<VideoFormat> formats,
        Uri? thumbnailUri)
    {
        Source = source;
        Title = title;
        Author = author;
        Duration = duration;
        Formats = formats;
        ThumbnailUri = thumbnailUri;
    }

    public VideoSource Source { get; }

    public string Title { get; }

    public string? Author { get; }

    public TimeSpan? Duration { get; }

    public IReadOnlyList<VideoFormat> Formats { get; }

    public Uri? ThumbnailUri { get; }

    public static ValidationResult<VideoInfo> Create(
        VideoSource? source,
        string? title,
        string? author,
        TimeSpan? duration,
        IEnumerable<VideoFormat>? formats,
        Uri? thumbnailUri = null)
    {
        var errors = new List<ValidationError>();
        var formatArray = formats?.ToArray();

        if (source is null)
        {
            errors.Add(new ValidationError(ValidationErrorCode.Required, "Source"));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add(new ValidationError(ValidationErrorCode.Required, "Title"));
        }

        if (duration is { } knownDuration && knownDuration < TimeSpan.Zero)
        {
            errors.Add(new ValidationError(ValidationErrorCode.ValueOutOfRange, "Duration"));
        }

        if (formatArray is null || formatArray.Length == 0)
        {
            errors.Add(new ValidationError(ValidationErrorCode.Required, "Formats"));
        }

        if (errors.Count > 0)
        {
            return ValidationResult<VideoInfo>.Failure(errors);
        }

        return ValidationResult<VideoInfo>.Success(
            new VideoInfo(
                source!,
                title!.Trim(),
                string.IsNullOrWhiteSpace(author) ? null : author.Trim(),
                duration,
                Array.AsReadOnly(formatArray!),
                thumbnailUri));
    }
}
