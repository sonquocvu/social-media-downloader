using SVVideoDownloader.Core.Media;

namespace SVVideoDownloader.App.ViewModels;

internal static class MediaErrorTranslator
{
    public static string Translate(MediaOperationError? error)
    {
        if (error is null)
        {
            return "Đã xảy ra lỗi không xác định.";
        }

        return error.Category switch
        {
            MediaErrorCategory.InvalidRequest => "Yêu cầu không hợp lệ.",
            MediaErrorCategory.DependencyMissing =>
                $"Không tìm thấy {GetComponentName(error.Component)}.",
            MediaErrorCategory.DependencyInaccessible =>
                $"Không thể truy cập {GetComponentName(error.Component)}.",
            MediaErrorCategory.DependencyInvalid =>
                $"{GetComponentName(error.Component)} không hợp lệ hoặc không tương thích.",
            MediaErrorCategory.SourceUnavailable =>
                "Không thể truy cập video công khai này. Hãy kiểm tra liên kết và quyền truy cập.",
            MediaErrorCategory.InvalidResponse =>
                "Dữ liệu trả về không hợp lệ. Hãy thử lại sau.",
            MediaErrorCategory.TimedOut =>
                "Tác vụ đã hết thời gian chờ. Hãy thử lại.",
            MediaErrorCategory.ExecutionFailed =>
                "Không thể hoàn tất tác vụ. Hãy kiểm tra công cụ và thử lại.",
            _ => "Đã xảy ra lỗi không xác định.",
        };
    }

    private static string GetComponentName(MediaComponent component) => component switch
    {
        MediaComponent.MetadataExtractor => "yt-dlp",
        MediaComponent.MediaProcessor => "FFmpeg",
        MediaComponent.MediaProbe => "ffprobe",
        MediaComponent.Source => "nguồn video",
        _ => "công cụ cần thiết",
    };
}
