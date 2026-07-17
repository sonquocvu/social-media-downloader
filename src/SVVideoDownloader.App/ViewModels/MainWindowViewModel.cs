using System.Collections.ObjectModel;
using SVVideoDownloader.App.Services;
using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Core.Validation;
using SVVideoDownloader.Core.Videos;
using SVVideoDownloader.Infrastructure.ApplicationData;
using SVVideoDownloader.Infrastructure.Diagnostics;
using SVVideoDownloader.Infrastructure.ExternalTools;
using SVVideoDownloader.Infrastructure.Updates;

namespace SVVideoDownloader.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IVideoMetadataProvider _metadataProvider;
    private readonly IDownloadCoordinator _downloadCoordinator;
    private readonly IFolderPickerService _folderPickerService;
    private readonly IFileActionService _fileActionService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IUserDataService _userDataService;
    private readonly IToolManagementService _toolManagementService;
    private readonly IDiagnosticLogger _logger;
    private readonly object _settingsSync = new();
    private readonly object _historyTasksSync = new();
    private readonly HashSet<Guid> _recordedHistoryIds = [];
    private readonly HashSet<Task> _historyTasks = [];
    private CancellationTokenSource? _analysisCancellationSource;
    private Task _settingsSaveTask = Task.CompletedTask;
    private string _videoUrl = string.Empty;
    private string _outputFolder;
    private VideoInfo? _videoInfo;
    private QualityOptionViewModel? _selectedQuality;
    private bool _rightsConfirmed;
    private AnalysisState _analysisState = AnalysisState.Empty;
    private string _folderErrorMessage = string.Empty;
    private string _settingsMessage = string.Empty;
    private string _historyMessage = string.Empty;
    private string _toolsMessage =
        "Các công cụ được lưu riêng trong dữ liệu cục bộ của ứng dụng.";
    private string _analysisMessage =
        "Dán liên kết video công khai từ YouTube, TikTok hoặc Facebook để bắt đầu.";
    private bool _isRefreshingTools;
    private bool _isUpdatingYtDlp;
    private bool _isInitialized;
    private bool _disposed;

    public MainWindowViewModel(
        IVideoMetadataProvider metadataProvider,
        IDownloadCoordinator downloadCoordinator,
        IFolderPickerService folderPickerService,
        IFileActionService fileActionService,
        IUiDispatcher uiDispatcher,
        IUserDataService userDataService,
        IToolManagementService toolManagementService,
        IDiagnosticLogger logger,
        AppUiOptions options)
    {
        _metadataProvider = metadataProvider ??
            throw new ArgumentNullException(nameof(metadataProvider));
        _downloadCoordinator = downloadCoordinator ??
            throw new ArgumentNullException(nameof(downloadCoordinator));
        _folderPickerService = folderPickerService ??
            throw new ArgumentNullException(nameof(folderPickerService));
        _fileActionService = fileActionService ??
            throw new ArgumentNullException(nameof(fileActionService));
        _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        _userDataService = userDataService ?? throw new ArgumentNullException(nameof(userDataService));
        _toolManagementService = toolManagementService ??
            throw new ArgumentNullException(nameof(toolManagementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DefaultOutputFolder);

        _outputFolder = options.DefaultOutputFolder;
        QualityOptions =
        [
            new(QualityPreset.Best, "Tốt nhất"),
            new(QualityPreset.Video1080p, "Video 1080p"),
            new(QualityPreset.Video720p, "Video 720p"),
            new(QualityPreset.Video480p, "Video 480p"),
            new(QualityPreset.AudioMp3, "Âm thanh MP3"),
        ];
        _selectedQuality = QualityOptions.FirstOrDefault(item => item.Preset == options.DefaultQuality)
            ?? QualityOptions[0];
        ToolStatuses =
        [
            new(ExternalToolKind.YtDlp),
            new(ExternalToolKind.Ffmpeg),
            new(ExternalToolKind.Ffprobe),
        ];

        AnalyzeCommand = new AsyncRelayCommand(AnalyzeAsync, () => CanAnalyze);
        BrowseOutputFolderCommand = new AsyncRelayCommand(BrowseOutputFolderAsync);
        DownloadCommand = new RelayCommand(AddDownload, () => CanDownload);
        ClearHistoryCommand = new AsyncRelayCommand(ClearHistoryAsync, () => CanClearHistory);
        RefreshToolsCommand = new AsyncRelayCommand(RefreshToolsAsync, () => CanRefreshTools);
        UpdateYtDlpCommand = new AsyncRelayCommand(UpdateYtDlpAsync, () => CanUpdateYtDlp);
    }

    public string VideoUrl
    {
        get => _videoUrl;
        set
        {
            if (SetProperty(ref _videoUrl, value))
            {
                RightsConfirmed = false;
                if (_analysisState != AnalysisState.Empty || VideoInfo is not null)
                {
                    _analysisCancellationSource?.Cancel();
                    VideoInfo = null;
                    SetAnalysisState(
                        AnalysisState.Empty,
                        "Liên kết đã thay đổi. Chọn “Phân tích” để kiểm tra liên kết mới.");
                }

                AnalyzeCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string OutputFolder
    {
        get => _outputFolder;
        private set
        {
            if (SetProperty(ref _outputFolder, value))
            {
                DownloadCommand.NotifyCanExecuteChanged();
                QueueSettingsSave();
            }
        }
    }

    public IReadOnlyList<QualityOptionViewModel> QualityOptions { get; }

    public QualityOptionViewModel? SelectedQuality
    {
        get => _selectedQuality;
        set
        {
            if (SetProperty(ref _selectedQuality, value))
            {
                DownloadCommand.NotifyCanExecuteChanged();
                QueueSettingsSave();
            }
        }
    }

    public bool RightsConfirmed
    {
        get => _rightsConfirmed;
        set
        {
            if (SetProperty(ref _rightsConfirmed, value))
            {
                DownloadCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public VideoInfo? VideoInfo
    {
        get => _videoInfo;
        private set
        {
            if (SetProperty(ref _videoInfo, value))
            {
                OnPropertyChanged(nameof(VideoTitle));
                OnPropertyChanged(nameof(VideoSource));
                OnPropertyChanged(nameof(VideoDuration));
                OnPropertyChanged(nameof(ThumbnailUri));
                OnPropertyChanged(nameof(HasThumbnail));
                DownloadCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string VideoTitle => VideoInfo?.Title ?? string.Empty;

    public string VideoSource => VideoInfo is null
        ? string.Empty
        : DisplayFormatter.GetPlatformName(VideoInfo.Source.Platform);

    public string VideoDuration => DisplayFormatter.FormatDuration(VideoInfo?.Duration);

    public Uri? ThumbnailUri => VideoInfo?.ThumbnailUri;

    public bool HasThumbnail => ThumbnailUri is not null;

    public string AnalysisMessage
    {
        get => _analysisMessage;
        private set => SetProperty(ref _analysisMessage, value);
    }

    public bool IsAnalysisEmpty => _analysisState == AnalysisState.Empty;

    public bool IsAnalyzing => _analysisState == AnalysisState.Loading;

    public bool IsAnalysisSuccessful => _analysisState == AnalysisState.Success;

    public bool HasAnalysisError => _analysisState == AnalysisState.Error;

    public bool CanAnalyze =>
        !IsAnalyzing && !IsUpdatingYtDlp && !string.IsNullOrWhiteSpace(VideoUrl);

    public bool CanDownload =>
        VideoInfo is not null &&
        SelectedQuality is not null &&
        RightsConfirmed &&
        !IsUpdatingYtDlp &&
        !string.IsNullOrWhiteSpace(OutputFolder);

    public string FolderErrorMessage
    {
        get => _folderErrorMessage;
        private set
        {
            if (SetProperty(ref _folderErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasFolderError));
            }
        }
    }

    public bool HasFolderError => !string.IsNullOrWhiteSpace(FolderErrorMessage);

    public string SettingsMessage
    {
        get => _settingsMessage;
        private set
        {
            if (SetProperty(ref _settingsMessage, value))
            {
                OnPropertyChanged(nameof(HasSettingsMessage));
            }
        }
    }

    public bool HasSettingsMessage => !string.IsNullOrWhiteSpace(SettingsMessage);

    public ObservableCollection<DownloadItemViewModel> DownloadQueue { get; } = [];

    public bool IsQueueEmpty => DownloadQueue.Count == 0;

    public bool HasQueueItems => DownloadQueue.Count > 0;

    public bool HasActiveDownloads => DownloadQueue.Any(item => item.IsActive);

    public ObservableCollection<DownloadHistoryItemViewModel> DownloadHistory { get; } = [];

    public bool IsHistoryEmpty => DownloadHistory.Count == 0;

    public bool HasHistoryItems => DownloadHistory.Count > 0;

    public bool CanClearHistory => HasHistoryItems;

    public string HistoryMessage
    {
        get => _historyMessage;
        private set
        {
            if (SetProperty(ref _historyMessage, value))
            {
                OnPropertyChanged(nameof(HasHistoryMessage));
            }
        }
    }

    public bool HasHistoryMessage => !string.IsNullOrWhiteSpace(HistoryMessage);

    public ObservableCollection<ToolStatusItemViewModel> ToolStatuses { get; }

    public bool IsRefreshingTools
    {
        get => _isRefreshingTools;
        private set
        {
            if (SetProperty(ref _isRefreshingTools, value))
            {
                NotifyToolCommandsChanged();
            }
        }
    }

    public bool IsUpdatingYtDlp
    {
        get => _isUpdatingYtDlp;
        private set
        {
            if (SetProperty(ref _isUpdatingYtDlp, value))
            {
                NotifyToolCommandsChanged();
                AnalyzeCommand.NotifyCanExecuteChanged();
                DownloadCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool CanRefreshTools => !IsRefreshingTools && !IsUpdatingYtDlp;

    public bool CanUpdateYtDlp =>
        !IsUpdatingYtDlp &&
        !IsRefreshingTools &&
        !IsAnalyzing &&
        !HasActiveDownloads;

    public string ToolsMessage
    {
        get => _toolsMessage;
        private set => SetProperty(ref _toolsMessage, value);
    }

    public AsyncRelayCommand AnalyzeCommand { get; }

    public AsyncRelayCommand BrowseOutputFolderCommand { get; }

    public RelayCommand DownloadCommand { get; }

    public AsyncRelayCommand ClearHistoryCommand { get; }

    public AsyncRelayCommand RefreshToolsCommand { get; }

    public AsyncRelayCommand UpdateYtDlpCommand { get; }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        try
        {
            var entries = await _userDataService.LoadHistoryAsync();
            foreach (var entry in entries)
            {
                DownloadHistory.Add(new DownloadHistoryItemViewModel(entry, _fileActionService));
            }

            NotifyHistoryChanged();
        }
        catch (Exception)
        {
            HistoryMessage = "Không thể đọc lịch sử tải xuống.";
            await LogSafelyAsync(DiagnosticLogLevel.Warning, "Khởi tạo lịch sử thất bại.");
        }

        await RefreshToolsAsync();
    }

    public void CancelAllActiveDownloads()
    {
        foreach (var item in DownloadQueue.Where(item => item.IsActive).ToArray())
        {
            item.Cancel();
        }
    }

    public async Task PrepareForCloseAsync()
    {
        CancelAllActiveDownloads();
        var completionTasks = DownloadQueue.Select(item => item.WaitForCompletionAsync()).ToArray();
        if (completionTasks.Length > 0)
        {
            await Task.WhenAll(completionTasks);
        }

        Task[] historyTasks;
        lock (_historyTasksSync)
        {
            historyTasks = _historyTasks.ToArray();
        }

        if (historyTasks.Length > 0)
        {
            await Task.WhenAll(historyTasks);
        }

        Task settingsTask;
        lock (_settingsSync)
        {
            settingsTask = _settingsSaveTask;
        }

        await settingsTask;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _analysisCancellationSource?.Cancel();
        _analysisCancellationSource?.Dispose();
        _analysisCancellationSource = null;

        foreach (var item in DownloadQueue)
        {
            item.Dispose();
        }
    }

    private async Task AnalyzeAsync()
    {
        var sourceResult = SVVideoDownloader.Core.Videos.VideoSource.Create(VideoUrl);
        if (!sourceResult.IsSuccess)
        {
            VideoInfo = null;
            SetAnalysisState(AnalysisState.Error, TranslateValidation(sourceResult.Errors));
            return;
        }

        _analysisCancellationSource?.Cancel();
        _analysisCancellationSource?.Dispose();
        var cancellationSource = new CancellationTokenSource();
        _analysisCancellationSource = cancellationSource;
        var analyzedUrl = VideoUrl;
        VideoInfo = null;
        SetAnalysisState(AnalysisState.Loading, "Đang phân tích thông tin video…");

        try
        {
            var result = await _metadataProvider.GetVideoInfoAsync(
                sourceResult.Value!,
                cancellationSource.Token);

            if (!string.Equals(analyzedUrl, VideoUrl, StringComparison.Ordinal))
            {
                SetAnalysisState(
                    AnalysisState.Empty,
                    "Liên kết đã thay đổi. Chọn “Phân tích” để kiểm tra liên kết mới.");
                return;
            }

            if (!result.IsSuccess)
            {
                SetAnalysisState(AnalysisState.Error, MediaErrorTranslator.Translate(result.Error));
                await LogSafelyAsync(
                    DiagnosticLogLevel.Warning,
                    $"Phân tích video thất bại: {result.Error?.Category}");
                return;
            }

            VideoInfo = result.Value;
            SetAnalysisState(
                AnalysisState.Success,
                "Đã phân tích video. Nhận diện nguồn không bảo đảm nội dung luôn tải được.");
        }
        catch (OperationCanceledException)
        {
            SetAnalysisState(AnalysisState.Empty, "Đã dừng phân tích video.");
        }
        catch (Exception)
        {
            SetAnalysisState(
                AnalysisState.Error,
                "Không thể phân tích video. Hãy kiểm tra công cụ và thử lại.");
            await LogSafelyAsync(DiagnosticLogLevel.Error, "Phân tích video phát sinh lỗi.");
        }
        finally
        {
            if (ReferenceEquals(_analysisCancellationSource, cancellationSource))
            {
                _analysisCancellationSource = null;
            }

            cancellationSource.Dispose();
            NotifyToolCommandsChanged();
        }
    }

    private async Task BrowseOutputFolderAsync()
    {
        try
        {
            var selectedFolder = await _folderPickerService.PickFolderAsync(OutputFolder);
            if (!string.IsNullOrWhiteSpace(selectedFolder))
            {
                OutputFolder = selectedFolder;
                FolderErrorMessage = string.Empty;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            FolderErrorMessage = "Không thể chọn thư mục lưu. Hãy thử lại.";
            await LogSafelyAsync(DiagnosticLogLevel.Warning, "Chọn thư mục lưu thất bại.");
        }
    }

    private void AddDownload()
    {
        if (!CanDownload || VideoInfo is null || SelectedQuality is null)
        {
            return;
        }

        var optionsResult = DownloadOptions.Create(SelectedQuality.Preset, VideoInfo.Title);
        var requestResult = DownloadRequest.Create(
            VideoInfo.Source,
            optionsResult.Value,
            RightsConfirmed);

        if (!optionsResult.IsSuccess || !requestResult.IsSuccess)
        {
            SetAnalysisState(
                AnalysisState.Error,
                "Không thể tạo yêu cầu tải xuống từ các lựa chọn hiện tại.");
            return;
        }

        var item = new DownloadItemViewModel(
            VideoInfo,
            requestResult.Value!,
            OutputFolder,
            _downloadCoordinator,
            _fileActionService,
            _uiDispatcher,
            RemoveDownload);
        item.StateChanged += OnDownloadStateChanged;
        DownloadQueue.Insert(0, item);
        RightsConfirmed = false;
        NotifyQueueChanged();
        _ = item.StartAsync();
    }

    private void RemoveDownload(DownloadItemViewModel item)
    {
        if (!item.CanRemove || !DownloadQueue.Remove(item))
        {
            return;
        }

        item.StateChanged -= OnDownloadStateChanged;
        item.Dispose();
        NotifyQueueChanged();
    }

    private void OnDownloadStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(HasActiveDownloads));
        NotifyToolCommandsChanged();
        if (sender is DownloadItemViewModel
            {
                Status: DownloadStatus.Completed,
                FilePath: not null,
            } item && _recordedHistoryIds.Add(item.Id))
        {
            TrackHistoryTask(RecordHistoryAsync(item));
        }
    }

    private void TrackHistoryTask(Task task)
    {
        lock (_historyTasksSync)
        {
            _historyTasks.Add(task);
        }

        _ = RemoveHistoryTaskWhenCompleteAsync(task);
    }

    private async Task RemoveHistoryTaskWhenCompleteAsync(Task task)
    {
        try
        {
            await task;
        }
        finally
        {
            lock (_historyTasksSync)
            {
                _historyTasks.Remove(task);
            }
        }
    }

    private async Task RecordHistoryAsync(DownloadItemViewModel item)
    {
        var entry = new DownloadHistoryEntry(
            item.Id,
            item.Title,
            item.Platform,
            item.Quality,
            item.FilePath!,
            DateTimeOffset.UtcNow);
        try
        {
            await _userDataService.AddHistoryAsync(entry);
            DownloadHistory.Insert(
                0,
                new DownloadHistoryItemViewModel(entry, _fileActionService));
            HistoryMessage = string.Empty;
            NotifyHistoryChanged();
        }
        catch (Exception)
        {
            HistoryMessage = "Đã tải xong nhưng không thể lưu lịch sử.";
            await LogSafelyAsync(DiagnosticLogLevel.Warning, "Ghi lịch sử tải xuống thất bại.");
        }
    }

    private async Task ClearHistoryAsync()
    {
        try
        {
            await _userDataService.ClearHistoryAsync();
            DownloadHistory.Clear();
            HistoryMessage = "Đã xóa lịch sử. Các tệp video đã tải vẫn được giữ nguyên.";
            NotifyHistoryChanged();
        }
        catch (Exception)
        {
            HistoryMessage = "Không thể xóa lịch sử tải xuống.";
            await LogSafelyAsync(DiagnosticLogLevel.Warning, "Xóa lịch sử tải xuống thất bại.");
        }
    }

    private async Task RefreshToolsAsync()
    {
        IsRefreshingTools = true;
        foreach (var item in ToolStatuses)
        {
            item.SetChecking();
        }

        try
        {
            var statuses = await _toolManagementService.CheckStatusAsync();
            foreach (var status in statuses)
            {
                ToolStatuses.First(item => item.Tool == status.Tool).Apply(status);
            }

            ToolsMessage = statuses.All(item => item.IsAvailable)
                ? "Tất cả công cụ đã sẵn sàng."
                : "Một số công cụ chưa sẵn sàng. Xem đường dẫn và hướng dẫn thiết lập.";
        }
        catch (Exception)
        {
            ToolsMessage = "Không thể kiểm tra trạng thái công cụ.";
            await LogSafelyAsync(DiagnosticLogLevel.Warning, "Kiểm tra công cụ thất bại.");
        }
        finally
        {
            IsRefreshingTools = false;
        }
    }

    private async Task UpdateYtDlpAsync()
    {
        if (!CanUpdateYtDlp)
        {
            return;
        }

        IsUpdatingYtDlp = true;
        ToolsMessage = "Đang tải và xác minh bản yt-dlp mới từ kho phát hành chính thức…";
        string resultMessage;
        try
        {
            var operation = await _toolManagementService.UpdateYtDlpAsync();
            resultMessage = operation.WasBlocked
                ? "Không thể cập nhật khi đang phân tích hoặc tải xuống."
                : TranslateUpdateResult(operation.UpdateResult!);
        }
        catch (OperationCanceledException)
        {
            resultMessage = "Đã hủy cập nhật yt-dlp trước khi thay thế công cụ.";
        }
        catch (Exception)
        {
            resultMessage = "Cập nhật yt-dlp thất bại. Công cụ hiện có được giữ nguyên nếu có thể.";
            await LogSafelyAsync(DiagnosticLogLevel.Error, "Cập nhật yt-dlp phát sinh lỗi.");
        }
        finally
        {
            IsUpdatingYtDlp = false;
        }

        await RefreshToolsAsync();
        ToolsMessage = resultMessage;
    }

    private void QueueSettingsSave()
    {
        if (_disposed || SelectedQuality is null)
        {
            return;
        }

        var settings = new ApplicationSettings(OutputFolder, SelectedQuality.Preset);
        lock (_settingsSync)
        {
            _settingsSaveTask = SaveSettingsAfterAsync(_settingsSaveTask, settings);
        }
    }

    private async Task SaveSettingsAfterAsync(Task previousSave, ApplicationSettings settings)
    {
        await previousSave;
        try
        {
            await _userDataService.SaveSettingsAsync(settings);
            SettingsMessage = string.Empty;
        }
        catch (Exception)
        {
            SettingsMessage = "Không thể lưu cài đặt cá nhân.";
            await LogSafelyAsync(DiagnosticLogLevel.Warning, "Lưu cài đặt thất bại.");
        }
    }

    private void SetAnalysisState(AnalysisState state, string message)
    {
        _analysisState = state;
        AnalysisMessage = message;
        OnPropertyChanged(nameof(IsAnalysisEmpty));
        OnPropertyChanged(nameof(IsAnalyzing));
        OnPropertyChanged(nameof(IsAnalysisSuccessful));
        OnPropertyChanged(nameof(HasAnalysisError));
        OnPropertyChanged(nameof(CanAnalyze));
        AnalyzeCommand.NotifyCanExecuteChanged();
        DownloadCommand.NotifyCanExecuteChanged();
        NotifyToolCommandsChanged();
    }

    private void NotifyQueueChanged()
    {
        OnPropertyChanged(nameof(IsQueueEmpty));
        OnPropertyChanged(nameof(HasQueueItems));
        OnPropertyChanged(nameof(HasActiveDownloads));
        NotifyToolCommandsChanged();
    }

    private void NotifyHistoryChanged()
    {
        OnPropertyChanged(nameof(IsHistoryEmpty));
        OnPropertyChanged(nameof(HasHistoryItems));
        OnPropertyChanged(nameof(CanClearHistory));
        ClearHistoryCommand.NotifyCanExecuteChanged();
    }

    private void NotifyToolCommandsChanged()
    {
        OnPropertyChanged(nameof(CanRefreshTools));
        OnPropertyChanged(nameof(CanUpdateYtDlp));
        RefreshToolsCommand.NotifyCanExecuteChanged();
        UpdateYtDlpCommand.NotifyCanExecuteChanged();
    }

    private async Task LogSafelyAsync(DiagnosticLogLevel level, string message)
    {
        try
        {
            await _logger.LogAsync(level, message);
        }
        catch (Exception)
        {
        }
    }

    private static string TranslateUpdateResult(YtDlpUpdateResult result) => result.Status switch
    {
        YtDlpUpdateStatus.Success =>
            $"Đã cập nhật yt-dlp thành công lên phiên bản {result.InstalledVersion}.",
        YtDlpUpdateStatus.DownloadFailed =>
            "Không thể tải bản cập nhật yt-dlp.",
        YtDlpUpdateStatus.ChecksumUnavailable =>
            "Không tìm thấy checksum chính thức cho yt-dlp.exe; không thay đổi công cụ.",
        YtDlpUpdateStatus.ChecksumMismatch =>
            "Checksum không khớp; tệp tải về đã bị loại bỏ.",
        YtDlpUpdateStatus.InvalidDownloadedExecutable =>
            "Tệp tải về không phải executable yt-dlp hợp lệ; không thay đổi công cụ.",
        YtDlpUpdateStatus.ReplacementFailed =>
            "Không thể thay thế yt-dlp hiện có. Hãy đóng chương trình đang dùng tệp và thử lại.",
        YtDlpUpdateStatus.ValidationFailedAndRolledBack =>
            "Bản mới không hoạt động; ứng dụng đã khôi phục yt-dlp cũ.",
        YtDlpUpdateStatus.RollbackFailed =>
            "Khôi phục yt-dlp thất bại. Hãy xem nhật ký và thiết lập lại công cụ thủ công.",
        _ => "Cập nhật yt-dlp thất bại.",
    };

    private static string TranslateValidation(IReadOnlyList<ValidationError> errors)
    {
        var code = errors.FirstOrDefault()?.Code;
        return code switch
        {
            ValidationErrorCode.Required => "Hãy nhập liên kết video.",
            ValidationErrorCode.MalformedUrl => "Liên kết không đúng định dạng.",
            ValidationErrorCode.HttpsRequired => "Liên kết phải sử dụng HTTPS.",
            ValidationErrorCode.CredentialsNotAllowed =>
                "Liên kết không được chứa thông tin đăng nhập.",
            ValidationErrorCode.UnsupportedHost =>
                "Hiện chỉ hỗ trợ liên kết công khai từ YouTube, TikTok và Facebook.",
            _ => "Liên kết không hợp lệ.",
        };
    }

    private enum AnalysisState
    {
        Empty,
        Loading,
        Success,
        Error,
    }
}
