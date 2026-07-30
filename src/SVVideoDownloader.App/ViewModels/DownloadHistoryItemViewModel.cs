using System.IO;
using SVVideoDownloader.App.Services;
using SVVideoDownloader.Infrastructure.ApplicationData;

namespace SVVideoDownloader.App.ViewModels;

public sealed class DownloadHistoryItemViewModel : ViewModelBase
{
    private readonly IFileActionService _fileActionService;
    private string _actionMessage = string.Empty;

    public DownloadHistoryItemViewModel(
        DownloadHistoryEntry entry,
        IFileActionService fileActionService)
    {
        Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        _fileActionService = fileActionService ??
            throw new ArgumentNullException(nameof(fileActionService));
        OpenFileCommand = new AsyncRelayCommand(OpenFileAsync);
        OpenFolderCommand = new AsyncRelayCommand(OpenFolderAsync);
    }

    public DownloadHistoryEntry Entry { get; }

    public string Title => Entry.Title;

    public string SourceText => DisplayFormatter.GetPlatformName(Entry.Platform);

    public string FormatText => DisplayFormatter.GetMediaFormatName(Entry.Format, Entry.Quality);

    public string QualityText => DisplayFormatter.GetQualityName(Entry.Quality);

    public string CompletedAtText => Entry.CompletedAtUtc
        .ToLocalTime()
        .ToString("dd/MM/yyyy HH:mm");

    public string FilePath => Entry.FilePath;

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

    public AsyncRelayCommand OpenFileCommand { get; }

    public AsyncRelayCommand OpenFolderCommand { get; }

    private async Task OpenFileAsync()
    {
        try
        {
            var opened = await _fileActionService.OpenFileAsync(FilePath);
            ActionMessage = opened ? string.Empty : "Không thể mở tệp trong lịch sử.";
        }
        catch (Exception)
        {
            ActionMessage = "Không thể mở tệp trong lịch sử.";
        }
    }

    private async Task OpenFolderAsync()
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            ActionMessage = "Không thể xác định thư mục chứa tệp.";
            return;
        }

        try
        {
            var opened = await _fileActionService.OpenFolderAsync(directory);
            ActionMessage = opened ? string.Empty : "Không thể mở thư mục chứa tệp.";
        }
        catch (Exception)
        {
            ActionMessage = "Không thể mở thư mục chứa tệp.";
        }
    }
}
