using SVVideoDownloader.Core.Videos;
using SVVideoDownloader.Infrastructure.YtDlp;

namespace SVVideoDownloader.Infrastructure.Tests;

public sealed class YtDlpCommandBuilderTests
{
    [Fact]
    public void MetadataRequestUsesSingleVideoJsonAndSeparateUrlArgument()
    {
        const string url = "https://www.youtube.com/watch?v=owned-video&list=playlist-id";

        var request = YtDlpCommandBuilder.BuildMetadataRequest(
            TestData.CreateOptions(),
            TestData.CreateSource(url));

        Assert.Equal(TestData.YtDlpPath, request.ExecutablePath);
        Assert.Equal(
            new[]
            {
                "--ignore-config",
                "--dump-single-json",
                "--skip-download",
                "--no-playlist",
                "--no-warnings",
                "--no-colors",
                "--",
                url,
            },
            request.ArgumentList);
        Assert.Equal(TimeSpan.FromSeconds(30), request.Timeout);
    }

    [Theory]
    [InlineData(QualityPreset.Best, "bestvideo*+bestaudio/best")]
    [InlineData(
        QualityPreset.Video1080p,
        "bestvideo*[height<=1080]+bestaudio/best[height<=1080]")]
    [InlineData(
        QualityPreset.Video720p,
        "bestvideo*[height<=720]+bestaudio/best[height<=720]")]
    [InlineData(
        QualityPreset.Video480p,
        "bestvideo*[height<=480]+bestaudio/best[height<=480]")]
    [InlineData(QualityPreset.AudioMp3, "bestaudio/best")]
    public void DownloadRequestMapsQualityPresetToExplicitFormatSelector(
        QualityPreset preset,
        string expectedSelector)
    {
        var request = YtDlpCommandBuilder.BuildDownloadRequest(
            TestData.CreateOptions(),
            TestData.CreateRequest(preset));
        var arguments = request.ArgumentList.ToList();

        var formatIndex = arguments.IndexOf("--format");
        Assert.True(formatIndex >= 0);
        Assert.Equal(expectedSelector, arguments[formatIndex + 1]);
        Assert.Equal(preset == QualityPreset.AudioMp3, arguments.Contains("--extract-audio"));
        Assert.Equal(preset == QualityPreset.AudioMp3, arguments.Contains("--audio-format"));
        Assert.Equal(preset == QualityPreset.AudioMp3, arguments.Contains("--audio-quality"));
    }

    [Fact]
    public void DownloadRequestUsesMachineProgressAndSafeMvpFlags()
    {
        const string url = "https://www.youtube.com/watch?v=owned-video&list=playlist-id";
        var request = YtDlpCommandBuilder.BuildDownloadRequest(
            TestData.CreateOptions(),
            TestData.CreateRequest(url: url));
        var arguments = request.ArgumentList.ToList();

        Assert.Contains("--ignore-config", arguments);
        Assert.Contains("--no-playlist", arguments);
        Assert.Contains("--newline", arguments);
        Assert.Contains("--no-colors", arguments);
        Assert.Contains("--progress-template", arguments);
        Assert.Contains("download:SVVD_PROGRESS:%(progress)j", arguments);
        Assert.Contains("--print", arguments);
        Assert.Contains("after_move:SVVD_OUTPUT:%(filepath)j", arguments);
        Assert.Equal("--", arguments[^2]);
        Assert.Equal(url, arguments[^1]);
        Assert.Equal(1, arguments.Count(argument => argument == url));

        var ffmpegLocationIndex = arguments.IndexOf("--ffmpeg-location");
        Assert.Equal(@"C:\svvd-test\tools", arguments[ffmpegLocationIndex + 1]);

        var outputIndex = arguments.IndexOf("--output");
        Assert.Equal(
            @"C:\svvd-test\downloads\video của tôi.%(ext)s",
            arguments[outputIndex + 1]);

        var bannedFragments = new[]
        {
            "cookie",
            "password",
            "username",
            "netrc",
            "geo-bypass",
            "proxy",
            "allow-unplayable-formats",
        };
        Assert.DoesNotContain(
            arguments,
            argument => bannedFragments.Any(
                banned => argument.Contains(banned, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Mp3DownloadRequestExtractsBestAudioAndConvertsItToMp3()
    {
        var request = YtDlpCommandBuilder.BuildDownloadRequest(
            TestData.CreateOptions(),
            TestData.CreateRequest(QualityPreset.AudioMp3));
        var arguments = request.ArgumentList.ToList();

        var extractAudioIndex = arguments.IndexOf("--extract-audio");
        var audioFormatIndex = arguments.IndexOf("--audio-format");
        var audioQualityIndex = arguments.IndexOf("--audio-quality");

        Assert.True(extractAudioIndex >= 0);
        Assert.True(audioFormatIndex > extractAudioIndex);
        Assert.True(audioQualityIndex > audioFormatIndex);
        Assert.Equal("mp3", arguments[audioFormatIndex + 1]);
        Assert.Equal("0", arguments[audioQualityIndex + 1]);
        Assert.Equal(1, arguments.Count(argument => argument == "--extract-audio"));
        Assert.Equal(1, arguments.Count(argument => argument == "--audio-format"));
        Assert.Equal(1, arguments.Count(argument => argument == "--audio-quality"));
    }

    [Fact]
    public void DownloadRequestEscapesUserPercentAsLiteralOutputTemplateText()
    {
        var request = YtDlpCommandBuilder.BuildDownloadRequest(
            TestData.CreateOptions(),
            TestData.CreateRequest(outputFileName: "100% của tôi.mp4"));
        var arguments = request.ArgumentList.ToList();

        var outputIndex = arguments.IndexOf("--output");
        Assert.Equal(
            @"C:\svvd-test\downloads\100%% của tôi.%(ext)s",
            arguments[outputIndex + 1]);
    }
}
