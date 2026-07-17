using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.Core.Tests;

public sealed class VideoSourceTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=abc", SupportedPlatform.YouTube)]
    [InlineData("https://youtu.be/abc", SupportedPlatform.YouTube)]
    [InlineData("https://www.tiktok.com/@owner/video/123", SupportedPlatform.TikTok)]
    [InlineData("https://www.facebook.com/owner/videos/123", SupportedPlatform.Facebook)]
    [InlineData("https://fb.watch/abc", SupportedPlatform.Facebook)]
    public void TryCreateRecognizesSupportedHttpsHosts(string value, SupportedPlatform expectedPlatform)
    {
        var created = VideoSource.TryCreate(value, out var source);

        Assert.True(created);
        Assert.NotNull(source);
        Assert.Equal(expectedPlatform, source.Platform);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("http://youtube.com/watch?v=abc")]
    [InlineData("https://youtube.com.example.test/watch?v=abc")]
    [InlineData("https://user:password@youtube.com/watch?v=abc")]
    [InlineData("https://example.com/video/123")]
    public void TryCreateRejectsUnsupportedOrUnsafeValues(string? value)
    {
        var created = VideoSource.TryCreate(value, out var source);

        Assert.False(created);
        Assert.Null(source);
    }

    [Fact]
    public void CoreDoesNotReferenceForbiddenImplementationAssemblies()
    {
        var referenceNames = typeof(VideoSource).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain("PresentationFramework", referenceNames);
        Assert.DoesNotContain("System.Diagnostics.Process", referenceNames);
    }
}
