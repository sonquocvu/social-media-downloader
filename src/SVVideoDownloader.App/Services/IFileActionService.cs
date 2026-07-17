namespace SVVideoDownloader.App.Services;

public interface IFileActionService
{
    Task<bool> OpenFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    Task<bool> OpenFolderAsync(
        string folderPath,
        CancellationToken cancellationToken = default);
}
