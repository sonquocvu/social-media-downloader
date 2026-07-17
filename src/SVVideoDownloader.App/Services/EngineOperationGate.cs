namespace SVVideoDownloader.App.Services;

public sealed class EngineOperationGate : IEngineOperationGate
{
    private readonly object _sync = new();
    private int _activeOperations;
    private int _activeDownloads;
    private bool _updateActive;

    public int ActiveDownloadCount
    {
        get
        {
            lock (_sync)
            {
                return _activeDownloads;
            }
        }
    }

    public bool IsUpdateActive
    {
        get
        {
            lock (_sync)
            {
                return _updateActive;
            }
        }
    }

    public IDisposable? TryEnterMetadataOperation() => TryEnterOperation(isDownload: false);

    public IDisposable? TryEnterDownload() => TryEnterOperation(isDownload: true);

    public IDisposable? TryEnterUpdate()
    {
        lock (_sync)
        {
            if (_updateActive || _activeOperations > 0)
            {
                return null;
            }

            _updateActive = true;
            return new Lease(ExitUpdate);
        }
    }

    private IDisposable? TryEnterOperation(bool isDownload)
    {
        lock (_sync)
        {
            if (_updateActive)
            {
                return null;
            }

            _activeOperations++;
            if (isDownload)
            {
                _activeDownloads++;
            }

            return new Lease(() => ExitOperation(isDownload));
        }
    }

    private void ExitOperation(bool isDownload)
    {
        lock (_sync)
        {
            _activeOperations--;
            if (isDownload)
            {
                _activeDownloads--;
            }
        }
    }

    private void ExitUpdate()
    {
        lock (_sync)
        {
            _updateActive = false;
        }
    }

    private sealed class Lease(Action release) : IDisposable
    {
        private Action? _release = release;

        public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
    }
}
