using System.ComponentModel;
using System.Windows;
using SVVideoDownloader.App.ViewModels;

namespace SVVideoDownloader.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = _viewModel;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_viewModel.HasActiveDownloads)
        {
            var answer = MessageBox.Show(
                this,
                "Một hoặc nhiều tác vụ đang tải. Nếu thoát, các tác vụ này sẽ bị hủy. Bạn có muốn thoát không?",
                "Xác nhận thoát",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (answer != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            _viewModel.CancelAllActiveDownloads();
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
