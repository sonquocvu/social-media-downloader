using SVVideoDownloader.App.ViewModels;
using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Media;

namespace SVVideoDownloader.App.Tests;

public sealed class DownloadItemViewModelTests
{
    [Fact]
    public async Task Cancel_StopsActiveDownloadAndEnablesRetry()
    {
        var downloads = new FakeDownloadCoordinator
        {
            Handler = async (_, _, _, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return TestData.SuccessfulDownload();
            },
        };
        using var item = CreateItem(downloads);
        var execution = item.StartAsync();

        item.CancelCommand.Execute(null);
        await execution;

        Assert.Equal(DownloadStatus.Cancelled, item.Status);
        Assert.Equal("Đã hủy", item.StatusText);
        Assert.True(item.RetryCommand.CanExecute(null));
        Assert.False(item.CancelCommand.CanExecute(null));
    }

    [Fact]
    public async Task Retry_RunsFailedDownloadAgainAndCanComplete()
    {
        var downloads = new FakeDownloadCoordinator();
        downloads.Handler = (_, _, _, _) => Task.FromResult(
            downloads.CallCount == 1
                ? MediaOperationResult<DownloadResult>.Failure(
                    new MediaOperationError(
                        MediaErrorCategory.ExecutionFailed,
                        MediaComponent.MetadataExtractor))
                : TestData.SuccessfulDownload());
        using var item = CreateItem(downloads);

        await item.StartAsync();
        Assert.Equal(DownloadStatus.Failed, item.Status);

        await item.RetryCommand.ExecuteAsync();

        Assert.Equal(2, downloads.CallCount);
        Assert.Equal(DownloadStatus.Completed, item.Status);
        Assert.False(item.HasErrorMessage);
    }

    [Fact]
    public async Task Retry_WaitsForCancelledExecutionBeforeStartingAgain()
    {
        var cancellationCleanup = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var downloads = new FakeDownloadCoordinator();
        downloads.Handler = async (_, _, _, cancellationToken) =>
        {
            if (downloads.CallCount == 1)
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    await cancellationCleanup.Task;
                    throw;
                }
            }

            return TestData.SuccessfulDownload();
        };
        using var item = CreateItem(downloads);
        _ = item.StartAsync();
        item.Cancel();

        var retry = item.RetryCommand.ExecuteAsync();

        Assert.Equal(1, downloads.CallCount);
        cancellationCleanup.SetResult();
        await retry;

        Assert.Equal(2, downloads.CallCount);
        Assert.Equal(DownloadStatus.Completed, item.Status);
    }

    [Fact]
    public async Task Progress_FormatsPercentageSpeedDownloadedSizeAndEta()
    {
        var completion = new TaskCompletionSource<MediaOperationResult<DownloadResult>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var downloads = new FakeDownloadCoordinator
        {
            Handler = (_, _, progress, _) =>
            {
                progress?.Report(
                    DownloadProgress.Create(
                        25,
                        1_048_576,
                        4_194_304,
                        1_048_576).Value!);
                return completion.Task;
            },
        };
        using var item = CreateItem(downloads);

        var execution = item.StartAsync();

        Assert.Equal("25,0 %", item.PercentageText);
        Assert.Equal("1,0 MB", item.DownloadedSizeText);
        Assert.Equal("1,0 MB/giây", item.SpeedText);
        Assert.Equal("0:03", item.EtaText);

        completion.SetResult(TestData.SuccessfulDownload());
        await execution;
    }

    [Fact]
    public async Task OpenActions_UseAsyncFileServiceOnlyAfterCompletion()
    {
        var files = new FakeFileActionService();
        using var item = CreateItem(files: files);

        Assert.False(item.OpenFileCommand.CanExecute(null));
        Assert.False(item.OpenFolderCommand.CanExecute(null));

        await item.StartAsync();
        await item.OpenFileCommand.ExecuteAsync();
        await item.OpenFolderCommand.ExecuteAsync();

        Assert.EndsWith("Video được phép.mp4", files.OpenedFile, StringComparison.Ordinal);
        Assert.Equal(TestData.OutputFolder, files.OpenedFolder);
    }

    private static DownloadItemViewModel CreateItem(
        FakeDownloadCoordinator? downloads = null,
        FakeFileActionService? files = null) =>
        new(
            TestData.Video(),
            TestData.Request(),
            TestData.OutputFolder,
            downloads ?? new FakeDownloadCoordinator(),
            files ?? new FakeFileActionService(),
            new ImmediateDispatcher(),
            _ => { });
}
