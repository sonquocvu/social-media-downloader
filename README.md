# SVVideoDownloader

SVVideoDownloader là ứng dụng WPF riêng tư bằng tiếng Việt dành cho Windows x64. Mục tiêu của ứng dụng là tải video công khai do người dùng sở hữu hoặc được cho phép tải từ YouTube, TikTok và Facebook.

> Trạng thái hiện tại: đã sẵn sàng để thử nghiệm sử dụng riêng hằng ngày trên
> Windows x64. Ứng dụng có cài đặt được nhớ, lịch sử tải, nhật ký chẩn đoán xoay
> vòng, màn hình trạng thái công cụ và luồng cập nhật yt-dlp thủ công. Kho mã và
> gói publish không chứa `yt-dlp`, FFmpeg, ffprobe hoặc media tải về.

## Nguyên tắc sử dụng

- Chỉ tải nội dung do người dùng sở hữu hoặc có quyền tải.
- Không vượt DRM, tường phí hoặc CAPTCHA.
- Không truy cập tài khoản trái phép và không thu thập thông tin đăng nhập.
- Người dùng chịu trách nhiệm tuân thủ bản quyền, điều khoản của nền tảng và pháp luật áp dụng.

## Yêu cầu phát triển

- Windows x64.
- .NET 10 SDK có hỗ trợ WPF.
- Công cụ dòng lệnh `dotnet`.
- `yt-dlp.exe`, `ffmpeg.exe` và `ffprobe.exe` Windows x64 được thiết lập riêng
  theo [hướng dẫn cài đặt](docs/SETUP.md).

## Bắt đầu

```powershell
dotnet restore .\SVVideoDownloader.sln
dotnet build .\SVVideoDownloader.sln --configuration Release
dotnet test .\SVVideoDownloader.sln --configuration Release --no-build
dotnet publish .\src\SVVideoDownloader.App\SVVideoDownloader.App.csproj -p:PublishProfile=win-x64
```

Chạy ứng dụng:

```powershell
dotnet run --project .\src\SVVideoDownloader.App\SVVideoDownloader.App.csproj --configuration Release
```

Gói self-contained được tạo tại `artifacts/publish/win-x64`. Máy sử dụng không
cần cài .NET runtime riêng. Xem [thiết lập Windows](docs/SETUP.md) và
[khắc phục sự cố](docs/TROUBLESHOOTING.md).

## Giao diện hiện có

- Dán và phân tích URL HTTPS công khai của YouTube, TikTok hoặc Facebook.
- Hiển thị ảnh thu nhỏ, tiêu đề, nguồn và thời lượng khi metadata có sẵn.
- Chọn preset chất lượng, thư mục lưu và xác nhận quyền trước mỗi tác vụ.
- Theo dõi hàng đợi, phần trăm, dung lượng đã tải, tốc độ và thời gian còn lại.
- Hủy, thử lại, xóa mục hoàn tất, mở tệp và mở thư mục.
- Xác nhận bằng tiếng Việt khi đóng cửa sổ trong lúc còn tác vụ hoạt động.
- Nhớ thư mục tải và chất lượng mặc định trong `%LOCALAPPDATA%`.
- Lưu tối đa 500 tác vụ hoàn tất; xóa lịch sử không xóa media.
- Kiểm tra trạng thái/phiên bản yt-dlp, FFmpeg và ffprobe.
- Cập nhật yt-dlp chỉ khi người dùng yêu cầu, với SHA-256, thay thế nguyên tử và rollback.

Nhận diện host chỉ xác nhận hình dạng liên kết và nền tảng được hỗ trợ; không bảo
đảm nội dung tồn tại, công khai, tải được hoặc người dùng có quyền tải. MVP luôn
xử lý URL playlist như một video trừ khi phạm vi được thay đổi rõ ràng.

## Cấu trúc

- `src/SVVideoDownloader.App`: WPF, MVVM và composition root.
- `src/SVVideoDownloader.Core`: mô hình và quy tắc nghiệp vụ thuần .NET.
- `src/SVVideoDownloader.Infrastructure`: biên tích hợp dành cho filesystem, process và công cụ ngoài trong các giai đoạn sau.
- `tests`: kiểm thử xUnit cho Core, Infrastructure và ViewModel của App.
- `docs`: đặc tả sản phẩm, kiến trúc và danh sách công việc.

Xem [đặc tả sản phẩm](docs/PRODUCT_SPEC.md), [kiến trúc](docs/ARCHITECTURE.md),
[thiết lập](docs/SETUP.md), [khắc phục sự cố](docs/TROUBLESHOOTING.md) và
[công việc](docs/TASKS.md) trước khi mở rộng chức năng.

## Dữ liệu cục bộ

Ứng dụng dùng `%LOCALAPPDATA%\SVVideoDownloader`:

- `settings.json`: thư mục tải và chất lượng mặc định.
- `history.json`: lịch sử tác vụ hoàn tất, không chứa cookie.
- `logs`: nhật ký chẩn đoán xoay vòng, có che cookie/secret/token/URL.
- `tools`: executable ngoài do người dùng thiết lập hoặc yt-dlp được cập nhật thủ công.

Không có telemetry. Ứng dụng không tự nhập cookie trình duyệt, không cập nhật
ngầm và không xóa media khi xóa hàng đợi hoặc lịch sử.

## Phụ thuộc bên thứ ba

`yt-dlp`, FFmpeg và ffprobe chạy dưới dạng executable ngoài tiến trình và không
được phân phối cùng ứng dụng. `Microsoft.Extensions.DependencyInjection`
được dùng cho composition root. Giấy phép của binary thực tế phải được rà soát
trước khi chọn nguồn và hình thức phân phối. Chi tiết nằm trong
[tài liệu kiến trúc](docs/ARCHITECTURE.md#phụ-thuộc-bên-thứ-ba-và-giấy-phép).
