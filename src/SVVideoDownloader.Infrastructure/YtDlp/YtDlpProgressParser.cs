using System;
using System.Globalization;
using System.Text.Json;
using SVVideoDownloader.Core.Downloads;

namespace SVVideoDownloader.Infrastructure.YtDlp;

internal sealed class YtDlpProgressParser
{
    public const string LinePrefix = "SVVD_PROGRESS:";

    public bool TryParse(string line, out DownloadProgress? progress)
    {
        progress = null;
        if (!line.StartsWith(LinePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(line[LinePrefix.Length..]);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var downloadedBytes = GetInt64(root, "downloaded_bytes") ?? 0;
            var totalBytes = GetInt64(root, "total_bytes") ??
                GetInt64(root, "total_bytes_estimate");
            var speed = GetDouble(root, "speed");
            var percentage = CalculatePercentage(downloadedBytes, totalBytes) ??
                GetPercentString(root);
            var result = DownloadProgress.Create(
                percentage,
                downloadedBytes,
                totalBytes,
                speed);

            progress = result.Value;
            return result.IsSuccess;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static double? CalculatePercentage(long downloadedBytes, long? totalBytes)
    {
        if (totalBytes is not > 0)
        {
            return null;
        }

        return Math.Clamp(downloadedBytes * 100d / totalBytes.Value, 0d, 100d);
    }

    private static double? GetPercentString(JsonElement root)
    {
        if (!root.TryGetProperty("_percent_str", out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = property.GetString()?.Trim().TrimEnd('%');
        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var percentage)
            ? percentage
            : null;
    }

    private static long? GetInt64(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Number ||
            !property.TryGetInt64(out var value))
        {
            return null;
        }

        return value;
    }

    private static double? GetDouble(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Number ||
            !property.TryGetDouble(out var value) ||
            !double.IsFinite(value))
        {
            return null;
        }

        return value;
    }
}
