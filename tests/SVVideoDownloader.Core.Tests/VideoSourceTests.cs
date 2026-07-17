using SVVideoDownloader.Core.Validation;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.Core.Tests;

public sealed class VideoSourceTests
{
    [Theory]
    [InlineData("https://youtube.com/watch?v=abc", SupportedPlatform.YouTube)]
    [InlineData("https://www.youtube.com/watch?v=abc", SupportedPlatform.YouTube)]
    [InlineData("https://m.youtube.com/watch?v=abc", SupportedPlatform.YouTube)]
    [InlineData("https://youtu.be/abc", SupportedPlatform.YouTube)]
    [InlineData("https://www.tiktok.com/@owner/video/123", SupportedPlatform.TikTok)]
    [InlineData("https://m.tiktok.com/v/123", SupportedPlatform.TikTok)]
    [InlineData("https://facebook.com/owner/videos/123", SupportedPlatform.Facebook)]
    [InlineData("https://www.facebook.com/owner/videos/123", SupportedPlatform.Facebook)]
    [InlineData("https://fb.watch/abc", SupportedPlatform.Facebook)]
    [InlineData("https://WWW.YOUTUBE.COM./watch?v=abc", SupportedPlatform.YouTube)]
    public void CreateRecognizesSupportedPublicHosts(
        string value,
        SupportedPlatform expectedPlatform)
    {
        var result = VideoSource.Create(value);

        Assert.True(result.IsSuccess);
        var source = Assert.IsType<VideoSource>(result.Value);
        Assert.Equal(expectedPlatform, source.Platform);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRejectsEmptyValues(string? value)
    {
        var result = VideoSource.Create(value);

        AssertFailure(result, ValidationErrorCode.Required);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("youtube.com/watch?v=abc")]
    [InlineData("/watch?v=abc")]
    public void CreateRejectsMalformedOrRelativeUrls(string value)
    {
        var result = VideoSource.Create(value);

        AssertFailure(result, ValidationErrorCode.MalformedUrl);
    }

    [Theory]
    [InlineData("http://youtube.com/watch?v=abc")]
    [InlineData("ftp://youtube.com/video")]
    public void CreateRequiresHttps(string value)
    {
        var result = VideoSource.Create(value);

        AssertFailure(result, ValidationErrorCode.HttpsRequired);
    }

    [Fact]
    public void CreateRejectsEmbeddedCredentials()
    {
        var result = VideoSource.Create("https://user:password@youtube.com/watch?v=abc");

        AssertFailure(result, ValidationErrorCode.CredentialsNotAllowed);
    }

    [Theory]
    [InlineData("https://example.com/video/123")]
    [InlineData("https://youtube.com.example.test/watch?v=abc")]
    [InlineData("https://notyoutube.com/watch?v=abc")]
    [InlineData("https://youtu.be.example.test/abc")]
    [InlineData("https://facebook.example.com/video/123")]
    public void CreateRejectsUnsupportedOrDeceptiveHosts(string value)
    {
        var result = VideoSource.Create(value);

        AssertFailure(result, ValidationErrorCode.UnsupportedHost);
    }

    [Fact]
    public void TryCreatePreservesCompatibilityForValidUrls()
    {
        var created = VideoSource.TryCreate("https://youtu.be/abc", out var source);

        Assert.True(created);
        Assert.NotNull(source);
        Assert.Equal(SupportedPlatform.YouTube, source.Platform);
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
        Assert.DoesNotContain("System.IO.FileSystem", referenceNames);
        Assert.DoesNotContain("System.Net.Http", referenceNames);
    }

    private static void AssertFailure(
        ValidationResult<VideoSource> result,
        ValidationErrorCode expectedCode)
    {
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        var error = Assert.Single(result.Errors);
        Assert.Equal(expectedCode, error.Code);
        Assert.Equal("Url", error.Field);
    }
}
