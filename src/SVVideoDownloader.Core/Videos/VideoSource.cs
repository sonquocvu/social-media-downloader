using System;
using System.Diagnostics.CodeAnalysis;
using SVVideoDownloader.Core.Validation;

namespace SVVideoDownloader.Core.Videos;

public sealed record VideoSource
{
    private VideoSource(Uri uri, SupportedPlatform platform)
    {
        Uri = uri;
        Platform = platform;
    }

    public Uri Uri { get; }

    public SupportedPlatform Platform { get; }

    /// <summary>
    /// Recognizes a supported public URL host. A successful result only confirms
    /// URL shape and host recognition; it does not guarantee that content exists,
    /// is public, is downloadable, or that the user has permission to download it.
    /// </summary>
    public static ValidationResult<VideoSource> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult<VideoSource>.Failure(
                new ValidationError(ValidationErrorCode.Required, "Url"));
        }

        if (!System.Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            return ValidationResult<VideoSource>.Failure(
                new ValidationError(ValidationErrorCode.MalformedUrl, "Url"));
        }

        if (!string.Equals(uri.Scheme, System.Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult<VideoSource>.Failure(
                new ValidationError(ValidationErrorCode.HttpsRequired, "Url"));
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return ValidationResult<VideoSource>.Failure(
                new ValidationError(ValidationErrorCode.CredentialsNotAllowed, "Url"));
        }

        var host = uri.IdnHost.TrimEnd('.');
        var platform = RecognizePlatform(host);
        if (platform is null)
        {
            return ValidationResult<VideoSource>.Failure(
                new ValidationError(ValidationErrorCode.UnsupportedHost, "Url"));
        }

        return ValidationResult<VideoSource>.Success(new VideoSource(uri, platform.Value));
    }

    public static bool TryCreate(
        string? value,
        [NotNullWhen(true)] out VideoSource? source)
    {
        var result = Create(value);
        source = result.Value;
        return result.IsSuccess;
    }

    private static SupportedPlatform? RecognizePlatform(string host)
    {
        if (IsHostOrSubdomain(host, "youtube.com") ||
            string.Equals(host, "youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            return SupportedPlatform.YouTube;
        }

        if (IsHostOrSubdomain(host, "tiktok.com"))
        {
            return SupportedPlatform.TikTok;
        }

        if (IsHostOrSubdomain(host, "facebook.com") ||
            string.Equals(host, "fb.watch", StringComparison.OrdinalIgnoreCase))
        {
            return SupportedPlatform.Facebook;
        }

        return null;
    }

    private static bool IsHostOrSubdomain(string host, string domain) =>
        string.Equals(host, domain, StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase);
}
