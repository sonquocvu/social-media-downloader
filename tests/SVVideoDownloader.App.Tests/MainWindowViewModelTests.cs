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
        Assert.Equal("Tốt nhất", viewModel.SelectedQuality?.DisplayName);
    }

    [Fact]
    public void InitialState_UsesRememberedDefaultQuality()
    {
        using var viewModel = TestData.CreateMainViewModel(
            defaultQuality: QualityPreset.AudioMp3);

        Assert.Equal(QualityPreset.AudioMp3, viewModel.SelectedQuality?.Preset);
        Assert.Equal("Âm thanh MP3", viewModel.SelectedQuality?.DisplayName);
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
    public async Task Analyze_SuccessShowsMetadataAndEnablesDownloadAfterConfirmation()
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
        Assert.False(viewModel.CanDownload);

        viewModel.RightsConfirmed = true;

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
        Assert.False(viewModel.HasActiveDownloads);
        Assert.False(viewModel.RightsConfirmed);
        Assert.True(item.RemoveCommand.CanExecute(null));
    }

    [Fact]
    public async Task ChangingUrl_ClearsPreviousMetadataAndRightsConfirmation()
    {
        using var viewModel = await CreateAnalyzedViewModelAsync();

        viewModel.VideoUrl = "https://www.youtube.com/watch?v=another";

        Assert.True(viewModel.IsAnalysisEmpty);
        Assert.Null(viewModel.VideoInfo);
        Assert.False(viewModel.RightsConfirmed);
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
        viewModel.RightsConfirmed = true;

        viewModel.DownloadCommand.Execute(null);
        await Assert.Single(viewModel.DownloadQueue).StartAsync();

        Assert.Single(userData.History);
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
        Assert.Equal(0, tools.UpdateCallCount);

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

    private static async Task<MainWindowViewModel> CreateAnalyzedViewModelAsync(
        FakeDownloadCoordinator? downloads = null,
        FakeToolManagementService? tools = null)
    {
        var metadata = new FakeMetadataProvider
        {
            Handler = (_, _) => Task.FromResult(
                MediaOperationResult<VideoInfo>.Success(TestData.Video())),
        };
        var viewModel = TestData.CreateMainViewModel(metadata, downloads, tools: tools);
        viewModel.VideoUrl = TestData.VideoUrl;
        await viewModel.AnalyzeCommand.ExecuteAsync();
        viewModel.RightsConfirmed = true;
        return viewModel;
    }
}
