using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Core.Videos;
using SVVideoDownloader.Infrastructure.YtDlp;

namespace SVVideoDownloader.Infrastructure.Tests;

public sealed class YtDlpMetadataParserTests
{
    [Fact]
    public void ParseMapsStableJsonFieldsIntoCoreModels()
    {
        const string json = """
            {
              "id": "owned-video",
              "title": "Video do tôi sở hữu",
              "uploader": "Tác giả",
              "duration": 62.5,
              "thumbnail": "https://cdn.example.test/thumbnail.jpg",
              "formats": [
                {
                  "format_id": "137",
                  "ext": "mp4",
                  "vcodec": "avc1.640028",
                  "acodec": "none",
                  "width": 1920,
                  "height": 1080,
                  "filesize": 12000000
                },
                {
                  "format_id": "140",
                  "ext": "m4a",
                  "vcodec": "none",
                  "acodec": "mp4a.40.2",
                  "filesize_approx": 2000000
                },
                {
                  "format_id": "storyboard",
                  "ext": "mhtml",
                  "vcodec": "none",
                  "acodec": "none"
                }
              ]
            }
            """;
        var parser = new YtDlpMetadataParser();
        var source = TestData.CreateSource();

        var result = parser.Parse(source, json);

        Assert.True(result.IsSuccess);
        var info = Assert.IsType<VideoInfo>(result.Value);
        Assert.Same(source, info.Source);
        Assert.Equal("Video do tôi sở hữu", info.Title);
        Assert.Equal("Tác giả", info.Author);
        Assert.Equal(TimeSpan.FromSeconds(62.5), info.Duration);
        Assert.Equal(
            new Uri("https://cdn.example.test/thumbnail.jpg"),
            info.ThumbnailUri);
        Assert.Collection(
            info.Formats,
            video =>
            {
                Assert.Equal("137", video.Id);
                Assert.True(video.HasVideo);
                Assert.False(video.HasAudio);
                Assert.Equal(1080, video.Height);
                Assert.Equal(12_000_000, video.EstimatedSizeBytes);
            },
            audio =>
            {
                Assert.Equal("140", audio.Id);
                Assert.False(audio.HasVideo);
                Assert.True(audio.HasAudio);
                Assert.Equal(2_000_000, audio.EstimatedSizeBytes);
            });
    }

    [Fact]
    public void ParseUsesChannelWhenUploaderIsAbsent()
    {
        const string json = """
            {
              "title": "Video",
              "channel": "Kênh",
              "formats": [
                { "format_id": "best", "ext": "mp4", "vcodec": "h264", "acodec": "aac" }
              ]
            }
            """;

        var result = new YtDlpMetadataParser().Parse(TestData.CreateSource(), json);

        Assert.True(result.IsSuccess);
        Assert.Equal("Kênh", result.Value!.Author);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("{\"title\":\"Missing formats\"}")]
    [InlineData("{\"_type\":\"playlist\",\"title\":\"A playlist\",\"formats\":[]}")]
    public void ParseRejectsInvalidOrPlaylistResponses(string json)
    {
        var result = new YtDlpMetadataParser().Parse(TestData.CreateSource(), json);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(MediaErrorCategory.InvalidResponse, result.Error.Category);
        Assert.Equal(MediaComponent.MetadataExtractor, result.Error.Component);
    }
}
