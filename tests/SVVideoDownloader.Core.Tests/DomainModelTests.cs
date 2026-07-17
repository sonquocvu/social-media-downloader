using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Validation;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.Core.Tests;

public sealed class DomainModelTests
{
    [Fact]
    public void VideoFormatNormalizesExtensionAndStoresMetadata()
    {
        var result = VideoFormat.Create("137", ".mp4", true, false, 1920, 1080, 10_000);

        Assert.True(result.IsSuccess);
        var format = Assert.IsType<VideoFormat>(result.Value);
        Assert.Equal("137", format.Id);
        Assert.Equal("mp4", format.FileExtension);
        Assert.True(format.HasVideo);
        Assert.False(format.HasAudio);
        Assert.Equal(1920, format.Width);
        Assert.Equal(1080, format.Height);
        Assert.Equal(10_000, format.EstimatedSizeBytes);
    }

    [Fact]
    public void VideoFormatReturnsAllApplicableValidationErrors()
    {
        var result = VideoFormat.Create(null, "m-p4", false, false, 0, -1, -1);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors,
            error => AssertError(error, ValidationErrorCode.Required, "FormatId"),
            error => AssertError(error, ValidationErrorCode.InvalidValue, "FileExtension"),
            error => AssertError(error, ValidationErrorCode.InvalidValue, "MediaStreams"),
            error => AssertError(error, ValidationErrorCode.ValueOutOfRange, "Width"),
            error => AssertError(error, ValidationErrorCode.ValueOutOfRange, "Height"),
            error => AssertError(error, ValidationErrorCode.ValueOutOfRange, "EstimatedSizeBytes"));
    }

    [Fact]
    public void VideoInfoDefensivelyCopiesFormats()
    {
        var source = CreateSource();
        var format = CreateFormat();
        var formats = new List<VideoFormat> { format };

        var result = VideoInfo.Create(source, " Video do tôi sở hữu ", " Tác giả ", TimeSpan.FromMinutes(1), formats);
        formats.Clear();

        Assert.True(result.IsSuccess);
        var info = Assert.IsType<VideoInfo>(result.Value);
        Assert.Equal("Video do tôi sở hữu", info.Title);
        Assert.Equal("Tác giả", info.Author);
        Assert.Single(info.Formats);
    }

    [Fact]
    public void VideoInfoRejectsMissingDataAndNegativeDuration()
    {
        var result = VideoInfo.Create(null, " ", null, TimeSpan.FromSeconds(-1), null);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors,
            error => AssertError(error, ValidationErrorCode.Required, "Source"),
            error => AssertError(error, ValidationErrorCode.Required, "Title"),
            error => AssertError(error, ValidationErrorCode.ValueOutOfRange, "Duration"),
            error => AssertError(error, ValidationErrorCode.Required, "Formats"));
    }

    [Fact]
    public void DownloadRequestRequiresSourceOptionsAndRightsConfirmation()
    {
        var result = DownloadRequest.Create(null, null, false);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors,
            error => AssertError(error, ValidationErrorCode.Required, "Source"),
            error => AssertError(error, ValidationErrorCode.Required, "Options"),
            error => AssertError(
                error,
                ValidationErrorCode.RightsConfirmationRequired,
                "RightsConfirmed"));
    }

    [Fact]
    public void DownloadRequestStoresValidatedInputs()
    {
        var source = CreateSource();
        var options = CreateOptions();

        var result = DownloadRequest.Create(source, options, true);

        Assert.True(result.IsSuccess);
        var request = Assert.IsType<DownloadRequest>(result.Value);
        Assert.Same(source, request.Source);
        Assert.Same(options, request.Options);
        Assert.True(request.RightsConfirmed);
    }

    [Fact]
    public void DownloadTaskStartsPendingWithEmptyProgress()
    {
        var request = Assert.IsType<DownloadRequest>(
            DownloadRequest.Create(CreateSource(), CreateOptions(), true).Value);
        var id = Guid.NewGuid();

        var result = DownloadTask.Create(id, request);

        Assert.True(result.IsSuccess);
        var task = Assert.IsType<DownloadTask>(result.Value);
        Assert.Equal(id, task.Id);
        Assert.Equal(DownloadStatus.Pending, task.Status);
        Assert.Same(DownloadProgress.Empty, task.Progress);
    }

    [Theory]
    [InlineData(null, 0, null, null)]
    [InlineData(0.0, 0L, 0L, 0.0)]
    [InlineData(100.0, 1_000L, 1_000L, 250.0)]
    public void DownloadProgressAcceptsValidKnownAndIndeterminateValues(
        double? percentage,
        long downloadedBytes,
        long? totalBytes,
        double? bytesPerSecond)
    {
        var result = DownloadProgress.Create(
            percentage,
            downloadedBytes,
            totalBytes,
            bytesPerSecond);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(-0.1, 0, null, null, "Percentage")]
    [InlineData(100.1, 0, null, null, "Percentage")]
    [InlineData(50.0, -1L, null, null, "DownloadedBytes")]
    [InlineData(50.0, 0L, -1L, null, "TotalBytes")]
    [InlineData(50.0, 0L, 100L, -1.0, "BytesPerSecond")]
    public void DownloadProgressRejectsOutOfRangeValues(
        double? percentage,
        long downloadedBytes,
        long? totalBytes,
        double? bytesPerSecond,
        string expectedField)
    {
        var result = DownloadProgress.Create(
            percentage,
            downloadedBytes,
            totalBytes,
            bytesPerSecond);

        Assert.False(result.IsSuccess);
        AssertError(
            Assert.Single(result.Errors),
            ValidationErrorCode.ValueOutOfRange,
            expectedField);
    }

    [Fact]
    public void DownloadProgressRejectsNonFiniteNumbers()
    {
        var percentageResult = DownloadProgress.Create(double.NaN, 0);
        var speedResult = DownloadProgress.Create(null, 0, null, double.PositiveInfinity);

        AssertError(
            Assert.Single(percentageResult.Errors),
            ValidationErrorCode.ValueOutOfRange,
            "Percentage");
        AssertError(
            Assert.Single(speedResult.Errors),
            ValidationErrorCode.ValueOutOfRange,
            "BytesPerSecond");
    }

    [Fact]
    public void DownloadTaskRejectsEmptyId()
    {
        var request = Assert.IsType<DownloadRequest>(
            DownloadRequest.Create(CreateSource(), CreateOptions(), true).Value);

        var result = DownloadTask.Create(Guid.Empty, request);

        Assert.False(result.IsSuccess);
        AssertError(Assert.Single(result.Errors), ValidationErrorCode.Required, "TaskId");
    }

    private static VideoSource CreateSource() =>
        Assert.IsType<VideoSource>(VideoSource.Create("https://youtu.be/owned-video").Value);

    private static VideoFormat CreateFormat() =>
        Assert.IsType<VideoFormat>(VideoFormat.Create("best", "mp4", true, true).Value);

    private static DownloadOptions CreateOptions() =>
        Assert.IsType<DownloadOptions>(DownloadOptions.Create(QualityPreset.Best, "video.mp4").Value);

    private static void AssertError(
        ValidationError error,
        ValidationErrorCode expectedCode,
        string expectedField)
    {
        Assert.Equal(expectedCode, error.Code);
        Assert.Equal(expectedField, error.Field);
    }
}
