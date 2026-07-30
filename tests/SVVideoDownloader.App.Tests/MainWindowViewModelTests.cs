using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.App.ViewModels;
using SVVideoDownloader.Core.Videos;
using SVVideoDownloader.Infrastructure.ApplicationData;
using SVVideoDownloader.Infrastructure.ExternalTools;

namespace SVVideoDownloader.App.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void InitialState_IsClearAndCommandsAreGuarded()
    {
        using var viewModel = TestData.CreateMainViewModel();

        Assert.True(viewModel.IsAnalysisEmpty);
        Assert.True(viewModel.IsQueueEmpty);
        Assert.False(viewModel.HasActiveDownloads);
        Assert.False(viewModel.AnalyzeCommand.CanExecute(null));
        Assert.False(viewModel.DownloadCommand.CanExecute(null));
        Assert.False(viewModel.UpdateFfmpegCommand.CanExecute(null));
        Assert.Equal(
            new[]
            {
                DownloadMediaFormat.VideoMp4,
                DownloadMediaFormat.VideoOriginal,
                DownloadMediaFormat.AudioMp3,
            },
            viewModel.DownloadFormatOptions.Select(item => item.Format));
        Assert.Equal(
            DownloadMediaFormat.VideoMp4,
            viewModel.SelectedDownloadFormat?.Format);
        Assert.Equal("MP4 tương thích", viewModel.SelectedDownloadFormat?.DisplayName);
        Assert.True(viewModel.IsMp4FormatSelected);
        Assert.False(viewModel.IsOriginalFormatSelected);
        Assert.Equal("Tốt nhất", viewModel.SelectedQuality?.DisplayName);
    }

    [Fact]
    public void InitialState_UsesRememberedDefaultQuality()
    {
        using var viewModel = TestData.CreateMainViewModel(
            defaultQuality: QualityPreset.AudioMp3);

        Assert.Equal(
            DownloadMediaFormat.AudioMp3,
            viewModel.SelectedDownloadFormat?.Format);
        Assert.True(viewModel.IsMp3FormatSelected);
        Assert.False(viewModel.IsVideoFormatSelected);
        Assert.Equal(QualityPreset.Best, viewModel.SelectedQuality?.Preset);
    }

    [Fact]
    public void InitialState_UsesRememberedOriginalQualityFormat()
    {
        using var viewModel = TestData.CreateMainViewModel(
            defaultQuality: QualityPreset.Video1080p,
            defaultFormat: DownloadMediaFormat.VideoOriginal);

        Assert.Equal(
            DownloadMediaFormat.VideoOriginal,
            viewModel.SelectedDownloadFormat?.Format);
        Assert.True(viewModel.IsVideoFormatSelected);
        Assert.True(viewModel.IsOriginalFormatSelected);
        Assert.False(viewModel.IsMp4FormatSelected);
        Assert.False(viewModel.IsMp3FormatSelected);
        Assert.Equal(QualityPreset.Video1080p, viewModel.SelectedQuality?.Preset);
    }

    [Fact]
    public void InitialState_UsesRememberedDarkTheme()
    {
        var themes = new FakeThemeService();
        using var viewModel = TestData.CreateMainViewModel(
            themes: themes,
            defaultTheme: ApplicationTheme.Dark);

        Assert.True(viewModel.IsDarkMode);
        Assert.Equal("Chế độ sáng", viewModel.ThemeToggleText);
        Assert.Equal(ApplicationTheme.Dark, themes.CurrentTheme);
    }

    [Fact]
    public async Task ToggleTheme_AppliesAndPersistsDarkTheme()
    {
        var themes = new FakeThemeService();
        var userData = new FakeUserDataService();
        using var viewModel = TestData.CreateMainViewModel(
            userData: userData,
            themes: themes);

        viewModel.ToggleThemeCommand.Execute(null);
        await viewModel.PrepareForCloseAsync();

        Assert.True(viewModel.IsDarkMode);
        Assert.Equal(ApplicationTheme.Dark, themes.CurrentTheme);
        Assert.Equal(ApplicationTheme.Dark, Assert.Single(userData.SavedSettings).Theme);
    }

    [Fact]
    public async Task Initialize_LoadsHistoryAndToolStatuses()
    {
        var userData = new FakeUserDataService();
        userData.History.Add(
            new DownloadHistoryEntry(
                Guid.NewGuid(),
                "Video cũ",
                SupportedPlatform.Facebook,
                QualityPreset.Video720p,
                @"C:\Video\video-cu.mp4",
                DateTimeOffset.UtcNow));
        var tools = new FakeToolManagementService();
        using var viewModel = TestData.CreateMainViewModel(userData: userData, tools: tools);

        await viewModel.InitializeAsync();

        var history = Assert.Single(viewModel.DownloadHistory);
        Assert.Equal("Video cũ", history.Title);
        Assert.Equal("Facebook", history.SourceText);
        Assert.Equal("Video (không xác định)", history.FormatText);
        Assert.All(viewModel.ToolStatuses, item => Assert.Equal("Sẵn sàng", item.StatusText));
        Assert.Equal("Tất cả công cụ đã sẵn sàng.", viewModel.ToolsMessage);
    }

    [Fact]
    public async Task Analyze_RejectsUnsupportedUrlWithVietnameseMessage()
    {
        using var viewModel = TestData.CreateMainViewModel();
        viewModel.VideoUrl = "https://example.com/video";

        await viewModel.AnalyzeCommand.ExecuteAsync();

        Assert.True(viewModel.HasAnalysisError);
        Assert.Contains("YouTube, TikTok và Facebook", viewModel.AnalysisMessage);
        Assert.Null(viewModel.VideoInfo);
    }

    [Fact]
    public async Task Analyze_SuccessShowsMetadataAndEnablesConfirmAndDownloadAction()
    {
        var metadata = new FakeMetadataProvider
        {
            Handler = (_, _) => Task.FromResult(
                MediaOperationResult<VideoInfo>.Success(TestData.Video())),
        };
        using var viewModel = TestData.CreateMainViewModel(metadata: metadata);
        viewModel.VideoUrl = TestData.VideoUrl;

        await viewModel.AnalyzeCommand.ExecuteAsync();

        Assert.True(viewModel.IsAnalysisSuccessful);
        Assert.Equal("Video được phép", viewModel.VideoTitle);
        Assert.Equal("YouTube", viewModel.VideoSource);
        Assert.Equal("1:30", viewModel.VideoDuration);
        Assert.True(viewModel.HasThumbnail);
        Assert.True(viewModel.CanDownload);
        Assert.True(viewModel.DownloadCommand.CanExecute(null));
    }

    [Fact]
    public async Task Analyze_DependencyErrorIsTranslatedWithoutRawDetails()
    {
        var metadata = new FakeMetadataProvider
        {
            Handler = (_, _) => Task.FromResult(
                MediaOperationResult<VideoInfo>.Failure(
                    new MediaOperationError(
                        MediaErrorCategory.DependencyMissing,
                        MediaComponent.MetadataExtractor))),
        };
        using var viewModel = TestData.CreateMainViewModel(metadata: metadata);
        viewModel.VideoUrl = TestData.VideoUrl;

        await viewModel.AnalyzeCommand.ExecuteAsync();

        Assert.True(viewModel.HasAnalysisError);
        Assert.Equal("Không tìm thấy yt-dlp.", viewModel.AnalysisMessage);
    }

    [Fact]
    public async Task BrowseOutputFolder_UsesAsyncPickerAndUpdatesSelection()
    {
        var folders = new FakeFolderPickerService { SelectedFolder = @"D:\Tải xuống" };
        var userData = new FakeUserDataService();
        using var viewModel = TestData.CreateMainViewModel(
            folders: folders,
            userData: userData);

        await viewModel.BrowseOutputFolderCommand.ExecuteAsync();
        await viewModel.PrepareForCloseAsync();

        Assert.Equal(TestData.OutputFolder, folders.InitialFolder);
        Assert.Equal(@"D:\Tải xuống", viewModel.OutputFolder);
        Assert.Equal(@"D:\Tải xuống", Assert.Single(userData.SavedSettings).DownloadDirectory);
    }

    [Fact]
    public async Task Download_AddsQueueItemTracksProgressAndCompletes()
    {
        var downloads = new FakeDownloadCoordinator
        {
            Handler = (_, _, progress, _) =>
            {
                progress?.Report(
                    DownloadProgress.Create(50, 5_000_000, 10_000_000, 1_000_000).Value!);
                return Task.FromResult(TestData.SuccessfulDownload());
            },
        };
        using var viewModel = await CreateAnalyzedViewModelAsync(downloads: downloads);

        viewModel.DownloadCommand.Execute(null);
        var item = Assert.Single(viewModel.DownloadQueue);
        await item.StartAsync();

        Assert.Equal(DownloadStatus.Completed, item.Status);
        Assert.Equal("100,0 %", item.PercentageText);
        Assert.Equal("Đã hoàn tất", item.StatusText);
        Assert.EndsWith("Video được phép.mp4", item.FilePath, StringComparison.Ordinal);
        Assert.Equal(TestData.OutputFolder, downloads.LastOutputFolder);
        Assert.True(downloads.LastRequest?.RightsConfirmed);
        Assert.False(viewModel.HasActiveDownloads);
        Assert.True(item.RemoveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Download_Mp3SelectionCreatesAudioRequestAndPersistsSelection()
    {
        var downloads = new FakeDownloadCoordinator();
        var userData = new FakeUserDataService();
        using var viewModel = await CreateAnalyzedViewModelAsync(
            downloads,
            userData: userData);
        viewModel.SelectedDownloadFormat = viewModel.DownloadFormatOptions.Single(
            item => item.Format == DownloadMediaFormat.AudioMp3);

        viewModel.DownloadCommand.Execute(null);
        await Assert.Single(viewModel.DownloadQueue).StartAsync();
        await viewModel.PrepareForCloseAsync();

        Assert.Equal(QualityPreset.AudioMp3, downloads.LastRequest?.Options.QualityPreset);
        Assert.Equal(
            DownloadMediaFormat.AudioMp3,
            downloads.LastRequest?.Options.MediaFormat);
        Assert.Equal(QualityPreset.AudioMp3, Assert.Single(userData.SavedSettings).DefaultQuality);
        Assert.Equal(
            DownloadMediaFormat.AudioMp3,
            Assert.Single(userData.SavedSettings).DefaultFormat);
        Assert.True(viewModel.IsMp3FormatSelected);
    }

    [Fact]
    public async Task Download_OriginalSelectionPreservesFormatAndPersistsSelection()
    {
        var downloads = new FakeDownloadCoordinator();
        var userData = new FakeUserDataService();
        using var viewModel = await CreateAnalyzedViewModelAsync(
            downloads,
            userData: userData);
        viewModel.SelectedDownloadFormat = viewModel.DownloadFormatOptions.Single(
            item => item.Format == DownloadMediaFormat.VideoOriginal);

        viewModel.DownloadCommand.Execute(null);
        await Assert.Single(viewModel.DownloadQueue).StartAsync();
        await viewModel.PrepareForCloseAsync();

        Assert.Equal(
            DownloadMediaFormat.VideoOriginal,
            downloads.LastRequest?.Options.MediaFormat);
        Assert.Equal(QualityPreset.Best, downloads.LastRequest?.Options.QualityPreset);
        Assert.Equal(
            DownloadMediaFormat.VideoOriginal,
            Assert.Single(userData.SavedSettings).DefaultFormat);
        Assert.True(viewModel.IsVideoFormatSelected);
    }

    [Fact]
    public async Task ChangingUrl_ClearsPreviousMetadataAndDisablesDownload()
    {
        using var viewModel = await CreateAnalyzedViewModelAsync();

        viewModel.VideoUrl = "https://www.youtube.com/watch?v=another";

        Assert.True(viewModel.IsAnalysisEmpty);
        Assert.Null(viewModel.VideoInfo);
        Assert.False(viewModel.DownloadCommand.CanExecute(null));
    }

    [Fact]
    public async Task CancelAllActiveDownloads_CancelsQueueForWindowCloseWorkflow()
    {
        var downloads = new FakeDownloadCoordinator
        {
            Handler = async (_, _, _, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return TestData.SuccessfulDownload();
            },
        };
        using var viewModel = await CreateAnalyzedViewModelAsync(downloads);
        viewModel.DownloadCommand.Execute(null);
        var item = Assert.Single(viewModel.DownloadQueue);
        var execution = item.StartAsync();

        Assert.True(viewModel.HasActiveDownloads);
        viewModel.CancelAllActiveDownloads();
        await execution;

        Assert.Equal(DownloadStatus.Cancelled, item.Status);
        Assert.False(viewModel.HasActiveDownloads);
    }

    [Fact]
    public async Task RemoveCompletedItem_EmptiesQueue()
    {
        using var viewModel = await CreateAnalyzedViewModelAsync();
        viewModel.DownloadCommand.Execute(null);
        var item = Assert.Single(viewModel.DownloadQueue);
        await item.StartAsync();

        item.RemoveCommand.Execute(null);

        Assert.Empty(viewModel.DownloadQueue);
        Assert.True(viewModel.IsQueueEmpty);
        Assert.False(viewModel.HasQueueItems);
    }

    [Fact]
    public async Task CompletedDownloadIsAddedToHistoryAndClearDoesNotInvokeFileActions()
    {
        var userData = new FakeUserDataService();
        var files = new FakeFileActionService();
        var metadata = new FakeMetadataProvider
        {
            Handler = (_, _) => Task.FromResult(
                MediaOperationResult<VideoInfo>.Success(TestData.Video())),
        };
        using var viewModel = TestData.CreateMainViewModel(
            metadata,
            files: files,
            userData: userData);
        viewModel.VideoUrl = TestData.VideoUrl;
        await viewModel.AnalyzeCommand.ExecuteAsync();

        viewModel.DownloadCommand.Execute(null);
        await Assert.Single(viewModel.DownloadQueue).StartAsync();

        Assert.Single(userData.History);
        Assert.Equal(
            DownloadMediaFormat.VideoMp4,
            Assert.Single(userData.History).Format);
        Assert.Single(viewModel.DownloadHistory);
        await viewModel.ClearHistoryCommand.ExecuteAsync();
        Assert.Empty(userData.History);
        Assert.Empty(viewModel.DownloadHistory);
        Assert.Null(files.OpenedFile);
        Assert.Null(files.OpenedFolder);
        Assert.Contains("vẫn được giữ nguyên", viewModel.HistoryMessage);
    }

    [Fact]
    public async Task YtDlpUpdateIsDisabledWhileDownloadIsActive()
    {
        var completion = new TaskCompletionSource<MediaOperationResult<DownloadResult>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var downloads = new FakeDownloadCoordinator
        {
            Handler = (_, _, _, _) => completion.Task,
        };
        var tools = new FakeToolManagementService();
        using var viewModel = await CreateAnalyzedViewModelAsync(downloads, tools);

        viewModel.DownloadCommand.Execute(null);

        Assert.True(viewModel.HasActiveDownloads);
        Assert.False(viewModel.UpdateYtDlpCommand.CanExecute(null));
        Assert.False(viewModel.UpdateFfmpegCommand.CanExecute(null));
        Assert.Equal(0, tools.UpdateCallCount);
        Assert.Equal(0, tools.FfmpegUpdateCallCount);

        completion.SetResult(TestData.SuccessfulDownload());
        await Assert.Single(viewModel.DownloadQueue).StartAsync();
    }

    [Fact]
    public async Task ManualYtDlpUpdateReportsResultAndRefreshesStatus()
    {
        var tools = new FakeToolManagementService();
        using var viewModel = TestData.CreateMainViewModel(tools: tools);
        await viewModel.InitializeAsync();

        await viewModel.UpdateYtDlpCommand.ExecuteAsync();

        Assert.Equal(1, tools.UpdateCallCount);
        Assert.Contains("2026.07.17", viewModel.ToolsMessage);
    }

    [Fact]
    public async Task ManualFfmpegUpdateRequiresConfirmationAndRefreshesBothTools()
    {
        var tools = new FakeToolManagementService();
        using var viewModel = TestData.CreateMainViewModel(tools: tools);
        await viewModel.InitializeAsync();

        Assert.False(viewModel.UpdateFfmpegCommand.CanExecute(null));
        viewModel.FfmpegLicenseConfirmed = true;
        Assert.True(viewModel.UpdateFfmpegCommand.CanExecute(null));

        await viewModel.UpdateFfmpegCommand.ExecuteAsync();

        Assert.Equal(1, tools.FfmpegUpdateCallCount);
        Assert.Contains("8.1.2", viewModel.ToolsMessage);
        Assert.False(viewModel.FfmpegLicenseConfirmed);
        Assert.False(viewModel.UpdateFfmpegCommand.CanExecute(null));
    }

    private static async Task<MainWindowViewModel> CreateAnalyzedViewModelAsync(
        FakeDownloadCoordinator? downloads = null,
        FakeToolManagementService? tools = null,
        FakeUserDataService? userData = null)
    {
        var metadata = new FakeMetadataProvider
        {
            Handler = (_, _) => Task.FromResult(
                MediaOperationResult<VideoInfo>.Success(TestData.Video())),
        };
        var viewModel = TestData.CreateMainViewModel(
            metadata,
            downloads,
            tools: tools,
            userData: userData);
        viewModel.VideoUrl = TestData.VideoUrl;
        await viewModel.AnalyzeCommand.ExecuteAsync();
        return viewModel;
    }
}
