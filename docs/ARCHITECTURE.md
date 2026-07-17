# Kiến trúc SVVideoDownloader

## 1. Mục tiêu kiến trúc

Kiến trúc tách quy tắc nghiệp vụ khỏi WPF và chi tiết chạy executable. Điều này giúp kiểm thử Core độc lập, thay đổi công cụ ngoài có kiểm soát và giảm nguy cơ dữ liệu người dùng đi vào shell hoặc log.

## 2. Thành phần và chiều phụ thuộc

```text
SVVideoDownloader.App (WPF, MVVM, composition root)
             |                         |
             v                         v
SVVideoDownloader.Core <--- SVVideoDownloader.Infrastructure
             ^
             |
      kiểm thử Core/Infrastructure
```

### `SVVideoDownloader.Core`

- Target `net10.0`.
- Chứa mô hình nguồn video, metadata, định dạng, yêu cầu/tùy chọn tải, tiến độ,
  trạng thái và các hợp đồng nghiệp vụ.
- Không tham chiếu WPF, `System.Diagnostics.Process`, filesystem, mạng hoặc tên/cú pháp của `yt-dlp`.
- Cung cấp `IVideoMetadataProvider` và `IVideoDownloadService`; hai hợp đồng chỉ
  dùng mô hình Core, `CancellationToken` và `IProgress<DownloadProgress>`.
- Lỗi runtime được biểu diễn bằng `MediaErrorCategory` + `MediaComponent` để UI
  ánh xạ sang thông báo tiếng Việt mà không nhận stderr hoặc dữ liệu nhạy cảm.

### `SVVideoDownloader.Infrastructure`

- Target `net10.0` và chỉ phụ thuộc Core.
- Sở hữu cấu hình đường dẫn executable, process runner, kiểm tra danh tính/phiên
  bản, parse JSON từ `yt-dlp`, ánh xạ lỗi và tạo output template.
- `YtDlpMediaService` triển khai hai hợp đồng Core. Metadata dùng
  `--dump-single-json`; tiến độ dùng JSON từ `--progress-template`.
- Mọi lời gọi dùng `ProcessStartInfo.ArgumentList`, `UseShellExecute=false`,
  redirect đồng thời stdout/stderr và tuyệt đối không gọi `cmd.exe`, PowerShell
  hoặc `pwsh`.
- `--ignore-config` ngăn cấu hình yt-dlp ngoài ứng dụng tự thêm cookie, proxy,
  credential hoặc hành vi playlist. `--no-playlist` giữ phạm vi một video cho MVP.
- FFmpeg/ffprobe được kiểm tra bằng `-version`; thư mục chứa hai binary được
  truyền cho yt-dlp bằng `--ffmpeg-location`.
- Không có binary nào được tải hoặc đóng gói trong kho mã.

### `SVVideoDownloader.App`

- Target `net10.0-windows`, WPF, RID `win-x64`, `PlatformTarget` x64.
- Là composition root, chứa View, ViewModel và adapter dành riêng cho giao diện.
- Code-behind chỉ làm nhiệm vụ giao diện/vòng đời; trạng thái và lệnh nằm trong ViewModel.
- Dùng `Microsoft.Extensions.DependencyInjection` để nối ViewModel với cổng Core
  và implementation Infrastructure.
- Có màn hình phân tích, tùy chọn tải và hàng đợi. Thao tác filesystem/process từ
  ViewModel đi qua dịch vụ bất đồng bộ; hộp thoại xác nhận đóng cửa sổ vẫn là view concern.
- `DownloadCoordinator` tạo cấu hình Infrastructure theo thư mục đích của từng tác
  vụ mà không đưa khái niệm filesystem vào Core.

### Kiểm thử

- `SVVideoDownloader.Core.Tests` kiểm tra nhận diện URL, các trường hợp URL không an toàn và tham chiếu assembly bị cấm.
- `SVVideoDownloader.Infrastructure.Tests` dùng process runner giả, không dùng
  Internet hoặc executable thật; kiểm tra argument, JSON metadata/progress,
  timeout, cancellation, nhận diện công cụ và che dữ liệu nhạy cảm.
