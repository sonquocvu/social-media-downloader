using SVVideoDownloader.Core.Files;
using SVVideoDownloader.Core.Validation;

namespace SVVideoDownloader.Core.Downloads;

public sealed record DownloadResult
{
    private DownloadResult(string outputFileName)
    {
        OutputFileName = outputFileName;
    }

    public string OutputFileName { get; }

    public static ValidationResult<DownloadResult> Create(string? outputFileName)
    {
        var fileNameResult = WindowsFileNameSanitizer.Sanitize(outputFileName);
        return fileNameResult.IsSuccess
            ? ValidationResult<DownloadResult>.Success(
                new DownloadResult(fileNameResult.Value!))
            : ValidationResult<DownloadResult>.Failure(fileNameResult.Errors);
    }
}
