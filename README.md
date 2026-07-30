# SVVideoDownloader

SVVideoDownloader là ứng dụng WPF riêng tư bằng tiếng Việt dành cho Windows x64.
Ứng dụng tải Video ở chế độ MP4 tương thích hoặc chất lượng gốc, cùng tệp MP3,
từ nội dung công khai do người dùng sở hữu hoặc được cho phép tải trên YouTube,
TikTok và Facebook.

Phiên bản hiện tại: **1.1.0**.

> Trạng thái hiện tại: đã sẵn sàng để thử nghiệm sử dụng riêng hằng ngày trên
> Windows x64. Ứng dụng có cài đặt được nhớ, lịch sử tải, nhật ký chẩn đoán xoay
> vòng, màn hình trạng thái công cụ và luồng cập nhật thủ công cho yt-dlp cùng
> gói FFmpeg/ffprobe. Kho mã và
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
- `yt-dlp.exe`, `ffmpeg.exe` và `ffprobe.exe` Windows x64 được cài thủ công từ
  màn hình công cụ hoặc thiết lập riêng theo [hướng dẫn cài đặt](docs/SETUP.md).

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

## Cài đặt phiên bản 1.1.0

Gói cài đặt x64 được tạo tại:

```text
artifacts\installer\SVVideoDownloader-1.1.0-win-x64.msi
```

Mở MSI và chấp nhận yêu cầu quyền quản trị để cài vào Program Files. Gói tạo
shortcut ở Start Menu và Desktop, hỗ trợ gỡ cài đặt trong Windows Settings và
không yêu cầu .NET runtime riêng. yt-dlp, FFmpeg và ffprobe không nằm trong MSI;
cài chúng thủ công từ màn hình “Công cụ và cài đặt” sau lần chạy đầu tiên.

MSI/executable 1.1.0 hiện chưa được ký mã nên Defender hoặc SmartScreen có thể
cảnh báo. Chỉ dùng artifact do bạn tự build hoặc nhận qua kênh tin cậy và kiểm
tra SHA-256 trước khi chạy. Xem [quy trình phát hành](docs/RELEASING.md) và
[nhật ký thay đổi](CHANGELOG.md) để tạo phiên bản mới.

## Giao diện hiện có

- Dán và phân tích URL HTTPS công khai của YouTube, TikTok hoặc Facebook.
- Hiển thị ảnh thu nhỏ, tiêu đề, nguồn và thời lượng khi metadata có sẵn.
- Chọn MP4 tương thích, chất lượng gốc tốt nhất hoặc MP3; chọn chất lượng video,
  thư mục lưu rồi xác nhận quyền ngay bằng hành động tải xuống, không cần ô chọn riêng.
- MP4 tương thích ưu tiên H.264/AAC và được FFmpeg chuyển sang MP4 khi cần.
- Chất lượng gốc giữ các luồng tốt nhất mà nguồn cung cấp, không mã hóa lại; tệp
  có thể là MP4, WebM hoặc MKV.
- MP3 lấy luồng âm thanh tốt nhất và được mã hóa bằng mức chất lượng VBR cao nhất.
- Theo dõi hàng đợi, phần trăm, dung lượng đã tải, tốc độ và thời gian còn lại.
- Hủy, thử lại, xóa mục hoàn tất, mở tệp và mở thư mục.
- Xác nhận bằng tiếng Việt khi đóng cửa sổ trong lúc còn tác vụ hoạt động.
- Giao diện sáng/tối đồng bộ, có nút chuyển nhanh và nhớ lựa chọn sau khi khởi động lại.
- Hệ thống thiết kế WPF riêng với thẻ bo góc, trạng thái tương phản rõ và điều hướng bàn phím.
- Trang Công cụ và cài đặt xếp dọc các tác vụ yt-dlp/FFmpeg, chỉ cần cuộn dọc.
- Nhớ thư mục tải, loại tệp/chất lượng mặc định trong `%LOCALAPPDATA%`.
- Lưu tối đa 500 tác vụ hoàn tất; xóa lịch sử không xóa media.
- Kiểm tra trạng thái/phiên bản yt-dlp, FFmpeg và ffprobe.
- Cập nhật yt-dlp chỉ khi người dùng yêu cầu, với SHA-256, thay thế nguyên tử và rollback.
- Cài/cập nhật FFmpeg và ffprobe cùng nhau từ gói Release Essentials x64 của
  gyan.dev sau khi người dùng xác nhận nguồn/GPLv3; có SHA-256 và rollback theo cặp.

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
[công việc](docs/TASKS.md) trước khi mở rộng chức năng. Quy trình đánh phiên bản,
nâng cấp MSI và phát hành nằm trong [RELEASING.md](docs/RELEASING.md).

## Dữ liệu cục bộ

Ứng dụng dùng `%LOCALAPPDATA%\SVVideoDownloader`:

- `settings.json`: thư mục tải, chất lượng mặc định và giao diện sáng/tối.
- `history.json`: lịch sử tác vụ hoàn tất, không chứa cookie.
- `logs`: nhật ký chẩn đoán xoay vòng, có che cookie/secret/token/URL.
- `tools`: executable ngoài do người dùng thiết lập hoặc cập nhật thủ công trong ứng dụng.

Không có telemetry. Ứng dụng không tự nhập cookie trình duyệt, không cập nhật
ngầm và không xóa media khi xóa hàng đợi hoặc lịch sử.

## Phụ thuộc bên thứ ba

`yt-dlp`, FFmpeg và ffprobe chạy dưới dạng executable ngoài tiến trình và không
được phân phối cùng ứng dụng. `Microsoft.Extensions.DependencyInjection`
được dùng cho composition root. Giấy phép của binary thực tế phải được rà soát
trước khi chọn nguồn và hình thức phân phối. Chi tiết nằm trong
[tài liệu kiến trúc](docs/ARCHITECTURE.md#phụ-thuộc-bên-thứ-ba-và-giấy-phép).

WiX Toolset v3.11 chỉ được dùng trên máy build để tạo MSI và không được nhúng
trong sản phẩm. WiX v3 đã hết hỗ trợ cộng đồng; cần chuyển sang nhánh còn được
hỗ trợ trước khi duy trì phát hành dài hạn.
