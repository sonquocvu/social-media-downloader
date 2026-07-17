using SVVideoDownloader.Infrastructure.YtDlp;

namespace SVVideoDownloader.Infrastructure.Tests;

public sealed class YtDlpProgressParserTests
{
    [Fact]
    public void TryParseUsesNumericJsonProgressFields()
    {
        const string line =
            "SVVD_PROGRESS:{\"downloaded_bytes\":500,\"total_bytes\":1000,\"speed\":125.5}";
        var parser = new YtDlpProgressParser();

        var parsed = parser.TryParse(line, out var progress);

        Assert.True(parsed);
        Assert.NotNull(progress);
        Assert.Equal(50d, progress.Percentage);
        Assert.Equal(500, progress.DownloadedBytes);
        Assert.Equal(1_000, progress.TotalBytes);
        Assert.Equal(125.5, progress.BytesPerSecond);
    }

    [Fact]
    public void TryParseFallsBackToEstimatedTotal()
    {
        const string line =
            "SVVD_PROGRESS:{\"downloaded_bytes\":250,\"total_bytes\":null," +
            "\"total_bytes_estimate\":1000,\"speed\":null}";

        var parsed = new YtDlpProgressParser().TryParse(line, out var progress);

        Assert.True(parsed);
        Assert.NotNull(progress);
        Assert.Equal(25d, progress.Percentage);
        Assert.Equal(1_000, progress.TotalBytes);
        Assert.Null(progress.BytesPerSecond);
    }

    [Fact]
    public void TryParseUsesTemplatePercentWhenTotalIsUnknown()
    {
        const string line =
            "SVVD_PROGRESS:{\"downloaded_bytes\":250,\"_percent_str\":\" 12.5%\"}";

        var parsed = new YtDlpProgressParser().TryParse(line, out var progress);

        Assert.True(parsed);
        Assert.NotNull(progress);
        Assert.Equal(12.5, progress.Percentage);
        Assert.Null(progress.TotalBytes);
    }

    [Theory]
    [InlineData("[download] 50% of 10MiB")]
    [InlineData("SVVD_PROGRESS:not-json")]
    [InlineData("SVVD_PROGRESS:[]")]
    [InlineData("SVVD_PROGRESS:{\"downloaded_bytes\":-1}")]
    public void TryParseIgnoresHumanReadableOrInvalidOutput(string line)
    {
        var parsed = new YtDlpProgressParser().TryParse(line, out var progress);

        Assert.False(parsed);
        Assert.Null(progress);
    }
}
