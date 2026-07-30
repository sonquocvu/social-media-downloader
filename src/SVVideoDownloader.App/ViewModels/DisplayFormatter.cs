using System.Globalization;
using SVVideoDownloader.Core.Downloads;
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

    public static string GetQualityName(QualityPreset quality) => quality switch
    {
        QualityPreset.Best => "Tốt nhất",
        QualityPreset.Video1080p => "Video 1080p",
        QualityPreset.Video720p => "Video 720p",
        QualityPreset.Video480p => "Video 480p",
        QualityPreset.AudioMp3 => "Âm thanh MP3",
        _ => "Không xác định",
    };

    public static string GetMediaFormatName(
        DownloadMediaFormat? format,
        QualityPreset quality) => format switch
    {
        DownloadMediaFormat.VideoMp4 => "MP4 tương thích",
        DownloadMediaFormat.VideoOriginal => "Chất lượng gốc tốt nhất",
        DownloadMediaFormat.AudioMp3 => "MP3 (âm thanh)",
        null when quality == QualityPreset.AudioMp3 => "MP3 (âm thanh)",
        null => "Video (không xác định)",
        _ => "Không xác định",
    };
}
