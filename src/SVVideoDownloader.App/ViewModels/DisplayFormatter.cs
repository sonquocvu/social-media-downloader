using System.Globalization;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.App.ViewModels;

internal static class DisplayFormatter
{
    private static readonly CultureInfo VietnameseCulture =
        CultureInfo.GetCultureInfo("vi-VN");

    public static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)Math.Max(bytes, 0);
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value.ToString(value >= 100 || unitIndex == 0 ? "0" : "0.0", VietnameseCulture)} {units[unitIndex]}";
    }

    public static string FormatDuration(TimeSpan? duration)
    {
        if (duration is null)
        {
            return "Không xác định";
        }

        return duration.Value.TotalHours >= 1
            ? duration.Value.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
            : duration.Value.ToString(@"m\:ss", CultureInfo.InvariantCulture);
    }

    public static string FormatEta(TimeSpan eta) => eta.TotalHours >= 1
        ? eta.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
        : eta.ToString(@"m\:ss", CultureInfo.InvariantCulture);

    public static string GetPlatformName(SupportedPlatform platform) => platform switch
    {
        SupportedPlatform.YouTube => "YouTube",
        SupportedPlatform.TikTok => "TikTok",
        SupportedPlatform.Facebook => "Facebook",
        _ => "Không xác định",
    };
}
