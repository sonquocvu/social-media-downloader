using SVVideoDownloader.Infrastructure.Processes;

namespace SVVideoDownloader.Infrastructure.Tests;

public sealed class SystemProcessRunnerTests
{
    [Fact]
    public void CreateStartInfoUsesArgumentListAndNeverUsesShellExecution()
    {
        var url = "https://www.youtube.com/watch?v=owned&list=private-value";
        var request = new ProcessRequest(
            TestData.YtDlpPath,
            new[] { "--no-playlist", "--", url });

        var startInfo = SystemProcessRunner.CreateStartInfo(request);

        Assert.Equal(TestData.YtDlpPath, startInfo.FileName);
        Assert.False(startInfo.UseShellExecute);
        Assert.True(startInfo.RedirectStandardOutput);
        Assert.True(startInfo.RedirectStandardError);
        Assert.False(startInfo.RedirectStandardInput);
        Assert.True(startInfo.CreateNoWindow);
        Assert.Equal(string.Empty, startInfo.Arguments);
        Assert.Equal(new[] { "--no-playlist", "--", url }, startInfo.ArgumentList);
    }

    [Theory]
    [InlineData(@"C:\Windows\System32\cmd.exe")]
    [InlineData(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe")]
    [InlineData(@"C:\tools\pwsh.exe")]
    [InlineData(@"C:\tools\PowerShell_ISE.exe")]
    public void CreateStartInfoRejectsCommandShells(string executablePath)
    {
        var request = new ProcessRequest(executablePath, Array.Empty<string>());

        var exception = Assert.Throws<ExternalProcessStartException>(
            () => SystemProcessRunner.CreateStartInfo(request));

        Assert.Equal(ProcessStartFailureKind.InvalidExecutable, exception.Kind);
    }

    [Fact]
    public void ProcessRequestDefensivelyCopiesArguments()
    {
        var arguments = new List<string> { "--", "https://youtu.be/owned" };

        var request = new ProcessRequest(TestData.YtDlpPath, arguments);
        arguments.Clear();

        Assert.Equal(new[] { "--", "https://youtu.be/owned" }, request.ArgumentList);
    }

    [Fact]
    public void ProcessRequestRejectsNonPositiveTimeout()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ProcessRequest(
                TestData.YtDlpPath,
                Array.Empty<string>(),
                TimeSpan.Zero));
    }
}
