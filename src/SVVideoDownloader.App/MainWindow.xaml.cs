using System.ComponentModel;
using System.Windows;
using SVVideoDownloader.App.ViewModels;

namespace SVVideoDownloader.App;

public partial class MainWindow : Window
{
    private bool _closeApproved;
    private bool _closePreparationStarted;

    public MainWindow(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = ViewModel;
        SourceInitialized += OnSourceInitialized;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    public MainWindowViewModel ViewModel { get; }

    protected override async void OnClosing(CancelEventArgs e)
    {
        if (_closeApproved)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        if (_closePreparationStarted)
        {
            return;
        }

        if (ViewModel.IsUpdatingYtDlp)
        {
            MessageBox.Show(
                this,
                "Đang cập nhật yt-dlp. Hãy đợi cập nhật hoàn tất rồi đóng ứng dụng.",
                "Chưa thể thoát",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (ViewModel.HasActiveDownloads)
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
                return;
            }
        }

        _closePreparationStarted = true;
        try
        {
            await ViewModel.PrepareForCloseAsync();
            _closeApproved = true;
            _ = Dispatcher.BeginInvoke(new Action(Close));
        }
        catch (Exception)
        {
            _closePreparationStarted = false;
            MessageBox.Show(
                this,
                "Không thể hoàn tất việc đóng ứng dụng an toàn. Hãy thử lại.",
                "Không thể thoát",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        SourceInitialized -= OnSourceInitialized;
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Dispose();
        base.OnClosed(e);
    }

    private void OnSourceInitialized(object? sender, EventArgs e) =>
        NativeWindowTheme.Apply(this, ViewModel.IsDarkMode);

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsDarkMode))
        {
            NativeWindowTheme.Apply(this, ViewModel.IsDarkMode);
        }
    }
}
