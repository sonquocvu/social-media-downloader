# Thiết lập riêng trên Windows

## 1. Tạo bản publish

Yêu cầu trên máy phát triển: Windows x64 và .NET SDK đúng phiên bản trong
`global.json`.

```powershell
dotnet build .\SVVideoDownloader.sln --configuration Release
dotnet test .\SVVideoDownloader.sln --configuration Release --no-build
dotnet publish .\src\SVVideoDownloader.App\SVVideoDownloader.App.csproj -p:PublishProfile=win-x64
```

Kết quả nằm tại `artifacts\publish\win-x64`. Đây là bản self-contained win-x64,
không yêu cầu cài .NET runtime trên máy sử dụng. Không copy công cụ media, cài
đặt cá nhân, nhật ký hoặc tệp tải về vào thư mục publish.

## 2. Chạy lần đầu

1. Copy toàn bộ nội dung publish vào một thư mục riêng mà người dùng có quyền đọc/chạy.
2. Chạy `SVVideoDownloader.App.exe`.
3. Mở tab “Công cụ và cài đặt”.
4. Kiểm tra ba đường dẫn công cụ hiển thị trên màn hình.

Ứng dụng tạo dữ liệu cá nhân tại:

```text
%LOCALAPPDATA%\SVVideoDownloader
├── settings.json
├── history.json
├── logs\
└── tools\
```

Không copy thư mục này vào Git hoặc gói phát hành.

## 3. Thiết lập yt-dlp

Cách thuận tiện cho sử dụng riêng:

1. Đảm bảo không có tác vụ đang phân tích hoặc tải.
2. Mở tab “Công cụ và cài đặt”.
3. Bấm “Cập nhật yt-dlp”.

Đây là thao tác thủ công. Ứng dụng tải `yt-dlp.exe` và `SHA2-256SUMS` từ bản phát
hành ổn định chính thức của `yt-dlp/yt-dlp`, kiểm tra SHA-256 và chạy
`--version` trên tệp tạm trước khi thay thế. Tệp hiện có được backup trong lúc
thay thế; nếu bản mới không chạy, ứng dụng thử rollback.

Ứng dụng không kiểm tra chữ ký GPG của `SHA2-256SUMS`; xem hạn chế trong tài liệu
kiến trúc. Nếu tổ chức yêu cầu chuỗi tin cậy mạnh hơn, tải và xác minh công cụ
ngoài ứng dụng theo quy trình nội bộ rồi đặt tên là `yt-dlp.exe` trong thư mục
`tools` hiển thị trên màn hình.

## 4. Thiết lập FFmpeg và ffprobe

Ứng dụng không tải hoặc cập nhật FFmpeg tự động.

1. Chọn bản Windows x64 từ nguồn đã được tổ chức phê duyệt.
2. Ghi lại phiên bản, URL nguồn, SHA-256/chữ ký, cấu hình build và giấy phép.
3. Đặt `ffmpeg.exe` và `ffprobe.exe` cùng trong `%LOCALAPPDATA%\SVVideoDownloader\tools`.
4. Mở tab “Công cụ và cài đặt” và bấm “Kiểm tra lại”.

Hai tệp phải cùng thư mục. Phiên bản FFmpeg có thể là LGPL hoặc GPL tùy cấu hình
build; phải kiểm tra trước khi phân phối lại.

## 5. Cài đặt được nhớ

- Thư mục lưu mặc định ban đầu là thư mục `Downloads` của người dùng.
- Mỗi thay đổi thư mục hoặc chất lượng mặc định được lưu bất đồng bộ vào
  `settings.json` bằng tệp tạm và thay thế nguyên tử.
- Lịch sử chỉ ghi tác vụ hoàn tất, tối đa 500 mục.
- Xóa lịch sử hoặc xóa mục hoàn tất khỏi hàng đợi không gọi thao tác xóa media.

## 6. Nguyên tắc vận hành

- Chỉ tải nội dung do bạn sở hữu hoặc được phép tải.
- Không thêm cookie, mật khẩu, token hoặc hồ sơ trình duyệt vào thư mục ứng dụng.
- Không đổi cấu hình yt-dlp để đọc cookie/netrc ngoài ứng dụng.
- Không đóng ứng dụng trong lúc cập nhật yt-dlp; giao diện sẽ chặn thao tác này.
- Rà soát mục “Công cụ và cài đặt” sau mỗi thay đổi executable.
