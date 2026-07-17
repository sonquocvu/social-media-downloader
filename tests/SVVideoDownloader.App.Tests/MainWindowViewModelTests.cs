using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.App.ViewModels;
using SVVideoDownloader.Core.Videos;

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
        using var viewModel = TestData.CreateMainViewModel(folders: folders);

        await viewModel.BrowseOutputFolderCommand.ExecuteAsync();

        Assert.Equal(TestData.OutputFolder, folders.InitialFolder);
        Assert.Equal(@"D:\Tải xuống", viewModel.OutputFolder);
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

    private static async Task<MainWindowViewModel> CreateAnalyzedViewModelAsync(
        FakeDownloadCoordinator? downloads = null)
    {
        var metadata = new FakeMetadataProvider
        {
            Handler = (_, _) => Task.FromResult(
                MediaOperationResult<VideoInfo>.Success(TestData.Video())),
        };
        var viewModel = TestData.CreateMainViewModel(metadata, downloads);
        viewModel.VideoUrl = TestData.VideoUrl;
        await viewModel.AnalyzeCommand.ExecuteAsync();
        viewModel.RightsConfirmed = true;
        return viewModel;
    }
}
