# Danh sách công việc

Ngày cập nhật: 2026-07-30.

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
- [x] Lưu settings/history JSON dưới LocalApplicationData bằng write tạm + thay thế.
- [x] Nhớ thư mục tải và preset chất lượng; flush write trước khi đóng cửa sổ.
- [x] Lưu tối đa 500 mục lịch sử hoàn tất; clear không thao tác media.
- [x] Thêm diagnostic log 1 MiB × 5 với redaction cookie/secret/token/URL.
- [x] Thêm màn hình trạng thái và phiên bản yt-dlp/FFmpeg/ffprobe.
- [x] Thêm updater yt-dlp thủ công dùng SHA-256, tệp tạm, replace và rollback.
- [x] Thêm updater thủ công cho gói FFmpeg/ffprobe Release Essentials x64: xác
      nhận nguồn/GPLv3, SHA-256, giải nén chọn lọc và rollback theo cặp.
- [x] Chặn updater khi metadata/download hoạt động và chặn download khi update.
- [x] Thêm profile publish self-contained single-file win-x64.
- [x] Đánh phiên bản sản phẩm đầu tiên `1.0.0` trong metadata assembly và changelog.
- [x] Thêm MSI per-machine x64 với shortcut, uninstall, MajorUpgrade và upgrade
      identity ổn định cho các phiên bản tương lai.
- [x] Thêm script build installer cùng tài liệu cài đặt/phát hành/nâng cấp.
- [x] Thêm tài liệu thiết lập và khắc phục sự cố cho sử dụng riêng.
- [x] Nâng cấp giao diện WPF với hệ thống thiết kế sáng/tối, control template thống nhất
      và lựa chọn giao diện được nhớ sau khi khởi động lại.
- [x] Tách lựa chọn Video/MP3 khỏi chất lượng video; nhớ lựa chọn và kiểm thử đối
      số trích xuất/chuyển đổi MP3 tại ranh giới process.
- [x] Mã hóa MP3 bằng mức chất lượng VBR cao nhất của yt-dlp/FFmpeg và kiểm thử
      `--audio-quality 0` không được áp dụng cho tải Video.
- [x] Thêm hai chế độ Video: MP4 tương thích ưu tiên H.264/AAC và chuyển đổi khi
      cần; chất lượng gốc giữ luồng tốt nhất không mã hóa lại. Nhớ lựa chọn, di
      trú cài đặt cũ và kiểm thử đối số, hủy, timeout, ánh xạ lỗi.
- [x] Xếp dọc các thẻ quản lý yt-dlp/FFmpeg và tắt cuộn ngang trên trang Công cụ
      và cài đặt.
- [x] Sửa thư mục và kiểu shortcut MSI theo máy để vượt qua ICE38/ICE43/ICE57.
- [x] Đánh phiên bản 1.1.0, tạo publish self-contained và MSI x64; xác minh
      ProductVersion, UpgradeCode, bảng File, SHA-256 và chạy ICE thành công.
- [x] Đối chiếu MSI 1.0.0/1.1.0 và registration đang cài: giữ UpgradeCode,
      install scope, feature/component GUID; dùng ProductCode mới và
      `RemoveExistingProducts` trước `InstallFiles` để hỗ trợ nâng cấp.
- [x] Chuyển gói 1.3.0 sang Inno Setup 7.0.2 x64 với wizard hiện đại sáng/tối,
      artwork thương hiệu, bản dịch tiếng Việt, shortcut tùy chọn, downgrade
      guard và di trú khỏi MSI 1.0.0/1.1.0.
- [x] Đánh phiên bản 1.3.0 và tạo installer EXE kèm tệp SHA-256.

## P0 — quyết định trước khi triển khai tải

- [ ] Duyệt nội dung xác nhận quyền của người dùng và luồng từ chối.
- [x] Chọn updater thủ công có chấp thuận cho yt-dlp và gói FFmpeg/ffprobe.
- [x] Chọn `ffmpeg-release-essentials.zip` x64 mới nhất từ gyan.dev, SHA-256
      sidecar và GPLv3 cho private-use; chưa chấp thuận bundle/phân phối lại.
