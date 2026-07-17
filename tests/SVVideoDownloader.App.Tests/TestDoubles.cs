using SVVideoDownloader.App.Services;
using SVVideoDownloader.App.ViewModels;
using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Core.Videos;
using SVVideoDownloader.Infrastructure.ApplicationData;
using SVVideoDownloader.Infrastructure.Diagnostics;
using SVVideoDownloader.Infrastructure.ExternalTools;
using SVVideoDownloader.Infrastructure.Updates;

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

internal sealed class FakeUserDataService : IUserDataService
{
    public List<ApplicationSettings> SavedSettings { get; } = [];

    public List<DownloadHistoryEntry> History { get; } = [];

    public Task SaveSettingsAsync(
        ApplicationSettings settings,
        CancellationToken cancellationToken = default)
    {
        SavedSettings.Add(settings);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DownloadHistoryEntry>> LoadHistoryAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DownloadHistoryEntry>>(History.ToArray());

    public Task AddHistoryAsync(
        DownloadHistoryEntry entry,
        CancellationToken cancellationToken = default)
    {
        History.Insert(0, entry);
        return Task.CompletedTask;
    }

    public Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        History.Clear();
        return Task.CompletedTask;
    }
}

internal sealed class FakeToolManagementService : IToolManagementService
{
    public Func<Task<ToolUpdateOperationResult>> UpdateHandler { get; set; } = () =>
        Task.FromResult(
            new ToolUpdateOperationResult(
                false,
                new YtDlpUpdateResult(YtDlpUpdateStatus.Success, "2026.07.17")));

    public Func<Task<FfmpegToolUpdateOperationResult>> FfmpegUpdateHandler { get; set; } = () =>
        Task.FromResult(
            new FfmpegToolUpdateOperationResult(
                false,
                new FfmpegUpdateResult(FfmpegUpdateStatus.Success, "8.1.2", "8.1.2")));

    public IReadOnlyList<ExternalToolStatus> Statuses { get; set; } =
    [
        new(ExternalToolKind.YtDlp, @"C:\Tools\yt-dlp.exe", true, "2026.07.17", null),
        new(ExternalToolKind.Ffmpeg, @"C:\Tools\ffmpeg.exe", true, "8.0", null),
        new(ExternalToolKind.Ffprobe, @"C:\Tools\ffprobe.exe", true, "8.0", null),
    ];

    public int UpdateCallCount { get; private set; }

    public int FfmpegUpdateCallCount { get; private set; }

    public Task<IReadOnlyList<ExternalToolStatus>> CheckStatusAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Statuses);

    public Task<ToolUpdateOperationResult> UpdateYtDlpAsync(
        CancellationToken cancellationToken = default)
    {
        UpdateCallCount++;
        return UpdateHandler();
    }

    public Task<FfmpegToolUpdateOperationResult> UpdateFfmpegAsync(
        CancellationToken cancellationToken = default)
    {
        FfmpegUpdateCallCount++;
        return FfmpegUpdateHandler();
    }
}

internal sealed class FakeDiagnosticLogger : IDiagnosticLogger
{
    public List<string> Messages { get; } = [];

    public Task LogAsync(
        DiagnosticLogLevel level,
        string message,
        CancellationToken cancellationToken = default)
    {
        Messages.Add($"{level}:{message}");
        return Task.CompletedTask;
    }
}

internal sealed class FakeThemeService : IThemeService
{
    public ApplicationTheme CurrentTheme { get; private set; } = ApplicationTheme.Light;

    public List<ApplicationTheme> AppliedThemes { get; } = [];

    public void Apply(ApplicationTheme theme)
    {
        CurrentTheme = theme;
        AppliedThemes.Add(theme);
    }
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
        FakeFileActionService? files = null,
        FakeUserDataService? userData = null,
        FakeToolManagementService? tools = null,
        FakeThemeService? themes = null,
        QualityPreset defaultQuality = QualityPreset.Best,
        ApplicationTheme defaultTheme = ApplicationTheme.Light) =>
        new(
            metadata ?? new FakeMetadataProvider(),
            downloads ?? new FakeDownloadCoordinator(),
            folders ?? new FakeFolderPickerService(),
            files ?? new FakeFileActionService(),
            new ImmediateDispatcher(),
            userData ?? new FakeUserDataService(),
            tools ?? new FakeToolManagementService(),
            themes ?? new FakeThemeService(),
            new FakeDiagnosticLogger(),
            new AppUiOptions(OutputFolder, defaultQuality, defaultTheme));
}
