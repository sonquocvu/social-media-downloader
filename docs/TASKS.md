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
- [x] Thêm giao diện WPF/MVVM đầu tiên cho phân tích, tùy chọn tải và hàng đợi.
- [x] Nối App với Core/Infrastructure bằng dependency injection.
- [x] Thêm trạng thái rỗng/đang tải/thành công/đã hủy/lỗi và khóa lệnh không hợp lệ.
- [x] Thêm thao tác hủy, thử lại, xóa mục hoàn tất, mở tệp và mở thư mục.
- [x] Thêm kiểm thử ViewModel dùng dịch vụ giả, không dùng mạng hay binary thật.
- [x] Parse thumbnail và đường dẫn output có cấu trúc từ yt-dlp vào mô hình Core.

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
- [x] Đọc đường dẫn tệp cuối bằng output template có tiền tố ổn định và kiểm tra
      đường dẫn thuộc thư mục đích.
- [x] Tích hợp ffprobe và FFmpeg qua kiểm tra toolchain và `--ffmpeg-location`.
- [x] Không trả stderr; ánh xạ lỗi kỹ thuật sang category/component ổn định.
- [ ] Quản lý thư mục tạm theo từng tác vụ và dọn dẹp có kiểm tra đường dẫn.
- [x] Thêm kiểm thử với process runner giả; không phụ thuộc mạng hoặc dịch vụ yt-dlp thật.

## P1 — giao diện

- [x] Thiết kế luồng dán URL, xác nhận quyền, chọn chất lượng/thư mục và tải.
- [x] Thêm validation, tiến độ, hủy, thử lại và thông báo lỗi bằng tiếng Việt.
- [x] Thêm nhãn accessibility, access key, thứ tự bàn phím, cuộn và bố cục co giãn
      cơ bản cho mức 125%/150%.
- [x] Không đưa stderr, URL, cookie hoặc secret vào thông báo giao diện.
- [ ] Kiểm thử thủ công với screen reader, high contrast và DPI 125%/150% trên
      các phiên bản Windows tối thiểu sau khi quyết định phạm vi hỗ trợ.
- [ ] Quyết định chính sách xử lý tệp trùng tên và ghi đè trước khi phát hành.

## P2 — phát hành và vận hành

- [ ] Thiết lập CI Windows x64 cho restore, build và test.
- [ ] Tạo kiểm thử tích hợp có fixture hợp pháp do dự án sở hữu.
- [ ] Thiết kế cập nhật/rollback ứng dụng và công cụ ngoài.
- [ ] Tạo SBOM, third-party notices và quy trình rà soát security advisory.
- [ ] Xác minh đóng gói sạch không chứa binary chưa phê duyệt.

## Điều kiện chặn hiện tại

- Chưa có quyết định pháp lý/phân phối cho binary ngoài.
- Nội dung xác nhận quyền hiện là bản MVP; chưa được duyệt pháp lý/sản phẩm.
- Chưa quyết định dependency JavaScript cần cho hỗ trợ YouTube đầy đủ.

Adapter Infrastructure đã được nối vào UI nhưng chưa bundle binary và chưa sẵn
sàng phân phối. Luồng chỉ chạy khi nhà phát triển tự cung cấp công cụ trong thư
mục `tools`; không phát hành cho người dùng cho đến khi các mục P0 liên quan được
xử lý.