- [ ] Xác minh chữ ký/attestation độc lập và lập third-party notices trước phát hành rộng.
- [ ] Quyết định có hỗ trợ `yt-dlp-ejs`/JavaScript runtime hay không.
- [x] Chọn installer Inno Setup per-machine x64 làm phương thức đóng gói
      private-use từ 1.3.0; giữ logic di trú cho MSI cũ.
- [ ] Xác định phiên bản Windows tối thiểu và chứng thư/quy trình ký mã.
- [ ] Hoàn thành threat model cho URL, đường dẫn, process, log và tệp tạm.
- [x] Chính sách private-use: settings/history/log chỉ LocalApplicationData, không telemetry.

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

- [x] Thiết kế luồng dán URL, chọn chất lượng/thư mục và dùng hành động tải làm xác
      nhận quyền cho từng tác vụ, không cần ô chọn riêng.
- [x] Thêm validation, tiến độ, hủy, thử lại và thông báo lỗi bằng tiếng Việt.
- [x] Thêm nhãn accessibility, access key, thứ tự bàn phím, cuộn và bố cục co giãn
      cơ bản cho mức 125%/150%.
- [x] Không đưa stderr, URL, cookie hoặc secret vào thông báo giao diện.
- [x] Thêm bảng màu sáng/tối, nút chuyển giao diện bằng tiếng Việt và kiểm thử ViewModel/XAML.
- [x] Thêm biểu tượng ứng dụng đa độ phân giải và dùng nhận diện mới trong phần đầu giao diện.
- [x] Sửa binding tiến độ hàng đợi thành một chiều để mục tải đầu tiên không làm WPF dừng ứng dụng.
- [ ] Kiểm thử thủ công với screen reader, high contrast và DPI 125%/150% trên
      các phiên bản Windows tối thiểu sau khi quyết định phạm vi hỗ trợ.
- [ ] Quyết định chính sách xử lý tệp trùng tên và ghi đè trước khi phát hành.

## P2 — phát hành và vận hành

- [ ] Thiết lập CI Windows x64 cho restore, build và test.
- [ ] Tạo kiểm thử tích hợp có fixture hợp pháp do dự án sở hữu.
- [x] Thiết kế cập nhật/rollback yt-dlp và gói FFmpeg/ffprobe; không cập nhật ngầm.
- [ ] Tạo SBOM, third-party notices và quy trình rà soát security advisory.
- [x] Xác minh bảng File của MSI 1.0.0 chỉ chứa executable ứng dụng self-contained;
      không có binary công cụ ngoài hoặc dữ liệu người dùng.
- [x] Xác minh bảng File của MSI 1.1.0 chỉ chứa executable ứng dụng self-contained;
      giữ UpgradeCode và vượt qua kiểm tra ICE.
- [x] Thay WiX v3.11 bằng Inno Setup 7.0.2 cho dòng installer 1.3.x.
- [ ] Cài mới/nâng cấp từ MSI 1.0.0/1.1.0, chặn hạ cấp và gỡ installer 1.3.0
      trên Windows sạch.
- [ ] Ký Authenticode executable và installer EXE, sau đó kiểm tra Defender/SmartScreen.
- [ ] Hoàn tất quyết định giấy phép Inno Setup nếu chuyển sang phát hành thương mại.
- [ ] Bổ sung xác minh chữ ký GPG/release attestation cho checksum nếu phát hành rộng.
- [ ] Smoke test gói self-contained trên Windows sạch, Defender/SmartScreen và DPI.

## Điều kiện chặn hiện tại

- Chưa có quyết định phân phối/giấy phép cuối cùng cho FFmpeg và release rộng.
- Nội dung xác nhận quyền hiện là bản MVP; chưa được duyệt pháp lý/sản phẩm.
- Chưa quyết định dependency JavaScript cần cho hỗ trợ YouTube đầy đủ.

App private-use dùng `%LOCALAPPDATA%\SVVideoDownloader\tools`; không bundle binary.
Updater yt-dlp và FFmpeg đều là thao tác thủ công và không nhập cookie. Chưa coi bản publish là
gói phát hành rộng cho đến khi hoàn tất giấy phép, signature/attestation, ký mã và
smoke test Windows sạch.
