using SVVideoDownloader.Infrastructure.ExternalTools;

namespace SVVideoDownloader.Infrastructure.Tests;

public sealed class ExternalToolNamesTests
{
    [Fact]
    public void DefaultsUseExpectedWindowsExecutableNames()
    {
        Assert.Equal("yt-dlp.exe", ExternalToolNames.YtDlp);
        Assert.Equal("ffmpeg.exe", ExternalToolNames.Ffmpeg);
        Assert.Equal("ffprobe.exe", ExternalToolNames.Ffprobe);
    }
}
