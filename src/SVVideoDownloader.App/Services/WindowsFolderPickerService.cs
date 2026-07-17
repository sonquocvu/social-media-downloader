using Microsoft.Win32;

namespace SVVideoDownloader.App.Services;

public sealed class WindowsFolderPickerService : IFolderPickerService
{
    public Task<string?> PickFolderAsync(
        string initialFolder,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dialog = new OpenFolderDialog
        {
            Title = "Chọn thư mục lưu video",
            InitialDirectory = initialFolder,
            Multiselect = false,
        };

        var selectedFolder = dialog.ShowDialog() == true ? dialog.FolderName : null;
        return Task.FromResult(selectedFolder);
    }
}
