using System;
using System.Collections.Generic;
using SVVideoDownloader.Core.Validation;

namespace SVVideoDownloader.Core.Videos;

public sealed record VideoFormat
{
    private VideoFormat(
        string id,
        string fileExtension,
        bool hasVideo,
        bool hasAudio,
        int? width,
        int? height,
        long? estimatedSizeBytes)
    {
        Id = id;
        FileExtension = fileExtension;
        HasVideo = hasVideo;
        HasAudio = hasAudio;
        Width = width;
        Height = height;
        EstimatedSizeBytes = estimatedSizeBytes;
    }

    public string Id { get; }

    public string FileExtension { get; }

    public bool HasVideo { get; }

    public bool HasAudio { get; }

    public int? Width { get; }

    public int? Height { get; }

    public long? EstimatedSizeBytes { get; }

    public static ValidationResult<VideoFormat> Create(
        string? id,
        string? fileExtension,
        bool hasVideo,
        bool hasAudio,
        int? width = null,
        int? height = null,
        long? estimatedSizeBytes = null)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(id))
        {
            errors.Add(new ValidationError(ValidationErrorCode.Required, "FormatId"));
        }

        var normalizedExtension = fileExtension?.Trim().TrimStart('.');
        if (string.IsNullOrWhiteSpace(normalizedExtension))
        {
            errors.Add(new ValidationError(ValidationErrorCode.Required, "FileExtension"));
        }
        else if (!IsValidExtension(normalizedExtension))
        {
            errors.Add(new ValidationError(ValidationErrorCode.InvalidValue, "FileExtension"));
        }

        if (!hasVideo && !hasAudio)
        {
            errors.Add(new ValidationError(ValidationErrorCode.InvalidValue, "MediaStreams"));
        }

        if (width is <= 0)
        {
            errors.Add(new ValidationError(ValidationErrorCode.ValueOutOfRange, "Width"));
        }

        if (height is <= 0)
        {
            errors.Add(new ValidationError(ValidationErrorCode.ValueOutOfRange, "Height"));
        }

        if (estimatedSizeBytes is < 0)
        {
            errors.Add(new ValidationError(ValidationErrorCode.ValueOutOfRange, "EstimatedSizeBytes"));
        }

        if (errors.Count > 0)
        {
            return ValidationResult<VideoFormat>.Failure(errors);
        }

        return ValidationResult<VideoFormat>.Success(
            new VideoFormat(
                id!.Trim(),
                normalizedExtension!,
                hasVideo,
                hasAudio,
                width,
                height,
                estimatedSizeBytes));
    }

    private static bool IsValidExtension(string value)
    {
        foreach (var character in value)
        {
            if (!char.IsAsciiLetterOrDigit(character))
            {
                return false;
            }
        }

        return true;
    }
}
