using System.Windows;

namespace SVVideoDownloader.App.Services;

public sealed class WpfUiDispatcher : IUiDispatcher
{
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var dispatcher = Application.Current.Dispatcher;

        if (dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _ = dispatcher.BeginInvoke(action);
        }
    }
}
