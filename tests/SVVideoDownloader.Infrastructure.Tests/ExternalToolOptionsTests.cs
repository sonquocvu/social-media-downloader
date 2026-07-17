using SVVideoDownloader.Infrastructure.ExternalTools;

namespace SVVideoDownloader.Infrastructure.Tests;

public sealed class ExternalToolOptionsTests
{
    [Fact]
    public void ConstructorStoresAbsolutePathsAndTimeouts()
    {
        var options = TestData.CreateOptions();

        Assert.Equal(TestData.YtDlpPath, options.YtDlpPath);
        Assert.Equal(TestData.FfmpegPath, options.FfmpegPath);
        Assert.Equal(TestData.FfprobePath, options.FfprobePath);
        Assert.Equal(TestData.OutputDirectory, options.OutputDirectory);
        Assert.Equal(TimeSpan.FromSeconds(30), options.MetadataTimeout);
        Assert.Equal(TimeSpan.FromMinutes(5), options.DownloadTimeout);
    }

    [Theory]
    [InlineData(@"C:\Windows\System32\cmd.exe")]
    [InlineData(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe")]
    [InlineData(@"C:\tools\pwsh.exe")]
    public void ConstructorRejectsShellAsToolPath(string toolPath)
    {
        Assert.Throws<ArgumentException>(
            () => new ExternalToolOptions(
                toolPath,
                TestData.FfmpegPath,
                TestData.FfprobePath,
                TestData.OutputDirectory));
    }

    [Fact]
    public void ConstructorRequiresFfmpegAndFfprobeInSameDirectory()
    {
        Assert.Throws<ArgumentException>(
            () => new ExternalToolOptions(
                TestData.YtDlpPath,
                TestData.FfmpegPath,
                @"C:\other\ffprobe.exe",
                TestData.OutputDirectory));
    }
}