- `SVVideoDownloader.App.Tests` kiểm tra ViewModel phân tích, chọn thư mục, tải,
  tiến độ, hủy, thử lại, xóa và mở kết quả bằng dịch vụ giả.
- Các project dùng xUnit; kiểm thử App target `net10.0-windows`, các bộ còn lại
  target `net10.0`.

## 3. Luồng tích hợp

```text
View -> ViewModel -> dịch vụ ứng dụng/Core
                    -> cổng Infrastructure
                    -> yt-dlp (JSON/progress)
                    -> ffprobe (kiểm tra toolchain)
                    -> ffmpeg qua yt-dlp (ghép/chuyển đổi)
```

Không đưa kiểu `Process`, đường dẫn tệp hoặc đối số `yt-dlp` vào Core.
Infrastructure chuyển JSON công cụ ngoài thành mô hình Core trước khi trả về App.
App gọi các dịch vụ qua dependency injection. `yt-dlp` trả metadata JSON, progress
JSON và đường dẫn tệp cuối bằng template có tiền tố ổn định; đường dẫn cuối phải
nằm trong thư mục đích trước khi App cho phép mở tệp.

## 4. Nguyên tắc an toàn cho process ngoài

Các nguyên tắc sau đã được áp dụng tại process boundary:

- Gọi executable trực tiếp bằng `ProcessStartInfo.ArgumentList`; chặn `cmd.exe`,
  PowerShell, `pwsh` và các tên shell tương đương.
- Không nội suy URL/đường dẫn vào chuỗi lệnh shell.
- Chỉ cho phép tập tùy chọn `yt-dlp` được ứng dụng định nghĩa.
- Không bật cờ vượt DRM và không tự đọc cookie trình duyệt/netrc.
- Yêu cầu đường dẫn executable tuyệt đối và kiểm tra danh tính cơ bản qua
  `--version`/`-version` trước khi chạy tác vụ.
- Đọc `stdout`/`stderr` bất đồng bộ và đồng thời, vẫn drain sau khi đạt giới hạn
  capture để tránh deadlock.
- Hỗ trợ hủy/timeout; dùng `Kill(entireProcessTree: true)` và giới hạn thời gian
  chờ process/pipe kết thúc.
- Không ghi log trong adapter và không đưa stderr, URL, token, cookie hoặc đường
  dẫn vào `MediaOperationError`.
- Không coi thành công của process là bằng chứng người dùng có quyền với nội dung.

Các yêu cầu còn mở: xác minh kiến trúc/checksum/chữ ký từ nguồn được phê duyệt,
và quản lý thư mục tạm riêng theo tác vụ với quy tắc dọn dẹp an toàn.

## 5. Phụ thuộc bên thứ ba và giấy phép

Ngày rà soát ban đầu: 2026-07-17. Đây không phải tư vấn pháp lý. Giấy phép phải được xác minh lại theo đúng phiên bản và artifact trước khi phân phối.

