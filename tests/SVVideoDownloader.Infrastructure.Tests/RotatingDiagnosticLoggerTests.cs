using SVVideoDownloader.Infrastructure.Diagnostics;

namespace SVVideoDownloader.Infrastructure.Tests;

public sealed class RotatingDiagnosticLoggerTests
{
    [Fact]
    public async Task LogRedactsCookiesTokensAuthorizationAndCookieOptions()
    {
        using var directory = new TemporaryTestDirectory();
        var logPath = Path.Combine(directory.Path, "logs", "app.log");
        using var logger = new RotatingDiagnosticLogger(logPath, 10_000, 3);

        await logger.LogAsync(
            DiagnosticLogLevel.Error,
            "cookie=private-cookie Authorization: Bearer abc.def token=secret-token " +
            "https://example.test/?api_key=my-key --cookies-from-browser chrome");

        var content = await File.ReadAllTextAsync(logPath);
        Assert.DoesNotContain("private-cookie", content, StringComparison.Ordinal);
        Assert.DoesNotContain("abc.def", content, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-token", content, StringComparison.Ordinal);
        Assert.DoesNotContain("my-key", content, StringComparison.Ordinal);
        Assert.DoesNotContain("chrome", content, StringComparison.Ordinal);
        Assert.DoesNotContain("https://example.test", content, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LogRotationKeepsConfiguredNumberOfFiles()
    {
        using var directory = new TemporaryTestDirectory();
        var logDirectory = Path.Combine(directory.Path, "logs");
        var logPath = Path.Combine(logDirectory, "app.log");
        using var logger = new RotatingDiagnosticLogger(logPath, 120, 3);

        for (var index = 0; index < 12; index++)
        {
            await logger.LogAsync(
                DiagnosticLogLevel.Information,
                $"Sự kiện chẩn đoán số {index} với dữ liệu đủ dài để xoay tệp.");
        }

        var files = Directory.GetFiles(logDirectory, "app.log*");
        Assert.InRange(files.Length, 2, 3);
        Assert.True(File.Exists(logPath));
        Assert.True(File.Exists($"{logPath}.1"));
    }
}
