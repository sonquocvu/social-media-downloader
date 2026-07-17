# SVVideoDownloader

SVVideoDownloader là ứng dụng WPF riêng tư bằng tiếng Việt dành cho Windows x64. Mục tiêu của ứng dụng là tải video công khai do người dùng sở hữu hoặc được cho phép tải từ YouTube, TikTok và Facebook.

> Trạng thái hiện tại: đã có giao diện WPF/MVVM sử dụng được và đã nối với adapter
> `yt-dlp`/FFmpeg. Kho mã không tải hoặc chứa `yt-dlp`, `ffmpeg`, `ffprobe` hay
> binary bên thứ ba nào; vì vậy chức năng phân tích/tải chỉ hoạt động khi nhà phát
> triển tự cung cấp đúng các công cụ ngoài đã được rà soát.

## Nguyên tắc sử dụng

- Chỉ tải nội dung do người dùng sở hữu hoặc có quyền tải.
- Không vượt DRM, tường phí hoặc CAPTCHA.
- Không truy cập tài khoản trái phép và không thu thập thông tin đăng nhập.
- Người dùng chịu trách nhiệm tuân thủ bản quyền, điều khoản của nền tảng và pháp luật áp dụng.

## Yêu cầu phát triển

- Windows x64.
- .NET 10 SDK có hỗ trợ WPF.
- Công cụ dòng lệnh `dotnet`.
- Ba executable Windows x64 do nhà phát triển tự cung cấp khi thử luồng thật:
  `yt-dlp.exe`, `ffmpeg.exe` và `ffprobe.exe`.

## Bắt đầu

```powershell
dotnet restore .\SVVideoDownloader.sln
dotnet build .\SVVideoDownloader.sln --configuration Release
dotnet test .\SVVideoDownloader.sln --configuration Release --no-build
```

Chạy ứng dụng:

```powershell
dotnet run --project .\src\SVVideoDownloader.App\SVVideoDownloader.App.csproj --configuration Release
```

Ứng dụng tìm công cụ ngoài trong thư mục `tools` cạnh executable của ứng dụng.
Kho mã không tự tạo hoặc điền thư mục này. Chỉ sử dụng artifact có nguồn gốc,
phiên bản, checksum/chữ ký và giấy phép đã được phê duyệt.

## Giao diện hiện có

- Dán và phân tích URL HTTPS công khai của YouTube, TikTok hoặc Facebook.
- Hiển thị ảnh thu nhỏ, tiêu đề, nguồn và thời lượng khi metadata có sẵn.
- Chọn preset chất lượng, thư mục lưu và xác nhận quyền trước mỗi tác vụ.
- Theo dõi hàng đợi, phần trăm, dung lượng đã tải, tốc độ và thời gian còn lại.
- Hủy, thử lại, xóa mục hoàn tất, mở tệp và mở thư mục.
- Xác nhận bằng tiếng Việt khi đóng cửa sổ trong lúc còn tác vụ hoạt động.

Nhận diện host chỉ xác nhận hình dạng liên kết và nền tảng được hỗ trợ; không bảo
đảm nội dung tồn tại, công khai, tải được hoặc người dùng có quyền tải. MVP luôn
xử lý URL playlist như một video trừ khi phạm vi được thay đổi rõ ràng.

## Cấu trúc

- `src/SVVideoDownloader.App`: WPF, MVVM và composition root.
- `src/SVVideoDownloader.Core`: mô hình và quy tắc nghiệp vụ thuần .NET.
- `src/SVVideoDownloader.Infrastructure`: biên tích hợp dành cho filesystem, process và công cụ ngoài trong các giai đoạn sau.
- `tests`: kiểm thử xUnit cho Core, Infrastructure và ViewModel của App.
- `docs`: đặc tả sản phẩm, kiến trúc và danh sách công việc.

Xem [đặc tả sản phẩm](docs/PRODUCT_SPEC.md), [kiến trúc](docs/ARCHITECTURE.md) và [công việc](docs/TASKS.md) trước khi mở rộng chức năng.

## Phụ thuộc bên thứ ba

`yt-dlp`, FFmpeg và ffprobe chạy dưới dạng executable ngoài tiến trình nhưng chưa
được tải hoặc phân phối cùng ứng dụng. `Microsoft.Extensions.DependencyInjection`
được dùng cho composition root. Giấy phép của binary thực tế phải được rà soát
trước khi chọn nguồn và hình thức phân phối. Chi tiết nằm trong
[tài liệu kiến trúc](docs/ARCHITECTURE.md#phụ-thuộc-bên-thứ-ba-và-giấy-phép).