| Phụ thuộc | Vai trò | Tình trạng | Cân nhắc giấy phép/phân phối |
|---|---|---|---|
| [.NET 10 / WPF](https://github.com/dotnet/runtime) | Nền tảng ứng dụng | Dùng để build/chạy | Kiểm tra thông báo giấy phép của runtime và hình thức self-contained/framework-dependent khi đóng gói. |
| [Microsoft.Extensions.DependencyInjection 10.0.8](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/10.0.8) | Composition root của App | Package runtime | Giấy phép MIT; khóa phiên bản trong project và đưa vào SBOM/third-party notices của gói phát hành. |
| [yt-dlp](https://github.com/yt-dlp/yt-dlp#license) | Trích xuất metadata và tải nguồn công khai | Adapter đã có; chưa tải/bundle binary | Mã nguồn chính dùng Unlicense, nhưng binary PyInstaller chính thức chứa thành phần khác và được phân phối theo GPLv3+; phải giữ thông báo/nguồn tương ứng nếu phân phối lại. Không suy ra rằng mọi artifact đều chỉ có Unlicense. |
| [FFmpeg/ffprobe](https://ffmpeg.org/legal.html) | Kiểm tra, ghép và chuyển đổi media | Probe/tích hợp qua yt-dlp đã có; chưa tải/bundle binary | FFmpeg mặc định là LGPL 2.1+; cấu hình có thành phần GPL làm GPL áp dụng cho toàn bộ build. Phải lưu cấu hình build, nguồn, phiên bản và giấy phép của binary đã chọn. Codec có thể kéo theo cân nhắc bằng sáng chế tùy nơi phân phối. |
| [xUnit.net](https://xunit.net/) | Kiểm thử | Package phát triển | Apache License 2.0. Không phân phối trong sản phẩm runtime. |
| `Microsoft.NET.Test.Sdk`, `coverlet.collector`, runner xUnit | Chạy/đo kiểm thử | Package phát triển | Khóa phiên bản trong project; xác minh giấy phép và security advisory khi nâng cấp. Không phân phối trong sản phẩm runtime. |

README chính thức của yt-dlp hiện cũng nêu JavaScript runtime/engine và `yt-dlp-ejs` là cần thiết cho hỗ trợ YouTube đầy đủ. Chưa dependency nào trong nhóm này được chấp thuận hoặc tải; đây là quyết định kiến trúc và giấy phép còn mở.

Trước khi thêm binary, cần lập hồ sơ gồm tên artifact, URL nguồn chính thức, phiên bản cố định, SHA-256/chữ ký, kiến trúc, giấy phép, third-party notices, cấu hình build và quyết định có phân phối lại hay yêu cầu người dùng tự cung cấp.

## 6. Quản lý cấu hình dự kiến

- Không hard-code đường dẫn tuyệt đối trên máy phát triển.
- Cấu hình người dùng chỉ lưu đường dẫn công cụ và thư mục đầu ra sau khi kiểm tra.
- Không lưu credential trong tệp cấu hình thường.
- Mặc định không có telemetry.
- Secrets phục vụ kiểm thử tích hợp, nếu có, phải nằm ngoài kho mã.

## 7. Quyết định kiến trúc đã ghi nhận

- ADR tạm thời 001: dùng WPF + MVVM trên .NET 10, Windows x64.
- ADR tạm thời 002: Core thuần .NET, không phụ thuộc hạ tầng.
- ADR tạm thời 003: công cụ media chạy ngoài tiến trình, chưa bundle.
- ADR tạm thời 004: giao diện hoàn toàn bằng tiếng Việt; định danh mã nguồn có thể bằng tiếng Anh.

Các ADR trên cần tách thành tài liệu riêng nếu phạm vi hoặc nhóm phát triển mở rộng.

## 8. Rủi ro kiến trúc còn lại

- Output/progress của `yt-dlp` thay đổi giữa các phiên bản; cần ưu tiên JSON/mẫu có version thay vì parse văn bản tự do.
- Adapter hiện dựa vào `%(progress)j` và các field JSON của `--dump-single-json`;
  cần khóa phiên bản tương thích và có fixture khi chọn binary phát hành.
- UI đã có accessibility/DPI cơ bản nhưng chưa được kiểm thử thủ công bằng screen
  reader, high contrast và ma trận Windows/DPI đã phê duyệt.
- Tên output hiện có thể trùng với tệp đã tồn tại; phải chốt chính sách tránh ghi
  đè/đổi tên trước khi phân phối.
- Nền tảng có thể thay đổi URL, điều khoản hoặc biện pháp chống tự động hóa mà không báo trước.
- Hỗ trợ YouTube đầy đủ có thể làm tăng dependency và nghĩa vụ giấy phép do JavaScript runtime/`yt-dlp-ejs`.
- Binary FFmpeg khác nhau có tập codec và giấy phép khác nhau.
- Xử lý hủy process và tệp tạm sai có thể để lại process/tệp hoặc xóa nhầm dữ liệu.
- URL và metadata có thể chứa dữ liệu nhạy cảm; thiết kế log cần được threat-model trước.
- Kiểm tra `--version` xác nhận hình dạng/danh tính cơ bản, không xác minh nguồn
  cung cấp hoặc tính toàn vẹn; checksum/chữ ký vẫn là điều kiện P0.
