# Nhật ký thay đổi

Tài liệu này ghi lại các thay đổi đáng chú ý của SV Video Downloader. Dự án dùng phiên bản
ngữ nghĩa `MAJOR.MINOR.PATCH`; xem [quy trình phát hành](docs/RELEASING.md).

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
