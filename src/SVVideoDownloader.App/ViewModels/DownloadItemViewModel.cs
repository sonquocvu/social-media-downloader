using System.Globalization;
using System.IO;
using SVVideoDownloader.App.Services;
using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.App.ViewModels;

public sealed class DownloadItemViewModel : ViewModelBase, IDisposable
{
    private readonly DownloadRequest _request;
    private readonly IDownloadCoordinator _downloadCoordinator;
    private readonly IFileActionService _fileActionService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly Action<DownloadItemViewModel> _remove;
    private CancellationTokenSource? _cancellationSource;
    private Task? _executionTask;
    private DownloadStatus _status = DownloadStatus.Pending;
    private double? _percentage;
    private long _downloadedBytes;
    private long? _totalBytes;
    private double? _bytesPerSecond;
    private string _errorMessage = string.Empty;
    private string _actionMessage = string.Empty;
    private string? _filePath;
    private bool _isDisposed;

    public DownloadItemViewModel(
        VideoInfo videoInfo,
        DownloadRequest request,
        string outputFolder,
        IDownloadCoordinator downloadCoordinator,
        IFileActionService fileActionService,
        IUiDispatcher uiDispatcher,
        Action<DownloadItemViewModel> remove)
    {
        ArgumentNullException.ThrowIfNull(videoInfo);
        _request = request ?? throw new ArgumentNullException(nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFolder);
        _downloadCoordinator = downloadCoordinator ??
            throw new ArgumentNullException(nameof(downloadCoordinator));
        _fileActionService = fileActionService ??
            throw new ArgumentNullException(nameof(fileActionService));
        _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        _remove = remove ?? throw new ArgumentNullException(nameof(remove));

        Title = videoInfo.Title;
        Platform = videoInfo.Source.Platform;
        Quality = request.Options.QualityPreset;
        SourceText = DisplayFormatter.GetPlatformName(videoInfo.Source.Platform);
        OutputFolder = outputFolder;

        CancelCommand = new RelayCommand(Cancel, () => CanCancel);
        RetryCommand = new AsyncRelayCommand(RetryAsync, () => CanRetry);
        RemoveCommand = new RelayCommand(() => _remove(this), () => CanRemove);
        OpenFileCommand = new AsyncRelayCommand(OpenFileAsync, () => CanOpenFile);
        OpenFolderCommand = new AsyncRelayCommand(OpenFolderAsync, () => CanOpenFolder);
    }

    public event EventHandler? StateChanged;

    public string Title { get; }

    public Guid Id { get; } = Guid.NewGuid();

    public SupportedPlatform Platform { get; }

    public QualityPreset Quality { get; }

    public string SourceText { get; }

    public string OutputFolder { get; }

