using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Videos;
using SVVideoDownloader.Infrastructure.ExternalTools;

namespace SVVideoDownloader.Infrastructure.Tests;

internal static class TestData
{
    public const string YtDlpPath = @"C:\svvd-test\tools\yt-dlp.exe";
    public const string FfmpegPath = @"C:\svvd-test\tools\ffmpeg.exe";
    public const string FfprobePath = @"C:\svvd-test\tools\ffprobe.exe";
    public const string OutputDirectory = @"C:\svvd-test\downloads";

    public static ExternalToolOptions CreateOptions() =>
        new(
            YtDlpPath,
            FfmpegPath,
            FfprobePath,
            OutputDirectory,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(5));

    public static VideoSource CreateSource(
        string url = "https://www.youtube.com/watch?v=owned-video") =>
        Assert.IsType<VideoSource>(VideoSource.Create(url).Value);

    public static DownloadRequest CreateRequest(
        QualityPreset preset = QualityPreset.Best,
        string outputFileName = "video của tôi.mp4",
        string url = "https://www.youtube.com/watch?v=owned-video")
    {
        var options = Assert.IsType<DownloadOptions>(
            DownloadOptions.Create(preset, outputFileName).Value);
        return Assert.IsType<DownloadRequest>(
            DownloadRequest.Create(CreateSource(url), options, true).Value);
    }
}
