# Khắc phục sự cố

## Windows cảnh báo khi mở installer hoặc ứng dụng

Phiên bản 1.3.0 chưa được ký mã. Chỉ tiếp tục nếu installer đến từ kênh tin cậy và
SHA-256 khớp ghi chú phát hành. Không tắt Defender/SmartScreen trên toàn hệ thống.
Nếu antivirus cách ly hoặc khóa tệp, ghi lại tên sản phẩm bảo vệ và thông báo,
sau đó kiểm tra artifact trên máy build; không thêm ngoại lệ rộng cho Program
Files hoặc LocalApplicationData.

## Installer không cài đặt được

- Xác nhận hệ điều hành là Windows x64 và tài khoản có thể chấp nhận quyền quản trị.
- Nếu wizard báo còn MSI cũ không được nhận diện, gỡ “SV Video Downloader” trong
  Windows Settings rồi chạy lại installer. Dữ liệu LocalApplicationData và media
  không bị xóa.
- Nếu máy đã có phiên bản mới hơn, installer cũ phải từ chối hạ cấp bằng thông
  báo tiếng Việt.
- Thu thập log bằng
  `<installer.exe> /LOG="<đường-dẫn-log>"` trong phiên chẩn đoán do người dùng
  kiểm soát; rà soát log trước khi chia sẻ.
- Gỡ cài đặt hoặc lỗi installer không được xóa media hay dữ liệu LocalApplicationData.

## Không tìm thấy yt-dlp, FFmpeg hoặc ffprobe

1. Mở tab “Công cụ và cài đặt”.
2. So sánh đường dẫn hiển thị với `%LOCALAPPDATA%\SVVideoDownloader\tools`.
3. Kiểm tra đúng tên `yt-dlp.exe`, `ffmpeg.exe`, `ffprobe.exe`.
4. Bấm “Kiểm tra lại”.

Không thêm thư mục công cụ vào kho mã hoặc thư mục publish nguồn.

## Công cụ “không hợp lệ” hoặc “không thể truy cập”

- Xác nhận executable là Windows x64 và tải từ nguồn được phê duyệt.
- Kiểm tra Windows Defender/antivirus, thuộc tính “Unblock” và quyền NTFS.
- Chạy `yt-dlp.exe --version`, `ffmpeg.exe -version` và `ffprobe.exe -version`
  trực tiếp chỉ trong phiên chẩn đoán do người vận hành kiểm soát.
- Không thay đường dẫn bằng `cmd.exe`, PowerShell, script hoặc shortcut.

## Nút cập nhật công cụ bị vô hiệu hóa

Ứng dụng chặn cập nhật khi đang phân tích metadata, tải/ghép media, kiểm tra công
cụ hoặc khi một cập nhật khác đang chạy. Nút FFmpeg còn yêu cầu đánh dấu ô xác
nhận nguồn và GPLv3 trước mỗi lần cập nhật. Hủy hoặc đợi tác vụ kết thúc rồi thử lại.

## Checksum không khớp

Tệp tạm đã bị loại bỏ và executable hiện tại không bị thay đổi. Kiểm tra kết nối,
proxy bảo mật và thử lại sau. Không bỏ qua checksum hoặc copy tệp tạm để chạy.

## Bản mới không hoạt động

Nếu xác minh sau thay thế thất bại, ứng dụng cố khôi phục bản cũ. Nếu giao diện
báo rollback thất bại:

1. Đóng mọi tác vụ tải và ứng dụng khác đang dùng yt-dlp.
2. Sao lưu nhật ký chẩn đoán sau khi rà soát dữ liệu riêng tư.
3. Xóa/đổi tên tệp yt-dlp, FFmpeg hoặc ffprobe hỏng trong thư mục `tools`.
4. Cài lại bản đã xác minh theo quy trình thủ công.

Các tệp `.yt-dlp.*.backup`, `.ffmpeg.*.backup` hoặc `.ffprobe.*.backup` chỉ được
giữ lại khi rollback không thể hoàn tất. Không xóa backup trước khi xác định tệp
công cụ nào đã được khôi phục.

## Không lưu được cài đặt hoặc lịch sử

- Kiểm tra quyền ghi vào `%LOCALAPPDATA%\SVVideoDownloader`.
- Kiểm tra dung lượng đĩa và phần mềm bảo vệ thư mục.
- Nếu JSON hỏng, đổi tên tệp để ứng dụng tạo lại. Không đặt secret/cookie vào JSON.
- Xóa `history.json` thủ công chỉ xóa metadata lịch sử; không xóa media ở thư mục tải.

## Xem nhật ký chẩn đoán

Nhật ký nằm trong `%LOCALAPPDATA%\SVVideoDownloader\logs`. Mỗi tệp tối đa khoảng
1 MiB; giữ tối đa 5 tệp. Logger che các mẫu cookie, authorization, bearer token,
password, secret, API key, tùy chọn cookie và URL trước khi ghi.

Redaction theo mẫu không phải bảo đảm tuyệt đối. Luôn đọc lại nhật ký trước khi
chia sẻ và không gửi kèm `settings.json`, `history.json`, media hoặc executable.

## Video không tải được dù URL được nhận diện

Nhận diện host không bảo đảm video tồn tại, công khai, tải được hoặc người dùng có
quyền. Ứng dụng không vượt login, DRM, CAPTCHA, tường phí, giới hạn địa lý hoặc
video riêng tư; cũng không tự nhập cookie trình duyệt. Kiểm tra quyền và trạng thái
công khai của nội dung, sau đó kiểm tra phiên bản công cụ.
