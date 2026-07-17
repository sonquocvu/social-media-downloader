using SVVideoDownloader.Core.Files;
using SVVideoDownloader.Core.Validation;

namespace SVVideoDownloader.Core.Tests;

public sealed class WindowsFileNameSanitizerTests
{
    [Theory]
    [InlineData("video<demo>.mp4", "video_demo_.mp4")]
    [InlineData("video:demo/part\\one|two?.mp4", "video_demo_part_one_two_.mp4")]
    [InlineData("video\u0001name.mp4", "video_name.mp4")]
    public void SanitizeReplacesWindowsInvalidCharacters(string input, string expected)
    {
        var result = WindowsFileNameSanitizer.Sanitize(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("video.mp4...", "video.mp4")]
    [InlineData("video.mp4.   ", "video.mp4")]
    [InlineData("  video tiếng Việt.mp4  ", "video tiếng Việt.mp4")]
    public void SanitizeRemovesTrailingDotsAndSpaces(string input, string expected)
    {
        var result = WindowsFileNameSanitizer.Sanitize(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("CON", "_CON")]
    [InlineData("con.mp4", "_con.mp4")]
    [InlineData("PRN.txt", "_PRN.txt")]
    [InlineData("LPT9", "_LPT9")]
    [InlineData("COM1.video.mp4", "_COM1.video.mp4")]
    [InlineData("COM¹.txt", "_COM¹.txt")]
    [InlineData("LPT².txt", "_LPT².txt")]
    [InlineData("CON .mp4", "_CON .mp4")]
    [InlineData("CONIN$", "_CONIN$")]
    public void SanitizePrefixesReservedWindowsNames(string input, string expected)
    {
        var result = WindowsFileNameSanitizer.Sanitize(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeReturnsStructuredErrorForEmptyName(string? input)
    {
        var result = WindowsFileNameSanitizer.Sanitize(input);

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorCode.Required, error.Code);
        Assert.Equal("OutputFileName", error.Field);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("...")]
    public void SanitizeRejectsNameThatBecomesEmpty(string input)
    {
        var result = WindowsFileNameSanitizer.Sanitize(input);

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorCode.InvalidValue, error.Code);
    }
}
