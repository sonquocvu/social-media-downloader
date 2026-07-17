using SVVideoDownloader.Infrastructure.YtDlp;

namespace SVVideoDownloader.Infrastructure.Tests;

public sealed class YtDlpOutputParserTests
{
    [Fact]
    public void TryParseReturnsFileNameWithinExpectedDirectory()
    {
        const string line =
            "SVVD_OUTPUT:\"C:\\\\svvd-test\\\\downloads\\\\video của tôi.mp4\"";

        var parsed = new YtDlpOutputParser().TryParse(
            line,
            TestData.OutputDirectory,
            out var result);

        Assert.True(parsed);
        Assert.NotNull(result);
        Assert.Equal("video của tôi.mp4", result.OutputFileName);
    }

    [Theory]
    [InlineData("human output")]
    [InlineData("SVVD_OUTPUT:not-json")]
    [InlineData("SVVD_OUTPUT:\"C:\\\\outside\\\\video.mp4\"")]
    [InlineData("SVVD_OUTPUT:\"C:\\\\svvd-test\\\\downloads\\\\nested\\\\video.mp4\"")]
    public void TryParseRejectsUnstructuredOrEscapingPaths(string line)
    {
        var parsed = new YtDlpOutputParser().TryParse(
            line,
            TestData.OutputDirectory,
            out var result);

        Assert.False(parsed);
        Assert.Null(result);
    }
}
