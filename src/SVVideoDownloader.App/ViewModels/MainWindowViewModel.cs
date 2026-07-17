using System.Windows.Input;

namespace SVVideoDownloader.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private string _videoUrl = string.Empty;
    private string _statusMessage = "Đây là bản dựng khởi đầu; chức năng tải video chưa được triển khai.";

    public MainWindowViewModel()
    {
        ShowNotImplementedCommand = new RelayCommand(ShowNotImplemented);
    }

    public string VideoUrl
    {
        get => _videoUrl;
        set => SetProperty(ref _videoUrl, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ICommand ShowNotImplementedCommand { get; }

    private void ShowNotImplemented()
    {
        StatusMessage = "Chức năng tải video sẽ được bổ sung ở giai đoạn tiếp theo.";
    }
}
