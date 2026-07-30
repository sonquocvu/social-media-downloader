using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Validation;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.Core.Tests;

public sealed class QualityPresetTests
{
    [Theory]
    [InlineData(QualityPreset.Best, null, false)]
    [InlineData(QualityPreset.Video1080p, 1080, false)]
    [InlineData(QualityPreset.Video720p, 720, false)]
    [InlineData(QualityPreset.Video480p, 480, false)]
    [InlineData(QualityPreset.AudioMp3, null, true)]
    public void PresetsExposeExpectedConstraints(
        QualityPreset preset,
        int? maximumHeight,
        bool audioOnly)
    {
        Assert.Equal(maximumHeight, preset.GetMaximumHeight());
        Assert.Equal(audioOnly, preset.IsAudioOnly());
    }

    [Fact]
    public void DownloadOptionsStoresPresetAndSanitizedFileName()
    {
        var result = DownloadOptions.Create(QualityPreset.Video1080p, "Bản quay: 1080p.mp4");

        Assert.True(result.IsSuccess);
        var options = Assert.IsType<DownloadOptions>(result.Value);
        Assert.Equal(DownloadMediaFormat.VideoMp4, options.MediaFormat);
        Assert.Equal(QualityPreset.Video1080p, options.QualityPreset);
        Assert.Equal("Bản quay_ 1080p.mp4", options.OutputFileName);
    }

    [Theory]
    [InlineData(DownloadMediaFormat.VideoMp4, QualityPreset.Best)]
    [InlineData(DownloadMediaFormat.VideoOriginal, QualityPreset.Video1080p)]
    [InlineData(DownloadMediaFormat.AudioMp3, QualityPreset.AudioMp3)]
    public void DownloadOptionsStoresCompatibleMediaFormat(
        DownloadMediaFormat mediaFormat,
        QualityPreset qualityPreset)
    {
        var result = DownloadOptions.Create(mediaFormat, qualityPreset, "Tệp của tôi");

        Assert.True(result.IsSuccess);
        Assert.Equal(mediaFormat, result.Value!.MediaFormat);
        Assert.Equal(qualityPreset, result.Value.QualityPreset);
    }

    [Theory]
    [InlineData(DownloadMediaFormat.VideoMp4, QualityPreset.AudioMp3)]
    [InlineData(DownloadMediaFormat.VideoOriginal, QualityPreset.AudioMp3)]
    [InlineData(DownloadMediaFormat.AudioMp3, QualityPreset.Best)]
    public void DownloadOptionsRejectsIncompatibleMediaFormatAndQuality(
        DownloadMediaFormat mediaFormat,
        QualityPreset qualityPreset)
    {
        var result = DownloadOptions.Create(mediaFormat, qualityPreset, "video");

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorCode.InvalidValue, error.Code);
        Assert.Equal("MediaFormat", error.Field);
    }

    [Fact]
    public void DownloadOptionsRejectsUnknownMediaFormat()
    {
        var result = DownloadOptions.Create(
            (DownloadMediaFormat)999,
            QualityPreset.Best,
            "video");

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorCode.InvalidValue, error.Code);
        Assert.Equal("MediaFormat", error.Field);
    }

    [Fact]
    public void DownloadOptionsRejectsUnknownPreset()
    {
        var result = DownloadOptions.Create((QualityPreset)999, "video.mp4");

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorCode.InvalidValue, error.Code);
        Assert.Equal("QualityPreset", error.Field);
    }

    [Fact]
    public void UnknownPresetCannotBeMappedToConstraints()
    {
        var unknownPreset = (QualityPreset)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => unknownPreset.GetMaximumHeight());
        Assert.Throws<ArgumentOutOfRangeException>(() => unknownPreset.IsAudioOnly());
    }
}
