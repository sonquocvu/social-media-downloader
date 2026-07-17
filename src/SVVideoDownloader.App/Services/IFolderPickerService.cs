namespace SVVideoDownloader.App.Services;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync(
        string initialFolder,
        CancellationToken cancellationToken = default);
}
