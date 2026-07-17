using SVVideoDownloader.Core.Videos;
using SVVideoDownloader.Infrastructure.ApplicationData;
using SVVideoDownloader.Infrastructure.Tests.Fakes;

namespace SVVideoDownloader.Infrastructure.Tests;

public sealed class ApplicationDataStoreTests
{
    [Fact]
    public void PathsStayUnderConfiguredLocalRoot()
    {
        using var directory = new TemporaryTestDirectory();

        var paths = ApplicationDataPaths.Create(directory.Path);

        Assert.Equal(Path.Combine(directory.Path, "settings.json"), paths.SettingsFilePath);
        Assert.Equal(Path.Combine(directory.Path, "history.json"), paths.HistoryFilePath);
        Assert.Equal(Path.Combine(directory.Path, "tools"), paths.ToolsDirectory);
        Assert.StartsWith(directory.Path, paths.LogFilePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SettingsRoundTripRemembersDirectoryAndQuality()
    {
        using var directory = new TemporaryTestDirectory();
        var logger = new RecordingDiagnosticLogger();
        using var store = new JsonApplicationSettingsStore(
            Path.Combine(directory.Path, "settings.json"),
            logger);
        var expected = new ApplicationSettings(
            Path.Combine(directory.Path, "video"),
            QualityPreset.Video1080p);

        await store.SaveAsync(expected);
        var loaded = await store.LoadAsync(
            new ApplicationSettings(directory.Path, QualityPreset.Best));

        Assert.Equal(expected, loaded);
        Assert.DoesNotContain(".tmp", Directory.GetFiles(directory.Path).Single());
    }

    [Fact]
    public async Task CorruptSettingsFallBackWithoutLeakingContents()
    {
        using var directory = new TemporaryTestDirectory();
        var settingsPath = Path.Combine(directory.Path, "settings.json");
        await File.WriteAllTextAsync(settingsPath, "{ token=private-secret");
        var logger = new RecordingDiagnosticLogger();
        using var store = new JsonApplicationSettingsStore(settingsPath, logger);
        var defaults = new ApplicationSettings(directory.Path, QualityPreset.Best);

        var loaded = await store.LoadAsync(defaults);

        Assert.Equal(defaults, loaded);
        Assert.DoesNotContain(
            logger.Messages,
            message => message.Contains("private-secret", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ClearingHistoryDoesNotDeleteDownloadedMedia()
    {
        using var directory = new TemporaryTestDirectory();
        var mediaPath = Path.Combine(directory.Path, "video.mp4");
        await File.WriteAllTextAsync(mediaPath, "owned media fixture");
        var logger = new RecordingDiagnosticLogger();
        using var store = new JsonDownloadHistoryStore(
            Path.Combine(directory.Path, "history.json"),
            logger);
        var entry = new DownloadHistoryEntry(
            Guid.NewGuid(),
            "Video của tôi",
            SupportedPlatform.YouTube,
            QualityPreset.Video720p,
            mediaPath,
            DateTimeOffset.UtcNow);

        await store.AddAsync(entry);
        await store.ClearAsync();
        var history = await store.LoadAsync();

        Assert.Empty(history);
        Assert.True(File.Exists(mediaPath));
        Assert.Equal("owned media fixture", await File.ReadAllTextAsync(mediaPath));
    }

    [Fact]
    public async Task HistoryIsBoundedAndNewestEntryComesFirst()
    {
        using var directory = new TemporaryTestDirectory();
        var logger = new RecordingDiagnosticLogger();
        using var store = new JsonDownloadHistoryStore(
            Path.Combine(directory.Path, "history.json"),
            logger,
            maximumEntries: 2);

        for (var index = 0; index < 3; index++)
        {
            await store.AddAsync(
                new DownloadHistoryEntry(
                    Guid.NewGuid(),
                    $"Video {index}",
                    SupportedPlatform.YouTube,
                    QualityPreset.Best,
                    Path.Combine(directory.Path, $"video-{index}.mp4"),
                    DateTimeOffset.UtcNow.AddMinutes(index)));
        }

        var history = await store.LoadAsync();

        Assert.Equal(2, history.Count);
        Assert.Equal("Video 2", history[0].Title);
        Assert.Equal("Video 1", history[1].Title);
    }
}
