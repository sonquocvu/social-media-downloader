# Nhật ký thay đổi

Tài liệu này ghi lại các thay đổi đáng chú ý của SV Video Downloader. Dự án dùng phiên bản
ngữ nghĩa `MAJOR.MINOR.PATCH`; xem [quy trình phát hành](docs/RELEASING.md).

## [Chưa phát hành]

### Đã thêm

- Đầu ra MP4 cho mọi preset Video: ưu tiên H.264/AAC và dùng FFmpeg chuyển đổi
  khi tệp tải về chưa ở container MP4.
- Kiểm thử đối số process, hủy, timeout và lỗi công cụ cho pipeline MP4.

### Đã thay đổi

- Nhãn loại tệp Video trong giao diện được đổi thành “MP4 (video)”.

## [1.1.0] - 2026-07-27

Phiên bản bổ sung tải MP3 chất lượng cao và cải thiện bố cục trang Công cụ và
cài đặt.

### Đã thêm

- Lựa chọn loại tệp Video hoặc MP3 riêng biệt sau khi phân tích liên kết.
- Tải luồng âm thanh tốt nhất và mã hóa MP3 bằng mức chất lượng VBR cao nhất
  (`--audio-quality 0`).
- Ghi nhớ lựa chọn MP3 hoặc preset chất lượng Video trong cài đặt cục bộ.

### Đã thay đổi

- Chỉ hiển thị lựa chọn chất lượng khi tải Video.
- Xếp dọc các thẻ cập nhật yt-dlp và FFmpeg trên trang Công cụ và cài đặt.
- Tắt cuộn ngang trên trang Công cụ và cài đặt, đồng thời cho nội dung xác nhận
  giấy phép FFmpeg tự xuống dòng.
- Sửa shortcut MSI theo máy để dùng Start Menu/Desktop chung cho mọi người dùng
  và vượt qua kiểm tra ICE của Windows Installer.
- Bổ sung kiểm thử ViewModel, XAML và đối số process cho các luồng mới.

### Gói phát hành

- MSI: `SVVideoDownloader-1.1.0-win-x64.msi`
- SHA-256:
  `37398E6F9AF79B861C3D7D5A4D4772652838F4C2B31A55BB169DD8507D1A8272`
- Gói và executable chưa được ký mã.

## [1.0.0] - 2026-07-17

Phiên bản đầu tiên dành cho sử dụng riêng trên Windows x64.

### Đã có

- Giao diện WPF tiếng Việt theo MVVM, hỗ trợ bảng màu sáng/tối và điều hướng bàn phím.
- Phân tích URL công khai được hỗ trợ của YouTube, TikTok và Facebook bằng yt-dlp.
- Tải video/âm thanh bằng yt-dlp; FFmpeg/ffprobe chỉ được dùng để kiểm tra, ghép hoặc chuyển
  đổi media khi định dạng đã chọn yêu cầu.
- Hàng đợi, tiến độ, hủy, thử lại, mở tệp/thư mục và lịch sử tải.
- Lưu thiết lập dưới LocalApplicationData và nhật ký xoay vòng có che dữ liệu nhạy cảm.
- Kiểm tra trạng thái và cập nhật thủ công yt-dlp cùng gói FFmpeg/ffprobe có checksum và rollback.
- Bản publish self-contained win-x64 và gói cài đặt MSI theo máy.

### Giới hạn đã biết

- Gói MSI và executable chưa được ký mã; Windows Defender/SmartScreen có thể cảnh báo.
- yt-dlp, FFmpeg và ffprobe không được phân phối trong MSI; người dùng cài từ màn hình công cụ.
- Chưa hoàn thành smoke test trên máy Windows x64 sạch, accessibility, high contrast và DPI
  125%/150%.
- Hỗ trợ YouTube có thể cần JavaScript runtime/`yt-dlp-ejs` trong tương lai.
- Không hỗ trợ cookie trình duyệt, đăng nhập, DRM, CAPTCHA, tường phí, giới hạn địa lý hoặc video
  riêng tư.
