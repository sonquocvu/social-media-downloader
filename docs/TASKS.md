# Danh sách công việc

Ngày cập nhật: 2026-07-17.

## Hoàn thành trong khung ban đầu

- [x] Tạo solution và ba project `App`, `Core`, `Infrastructure`.
- [x] Tạo hai project kiểm thử xUnit.
- [x] Target .NET 10; WPF app target Windows x64.
- [x] Bật nullable và coi cảnh báo là lỗi qua `Directory.Build.props`.
- [x] Thiết lập chiều project reference để Core độc lập.
- [x] Tạo cửa sổ WPF/MVVM tối thiểu với toàn bộ nội dung hiển thị bằng tiếng Việt.
- [x] Thêm mô hình nhận diện URL HTTPS cho ba nền tảng và kiểm thử ban đầu.
- [x] Ghi nhận tên executable ngoài mà không tải hoặc chạy binary.
- [x] Ghi phạm vi an toàn, giả định, quyết định mở và cân nhắc giấy phép.
- [x] Hoàn thiện domain Core cho nguồn video, metadata, định dạng, tùy chọn,
      yêu cầu tải, tác vụ, tiến độ và trạng thái.
- [x] Thêm lỗi validation có cấu trúc để tầng giao diện ánh xạ sang tiếng Việt.
- [x] Thêm bộ làm sạch tên tệp Windows thuần chuỗi, không gọi filesystem.
- [x] Thêm kiểm thử URL, tên tệp, preset chất lượng và ma trận chuyển trạng thái.
- [x] Thêm hợp đồng Core cho phân tích metadata và tải với lỗi runtime có cấu trúc.
- [x] Thêm adapter yt-dlp/FFmpeg an toàn và kiểm thử hoàn toàn ngoại tuyến.

## P0 — quyết định trước khi triển khai tải

- [ ] Duyệt nội dung xác nhận quyền của người dùng và luồng từ chối.
- [ ] Chọn cách cung cấp `yt-dlp`, FFmpeg/ffprobe: tự cài, cài riêng hoặc tải có chấp thuận.
- [ ] Chọn artifact/phiên bản, xác minh giấy phép, checksum/chữ ký và lập third-party notices.
- [ ] Quyết định có hỗ trợ `yt-dlp-ejs`/JavaScript runtime hay không.
- [ ] Xác định phiên bản Windows tối thiểu và phương thức đóng gói/ký mã.
- [ ] Hoàn thành threat model cho URL, đường dẫn, process, log và tệp tạm.
- [ ] Quyết định chính sách dữ liệu: lịch sử, log, telemetry và báo lỗi.

## P1 — lõi nghiệp vụ

- [x] Định nghĩa yêu cầu tải, metadata, định dạng, tiến độ, trạng thái và mã lỗi không phụ thuộc hạ tầng.
- [x] Định nghĩa hợp đồng truy vấn metadata và thực thi tải.
- [x] Bổ sung quy tắc xác nhận quyền khi tạo yêu cầu tải.
- [ ] Bổ sung kiểm thử URL chuyển hướng, IDN, URL rút gọn và dữ liệu biên.

## P1 — hạ tầng

- [x] Tìm executable từ đường dẫn cấu hình và xác minh danh tính cơ bản bằng `--version`/`-version`.
- [x] Tạo process runner an toàn bằng `ArgumentList`, timeout, hủy cây process và giới hạn output.
- [x] Đọc metadata JSON/progress JSON có cấu trúc từ `yt-dlp`.
- [x] Tích hợp ffprobe và FFmpeg qua kiểm tra toolchain và `--ffmpeg-location`.
- [x] Không trả stderr; ánh xạ lỗi kỹ thuật sang category/component ổn định.
- [ ] Quản lý thư mục tạm theo từng tác vụ và dọn dẹp có kiểm tra đường dẫn.
- [x] Thêm kiểm thử với process runner giả; không phụ thuộc mạng hoặc dịch vụ yt-dlp thật.

## P1 — giao diện

- [ ] Thiết kế luồng dán URL, xác nhận quyền, chọn định dạng/thư mục và tải.
- [ ] Thêm validation, tiến độ, hủy và thông báo lỗi bằng tiếng Việt.
- [ ] Thêm accessibility, keyboard navigation, DPI và theme.
- [ ] Đảm bảo không hiển thị dữ liệu nhạy cảm ngoài ý muốn.

## P2 — phát hành và vận hành

- [ ] Thiết lập CI Windows x64 cho restore, build và test.
- [ ] Tạo kiểm thử tích hợp có fixture hợp pháp do dự án sở hữu.
- [ ] Thiết kế cập nhật/rollback ứng dụng và công cụ ngoài.
- [ ] Tạo SBOM, third-party notices và quy trình rà soát security advisory.
- [ ] Xác minh đóng gói sạch không chứa binary chưa phê duyệt.

## Điều kiện chặn hiện tại

- Chưa có quyết định pháp lý/phân phối cho binary ngoài.
- Chưa có thiết kế xác nhận quyền của người dùng.
- Chưa quyết định dependency JavaScript cần cho hỗ trợ YouTube đầy đủ.

Adapter Infrastructure đã được triển khai nhưng chưa được nối vào UI, chưa bundle
binary và chưa sẵn sàng phân phối. Không bật luồng end-to-end cho người dùng cho
đến khi các mục P0 liên quan được xử lý.
