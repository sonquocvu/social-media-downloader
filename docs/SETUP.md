# Thiết lập riêng trên Windows

## Cài phiên bản 1.3.0

1. Lấy `SVVideoDownloader-1.3.0-win-x64-setup.exe` từ kênh tin cậy và kiểm tra SHA-256
   theo ghi chú phát hành.
2. Mở trình cài đặt và chấp nhận yêu cầu quyền quản trị. Ứng dụng được cài vào
   `%ProgramFiles%\SVVideoDownloader`.
3. Giữ hoặc bỏ lựa chọn tạo shortcut Desktop, sau đó hoàn tất wizard.
4. Mở “SV Video Downloader” từ Start Menu hoặc shortcut Desktop.
5. Mở tab “Công cụ và cài đặt”, sau đó cài/kiểm tra yt-dlp, FFmpeg và ffprobe.

Installer là self-contained win-x64 nên máy sử dụng không cần cài .NET runtime. Gói
không chứa yt-dlp, FFmpeg, ffprobe, media, settings, history, log hoặc secret.
Installer/executable 1.3.0 chưa được ký mã nên Defender/SmartScreen có thể cảnh báo;
không bỏ qua cảnh báo nếu nguồn hoặc checksum không đáng tin cậy.

Khi phát hiện MSI 1.0.0 hoặc 1.1.0 đã phát hành trước đây, installer 1.3.0 tự
gỡ gói đó trong lúc chuẩn bị cài. Nếu còn một MSI thử nghiệm không được nhận
diện, wizard dừng và yêu cầu gỡ bản đó trong Windows Settings để tránh hai mục
gỡ cài đặt cùng sở hữu một thư mục. Phiên bản mới hơn luôn chặn cài bản cũ.

Gỡ ứng dụng trong Windows Settings sẽ xóa executable và shortcut nhưng không xóa
`%LOCALAPPDATA%\SVVideoDownloader` hoặc media đã tải. Điều này giữ thiết lập,
lịch sử và công cụ ngoài cho lần nâng cấp/cài lại; xóa thủ công chỉ khi đã sao
lưu và thực sự không còn cần dữ liệu đó.

## 1. Tạo bản publish

Yêu cầu trên máy phát triển: Windows x64 và .NET SDK đúng phiên bản trong
`global.json`. Để tạo installer cần thêm Inno Setup 7.0.2 x64.

```powershell
dotnet build .\SVVideoDownloader.sln --configuration Release
dotnet test .\SVVideoDownloader.sln --configuration Release --no-build
dotnet publish .\src\SVVideoDownloader.App\SVVideoDownloader.App.csproj -p:PublishProfile=win-x64
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\installer\build-installer.ps1 -NoRestore
```

Kết quả nằm tại `artifacts\publish\win-x64`. Đây là bản self-contained win-x64,
không yêu cầu cài .NET runtime trên máy sử dụng. Không copy công cụ media, cài
đặt cá nhân, nhật ký hoặc tệp tải về vào thư mục publish.

Installer và tệp checksum được tạo trong `artifacts\installer`.

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

Cách thuận tiện cho sử dụng riêng:

1. Đảm bảo không có tác vụ đang phân tích, tải hoặc cập nhật công cụ khác.
2. Mở tab “Công cụ và cài đặt”.
3. Đọc nguồn/gói và thông tin GPLv3, sau đó đánh dấu ô xác nhận.
4. Bấm “Cài đặt / cập nhật FFmpeg”.

Ứng dụng tải `ffmpeg-release-essentials.zip` Windows x64 và tệp SHA-256 từ
[gyan.dev](https://www.gyan.dev/ffmpeg/builds/), nguồn bản dựng Windows được
[trang tải chính thức của FFmpeg](https://ffmpeg.org/download.html) liên kết.
Ứng dụng chỉ trích xuất `ffmpeg.exe` và `ffprobe.exe`, giới hạn kích thước, xác
minh cả hai bằng `-version`, rồi thay thế theo cặp. Nếu một tệp bị khóa hoặc hậu
kiểm thất bại, ứng dụng cố khôi phục cả hai bản cũ.

Gói Release Essentials của gyan.dev là bản dựng static x64 GPLv3. Updater không
bundle hay phân phối lại gói trong ứng dụng, không chạy ngầm và không kiểm tra
chữ ký độc lập ngoài SHA-256 do cùng nguồn công bố. Nếu chính sách tổ chức yêu
cầu nguồn khác, vẫn có thể đặt thủ công `ffmpeg.exe` và `ffprobe.exe` đã được
phê duyệt vào `%LOCALAPPDATA%\SVVideoDownloader\tools`, rồi bấm “Kiểm tra lại”.

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
- Không đóng ứng dụng trong lúc cập nhật yt-dlp hoặc FFmpeg; giao diện sẽ chặn thao tác này.
- Rà soát mục “Công cụ và cài đặt” sau mỗi thay đổi executable.
