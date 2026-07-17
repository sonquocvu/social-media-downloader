namespace SVVideoDownloader.App.Services;

public interface IEngineOperationGate
{
    int ActiveDownloadCount { get; }

    bool IsUpdateActive { get; }

    IDisposable? TryEnterMetadataOperation();

    IDisposable? TryEnterDownload();

    IDisposable? TryEnterUpdate();
}
