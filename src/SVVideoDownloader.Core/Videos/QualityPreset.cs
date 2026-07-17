using System;

namespace SVVideoDownloader.Core.Videos;

public enum QualityPreset
{
    Best,
    Video1080p,
    Video720p,
    Video480p,
    AudioMp3,
}

public static class QualityPresetExtensions
{
    public static int? GetMaximumHeight(this QualityPreset preset) => preset switch
    {
        QualityPreset.Best => null,
        QualityPreset.Video1080p => 1080,
        QualityPreset.Video720p => 720,
        QualityPreset.Video480p => 480,
        QualityPreset.AudioMp3 => null,
        _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null),
    };

    public static bool IsAudioOnly(this QualityPreset preset) => preset switch
    {
        QualityPreset.Best => false,
        QualityPreset.Video1080p => false,
        QualityPreset.Video720p => false,
        QualityPreset.Video480p => false,
        QualityPreset.AudioMp3 => true,
        _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null),
    };
}