    public DownloadStatus Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                NotifyStateChanged();
            }
        }
    }

    public string StatusText => Status switch
    {
        DownloadStatus.Pending => "Đang chờ",
        DownloadStatus.Analyzing => "Đang phân tích",
        DownloadStatus.Ready => "Sẵn sàng",
        DownloadStatus.Downloading => "Đang tải xuống",
        DownloadStatus.Processing => "Đang xử lý tệp",
        DownloadStatus.Completed => "Đã hoàn tất",
        DownloadStatus.Failed => "Tải xuống thất bại",
        DownloadStatus.Cancelled => "Đã hủy",
        _ => "Không xác định",
    };

    public bool IsActive =>
        Status is DownloadStatus.Pending or
            DownloadStatus.Analyzing or
            DownloadStatus.Downloading or
            DownloadStatus.Processing;

    public bool CanCancel => !_isDisposed && IsActive;

    public bool CanRetry =>
        !_isDisposed &&
        Status is (DownloadStatus.Failed or DownloadStatus.Cancelled);

    public bool CanRemove => !_isDisposed && Status == DownloadStatus.Completed;

    public bool CanOpenFile =>
        !_isDisposed && Status == DownloadStatus.Completed && FilePath is not null;

    public bool CanOpenFolder => !_isDisposed && Status == DownloadStatus.Completed;

    public double PercentageValue => _percentage ?? 0;

    public string PercentageText => _percentage is { } percentage
        ? $"{percentage.ToString("0.0", CultureInfo.GetCultureInfo("vi-VN"))} %"
        : "—";

    public bool IsProgressIndeterminate => IsActive && _percentage is null;

    public string DownloadedSizeText => DisplayFormatter.FormatBytes(_downloadedBytes);

    public string SpeedText => _bytesPerSecond is > 0
        ? $"{DisplayFormatter.FormatBytes((long)_bytesPerSecond.Value)}/giây"
        : "—";

    public string EtaText
    {
        get
        {
            if (_bytesPerSecond is not > 0 ||
                _totalBytes is not { } totalBytes ||
                totalBytes <= _downloadedBytes)
            {
                return "—";
            }

            var seconds = (totalBytes - _downloadedBytes) / _bytesPerSecond.Value;
            return double.IsFinite(seconds) && seconds >= 0
                ? DisplayFormatter.FormatEta(TimeSpan.FromSeconds(seconds))
                : "—";
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasErrorMessage));
            }
        }
    }

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string ActionMessage
    {
        get => _actionMessage;
        private set
        {
            if (SetProperty(ref _actionMessage, value))
            {
                OnPropertyChanged(nameof(HasActionMessage));
            }
        }
    }

    public bool HasActionMessage => !string.IsNullOrWhiteSpace(ActionMessage);

    public string? FilePath
    {
        get => _filePath;
        private set
        {
            if (SetProperty(ref _filePath, value))
            {
                OnPropertyChanged(nameof(CanOpenFile));
                OpenFileCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public RelayCommand CancelCommand { get; }

    public AsyncRelayCommand RetryCommand { get; }

    public RelayCommand RemoveCommand { get; }

    public AsyncRelayCommand OpenFileCommand { get; }

    public AsyncRelayCommand OpenFolderCommand { get; }

    public Task StartAsync()
    {
        if (_isDisposed)
        {
            return Task.CompletedTask;
        }

        if (_executionTask is not null)
        {
            return _executionTask;
        }

        _executionTask = RunAsync();
        return _executionTask;
    }

    public Task WaitForCompletionAsync() => _executionTask ?? Task.CompletedTask;

    public void Cancel()
    {
        if (!CanCancel)
        {
            return;
        }

        _cancellationSource?.Cancel();
        Status = DownloadStatus.Cancelled;
        ErrorMessage = string.Empty;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _cancellationSource?.Cancel();
        _cancellationSource?.Dispose();
        _cancellationSource = null;
        NotifyStateChanged();
    }

    private async Task RunAsync()
    {
        _cancellationSource?.Dispose();
        var cancellationSource = new CancellationTokenSource();
        _cancellationSource = cancellationSource;
        ResetProgress();
        ErrorMessage = string.Empty;
        ActionMessage = string.Empty;
        FilePath = null;
        Status = DownloadStatus.Downloading;

        var progress = new CallbackProgress<DownloadProgress>(
            value => _uiDispatcher.Post(() => ApplyProgress(value)));

        try
        {
            var result = await _downloadCoordinator.DownloadAsync(
                _request,
                OutputFolder,
                progress,
                cancellationSource.Token);

            if (cancellationSource.IsCancellationRequested)
            {
                Status = DownloadStatus.Cancelled;
                ErrorMessage = string.Empty;
                return;
            }

            if (!result.IsSuccess)
            {
                Status = DownloadStatus.Failed;
                ErrorMessage = MediaErrorTranslator.Translate(result.Error);
                return;
            }

            _percentage = 100;
            if (_totalBytes is { } totalBytes)
            {
                _downloadedBytes = totalBytes;
            }

            _bytesPerSecond = null;
            FilePath = Path.Combine(OutputFolder, result.Value!.OutputFileName);
            Status = DownloadStatus.Completed;
            NotifyProgressChanged();
        }
        catch (OperationCanceledException)
        {
            Status = DownloadStatus.Cancelled;
            ErrorMessage = string.Empty;
        }
        catch (Exception)
        {
            Status = DownloadStatus.Failed;
            ErrorMessage = "Không thể hoàn tất tải xuống. Hãy thử lại.";
        }
        finally
        {
            if (ReferenceEquals(_cancellationSource, cancellationSource))
            {
                _cancellationSource = null;
            }

            cancellationSource.Dispose();
        }
    }

    private async Task RetryAsync()
    {
        if (!CanRetry)
        {
            return;
        }

        if (_executionTask is not null)
        {
            await _executionTask;
        }

        if (CanRetry)
        {
            _executionTask = null;
            await StartAsync();
        }
    }

    private async Task OpenFileAsync()
    {
        if (FilePath is null)
        {
            return;
        }

        try
        {
            var opened = await _fileActionService.OpenFileAsync(FilePath);
            ActionMessage = opened ? string.Empty : "Không thể mở tệp đã tải.";
        }
        catch (Exception)
        {
            ActionMessage = "Không thể mở tệp đã tải.";
        }
    }

    private async Task OpenFolderAsync()
    {
        try
        {
            var opened = await _fileActionService.OpenFolderAsync(OutputFolder);
            ActionMessage = opened ? string.Empty : "Không thể mở thư mục lưu tệp.";
        }
        catch (Exception)
        {
            ActionMessage = "Không thể mở thư mục lưu tệp.";
        }
    }

    private void ApplyProgress(DownloadProgress progress)
    {
        _percentage = progress.Percentage;
        _downloadedBytes = progress.DownloadedBytes;
        _totalBytes = progress.TotalBytes;
        _bytesPerSecond = progress.BytesPerSecond;

        if (Status == DownloadStatus.Downloading && progress.Percentage is >= 100)
        {
            Status = DownloadStatus.Processing;
        }

        NotifyProgressChanged();
    }

    private void ResetProgress()
    {
        _percentage = null;
        _downloadedBytes = 0;
        _totalBytes = null;
        _bytesPerSecond = null;
        NotifyProgressChanged();
    }

    private void NotifyProgressChanged()
    {
        OnPropertyChanged(nameof(PercentageValue));
        OnPropertyChanged(nameof(PercentageText));
        OnPropertyChanged(nameof(IsProgressIndeterminate));
        OnPropertyChanged(nameof(DownloadedSizeText));
        OnPropertyChanged(nameof(SpeedText));
        OnPropertyChanged(nameof(EtaText));
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(CanCancel));
        OnPropertyChanged(nameof(CanRetry));
        OnPropertyChanged(nameof(CanRemove));
        OnPropertyChanged(nameof(CanOpenFile));
        OnPropertyChanged(nameof(CanOpenFolder));
        OnPropertyChanged(nameof(IsProgressIndeterminate));
        CancelCommand.NotifyCanExecuteChanged();
        RetryCommand.NotifyCanExecuteChanged();
        RemoveCommand.NotifyCanExecuteChanged();
        OpenFileCommand.NotifyCanExecuteChanged();
        OpenFolderCommand.NotifyCanExecuteChanged();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class CallbackProgress<T>(Action<T> callback) : IProgress<T>
    {
        public void Report(T value) => callback(value);
    }
}
