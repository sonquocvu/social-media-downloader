using System.Collections.ObjectModel;
using SVVideoDownloader.App.Services;
using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Core.Validation;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IVideoMetadataProvider _metadataProvider;
    private readonly IDownloadCoordinator _downloadCoordinator;
    private readonly IFolderPickerService _folderPickerService;
    private readonly IFileActionService _fileActionService;
    private readonly IUiDispatcher _uiDispatcher;
    private CancellationTokenSource? _analysisCancellationSource;
    private string _videoUrl = string.Empty;
    private string _outputFolder;
    private VideoInfo? _videoInfo;
    private QualityOptionViewModel? _selectedQuality;
    private bool _rightsConfirmed;
    private AnalysisState _analysisState = AnalysisState.Empty;
    private string _folderErrorMessage = string.Empty;
    private string _analysisMessage =
        "Dán liên kết video công khai từ YouTube, TikTok hoặc Facebook để bắt đầu.";

    public MainWindowViewModel(
        IVideoMetadataProvider metadataProvider,
        IDownloadCoordinator downloadCoordinator,
        IFolderPickerService folderPickerService,
        IFileActionService fileActionService,
        IUiDispatcher uiDispatcher,
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
        _selectedQuality = QualityOptions[0];

        AnalyzeCommand = new AsyncRelayCommand(AnalyzeAsync, () => CanAnalyze);
        BrowseOutputFolderCommand = new AsyncRelayCommand(BrowseOutputFolderAsync);
        DownloadCommand = new RelayCommand(AddDownload, () => CanDownload);
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

    public bool CanAnalyze => !IsAnalyzing && !string.IsNullOrWhiteSpace(VideoUrl);

    public bool CanDownload =>
        VideoInfo is not null &&
        SelectedQuality is not null &&
        RightsConfirmed &&
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

    public ObservableCollection<DownloadItemViewModel> DownloadQueue { get; } = [];

    public bool IsQueueEmpty => DownloadQueue.Count == 0;

    public bool HasQueueItems => DownloadQueue.Count > 0;

    public bool HasActiveDownloads => DownloadQueue.Any(item => item.IsActive);

    public AsyncRelayCommand AnalyzeCommand { get; }

    public AsyncRelayCommand BrowseOutputFolderCommand { get; }

    public RelayCommand DownloadCommand { get; }

    public void CancelAllActiveDownloads()
    {
        foreach (var item in DownloadQueue.Where(item => item.IsActive).ToArray())
        {
            item.Cancel();
        }
    }

    public void Dispose()
    {
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
        }
        finally
        {
            if (ReferenceEquals(_analysisCancellationSource, cancellationSource))
            {
                _analysisCancellationSource = null;
            }

            cancellationSource.Dispose();
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
            // Closing the picker is a normal user action.
        }
        catch (Exception)
        {
            FolderErrorMessage = "Không thể chọn thư mục lưu. Hãy thử lại.";
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
        OnPropertyChanged(nameof(IsQueueEmpty));
        OnPropertyChanged(nameof(HasQueueItems));
        OnPropertyChanged(nameof(HasActiveDownloads));
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
        OnPropertyChanged(nameof(IsQueueEmpty));
        OnPropertyChanged(nameof(HasQueueItems));
        OnPropertyChanged(nameof(HasActiveDownloads));
    }

    private void OnDownloadStateChanged(object? sender, EventArgs e) =>
        OnPropertyChanged(nameof(HasActiveDownloads));

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
    }

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
