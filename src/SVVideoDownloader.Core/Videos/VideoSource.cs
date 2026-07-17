using System.Diagnostics.CodeAnalysis;

namespace SVVideoDownloader.Core.Videos;

public sealed record VideoSource(Uri Uri, SupportedPlatform Platform)
{
    public static bool TryCreate(
        string? value,
        [NotNullWhen(true)] out VideoSource? source)
    {
        source = null;

        if (!System.Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        var host = uri.IdnHost.TrimEnd('.');
        var platform = GetPlatform(host);
        if (platform is null)
        {
            return false;
        }

        source = new VideoSource(uri, platform.Value);
        return true;
    }

    private static SupportedPlatform? GetPlatform(string host)
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
