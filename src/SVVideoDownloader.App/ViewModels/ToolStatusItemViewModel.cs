using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Infrastructure.ExternalTools;

namespace SVVideoDownloader.App.ViewModels;

public sealed class ToolStatusItemViewModel : ViewModelBase
{
    private bool _isAvailable;
    private string _versionText = "Chưa kiểm tra";
    private string _statusText = "Chưa kiểm tra";
    private string _executablePath = string.Empty;

    public ToolStatusItemViewModel(ExternalToolKind tool)
    {
        Tool = tool;
    }

    public ExternalToolKind Tool { get; }

    public string Name => Tool switch
    {
        ExternalToolKind.YtDlp => "yt-dlp",
        ExternalToolKind.Ffmpeg => "FFmpeg",
        ExternalToolKind.Ffprobe => "ffprobe",
        _ => "Công cụ không xác định",
    };

    public bool IsAvailable
    {
        get => _isAvailable;
        private set => SetProperty(ref _isAvailable, value);
    }

    public string VersionText
    {
        get => _versionText;
        private set => SetProperty(ref _versionText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string ExecutablePath
    {
        get => _executablePath;
        private set => SetProperty(ref _executablePath, value);
    }

    public void Apply(ExternalToolStatus status)
    {
        if (status.Tool != Tool)
        {
            throw new ArgumentException(null, nameof(status));
        }

        IsAvailable = status.IsAvailable;
        ExecutablePath = status.ExecutablePath;
        VersionText = status.Version ?? "Không xác định";
        StatusText = status.IsAvailable
            ? "Sẵn sàng"
            : TranslateError(status.ErrorCategory);
    }

    public void SetChecking()
    {
        StatusText = "Đang kiểm tra…";
        VersionText = "—";
    }

    private static string TranslateError(MediaErrorCategory? category) => category switch
    {
        MediaErrorCategory.DependencyMissing => "Không tìm thấy",
        MediaErrorCategory.DependencyInaccessible => "Không thể truy cập",
        MediaErrorCategory.DependencyInvalid => "Không hợp lệ",
        MediaErrorCategory.TimedOut => "Hết thời gian kiểm tra",
        _ => "Không sẵn sàng",
    };
}
