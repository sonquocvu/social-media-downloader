using System.Diagnostics;
using System.IO;

namespace SVVideoDownloader.App.Services;

public sealed class WindowsFileActionService : IFileActionService
{
    public Task<bool> OpenFileAsync(
        string filePath,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => Open(filePath, File.Exists), cancellationToken);

    public Task<bool> OpenFolderAsync(
        string folderPath,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => Open(folderPath, Directory.Exists), cancellationToken);

    private static bool Open(string path, Func<string, bool> exists)
    {
        try
        {
            if (!exists(path))
            {
                return false;
            }

            using var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                });
            return process is not null;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
