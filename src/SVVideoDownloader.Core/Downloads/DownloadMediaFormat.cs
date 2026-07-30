using System;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.Core.Downloads;

public enum DownloadMediaFormat
{
    VideoMp4,
    VideoOriginal,
    AudioMp3,
}

public static class DownloadMediaFormatExtensions
{
    public static bool IsVideo(this DownloadMediaFormat format) => format switch
    {
        DownloadMediaFormat.VideoMp4 => true,
        DownloadMediaFormat.VideoOriginal => true,
        DownloadMediaFormat.AudioMp3 => false,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
    };

    public static bool IsCompatibleWith(
        this DownloadMediaFormat format,
        QualityPreset qualityPreset) => format switch
    {
        DownloadMediaFormat.VideoMp4 or DownloadMediaFormat.VideoOriginal =>
            qualityPreset is QualityPreset.Best or
                QualityPreset.Video1080p or
                QualityPreset.Video720p or
                QualityPreset.Video480p,
        DownloadMediaFormat.AudioMp3 => qualityPreset == QualityPreset.AudioMp3,
        _ => false,
    };
}
