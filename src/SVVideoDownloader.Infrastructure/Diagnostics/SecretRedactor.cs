using System.Text.RegularExpressions;

namespace SVVideoDownloader.Infrastructure.Diagnostics;

internal static class SecretRedactor
{
    private const string HiddenValue = "[REDACTED]";
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    private static readonly Regex HeaderOrAssignment = new(
        @"(?i)\b(cookie|set-cookie|authorization|proxy-authorization|password|passwd|token|secret|api[_-]?key)\b\s*[:=]\s*([^\s;,]+|""[^""]*"")",
        RegexOptions.CultureInvariant,
        MatchTimeout);

    private static readonly Regex QueryValue = new(
        @"(?i)([?&](?:cookie|password|passwd|token|secret|api[_-]?key)=)[^&\s]+",
        RegexOptions.CultureInvariant,
        MatchTimeout);

    private static readonly Regex BearerValue = new(
        @"(?i)\bBearer\s+[A-Za-z0-9._~+/-]+=*",
        RegexOptions.CultureInvariant,
        MatchTimeout);

    private static readonly Regex CookieOption = new(
        @"(?i)(--cookies(?:-from-browser)?\s+)(?:""[^""]*""|\S+)",
        RegexOptions.CultureInvariant,
        MatchTimeout);

    private static readonly Regex WebUrl = new(
        @"(?i)https?://[^\s""']+",
        RegexOptions.CultureInvariant,
        MatchTimeout);

    public static string Redact(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        try
        {
            var redacted = BearerValue.Replace(value, $"Bearer {HiddenValue}");
            redacted = HeaderOrAssignment.Replace(
                redacted,
                match => $"{match.Groups[1].Value}={HiddenValue}");
            redacted = QueryValue.Replace(
                redacted,
                match => $"{match.Groups[1].Value}{HiddenValue}");
            redacted = CookieOption.Replace(
                redacted,
                match => $"{match.Groups[1].Value}{HiddenValue}");
            return WebUrl.Replace(redacted, "[URL_REDACTED]");
        }
        catch (RegexMatchTimeoutException)
        {
            return HiddenValue;
        }
    }
}
