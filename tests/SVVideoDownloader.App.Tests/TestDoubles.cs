using SVVideoDownloader.App.Services;
using SVVideoDownloader.App.ViewModels;
using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.App.Tests;

internal sealed class FakeMetadataProvider : IVideoMetadataProvider
{
    public Func<VideoSource, CancellationToken, Task<MediaOperationResult<VideoInfo>>> Handler
        { get; set; } = (_, _) => Task.FromResult(
            MediaOperationResult<VideoInfo>.Failure(
                new MediaOperationError(
                    MediaErrorCategory.ExecutionFailed,
                    MediaComponent.MetadataExtractor)));

    public int CallCount { get; private set; }

    public Task<MediaOperationResult<VideoInfo>> GetVideoInfoAsync(
        VideoSource source,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Handler(source, cancellationToken);
    }
}

internal sealed class FakeDownloadCoordinator : IDownloadCoordinator
{
    public Func<
        DownloadRequest,
        string,
        IProgress<DownloadProgress>?,
        CancellationToken,
        Task<MediaOperationResult<DownloadResult>>> Handler { get; set; } =
        (_, _, _, _) => Task.FromResult(TestData.SuccessfulDownload());

    public int CallCount { get; private set; }

    public string? LastOutputFolder { get; private set; }

    public Task<MediaOperationResult<DownloadResult>> DownloadAsync(
        DownloadRequest request,
        string outputFolder,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastOutputFolder = outputFolder;
        return Handler(request, outputFolder, progress, cancellationToken);
    }
}

internal sealed class FakeFolderPickerService : IFolderPickerService
{
    public string? SelectedFolder { get; set; }

    public string? InitialFolder { get; private set; }

    public Task<string?> PickFolderAsync(
        string initialFolder,
        CancellationToken cancellationToken = default)
    {
        InitialFolder = initialFolder;
        return Task.FromResult(SelectedFolder);
    }
}

internal sealed class FakeFileActionService : IFileActionService
{
    public bool OpenFileResult { get; set; } = true;

    public bool OpenFolderResult { get; set; } = true;

    public string? OpenedFile { get; private set; }

    public string? OpenedFolder { get; private set; }

    public Task<bool> OpenFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        OpenedFile = filePath;
        return Task.FromResult(OpenFileResult);
    }

    public Task<bool> OpenFolderAsync(
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        OpenedFolder = folderPath;
        return Task.FromResult(OpenFolderResult);
    }
}

internal sealed class ImmediateDispatcher : IUiDispatcher
{
    public void Post(Action action) => action();
}

internal static class TestData
{
    public const string OutputFolder = @"C:\Video";
    public const string VideoUrl = "https://www.youtube.com/watch?v=owned";

    public static VideoSource Source() => VideoSource.Create(VideoUrl).Value!;

    public static VideoInfo Video()
    {
        var format = VideoFormat.Create("22", "mp4", true, true, 1920, 1080).Value!;
        return VideoInfo.Create(
            Source(),
            "Video được phép",
            "Tác giả",
            TimeSpan.FromSeconds(90),
            [format],
            new Uri("https://i.example.test/thumbnail.jpg")).Value!;
    }

    public static DownloadRequest Request()
    {
        var options = DownloadOptions.Create(QualityPreset.Best, "Video được phép").Value!;
        return DownloadRequest.Create(Source(), options, true).Value!;
    }

    public static MediaOperationResult<DownloadResult> SuccessfulDownload(
        string fileName = "Video được phép.mp4") =>
        MediaOperationResult<DownloadResult>.Success(DownloadResult.Create(fileName).Value!);

    public static MainWindowViewModel CreateMainViewModel(
        FakeMetadataProvider? metadata = null,
        FakeDownloadCoordinator? downloads = null,
        FakeFolderPickerService? folders = null,
        FakeFileActionService? files = null) =>
        new(
            metadata ?? new FakeMetadataProvider(),
            downloads ?? new FakeDownloadCoordinator(),
            folders ?? new FakeFolderPickerService(),
            files ?? new FakeFileActionService(),
            new ImmediateDispatcher(),
            new AppUiOptions(OutputFolder));
}
