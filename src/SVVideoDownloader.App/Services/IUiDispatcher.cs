namespace SVVideoDownloader.App.Services;

public interface IUiDispatcher
{
    void Post(Action action);
}
