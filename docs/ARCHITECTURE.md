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
- Sở hữu persistence JSON dưới `LocalApplicationData`, lịch sử giới hạn 500 mục,
  logger xoay vòng/redaction, kiểm tra trạng thái công cụ và các updater thủ công.
- Updater tải artifact/checksum qua HTTPS vào tệp tạm, kiểm tra SHA-256 và phiên
  bản executable, sau đó dùng thay thế nguyên tử cùng backup/rollback. FFmpeg và
  ffprobe được xử lý như một cặp từ cùng gói.
- Không có binary nào được tải hoặc đóng gói trong kho mã.

### `SVVideoDownloader.App`

- Target `net10.0-windows`, WPF, RID `win-x64`, `PlatformTarget` x64.
- Là composition root, chứa View, ViewModel và adapter dành riêng cho giao diện.
- Code-behind chỉ làm nhiệm vụ giao diện/vòng đời; trạng thái và lệnh nằm trong ViewModel.
- Dùng `Microsoft.Extensions.DependencyInjection` để nối ViewModel với cổng Core
  và implementation Infrastructure.
- Có màn hình phân tích, tùy chọn tải và hàng đợi. Thao tác filesystem/process từ
  ViewModel đi qua dịch vụ bất đồng bộ; hộp thoại xác nhận đóng cửa sổ vẫn là view concern.
- Giao diện tách lựa chọn loại tệp Video/MP3 khỏi chất lượng video. ViewModel ánh
  xạ lựa chọn MP3 sang preset Core `AudioMp3`; Infrastructure chuyển preset này
  thành `--extract-audio --audio-format mp3 --audio-quality 0`, không đưa cú pháp
  `yt-dlp` vào App/Core.
- `DownloadCoordinator` tạo cấu hình Infrastructure theo thư mục đích của từng tác
  vụ mà không đưa khái niệm filesystem vào Core.
- `EngineOperationGate` loại trừ cập nhật engine với metadata/download; updater
  không thể bắt đầu khi còn thao tác công cụ, và download mới không thể bắt đầu
  trong lúc updater giữ lease độc quyền.
- ViewModel nạp/lưu cài đặt, lịch sử, trạng thái công cụ và chờ các write đang chạy
  trước khi cửa sổ đóng.
- `WpfThemeService` thay bảng màu ResourceDictionary ở runtime; control template dùng
  `DynamicResource` nên cửa sổ chuyển sáng/tối tức thời mà không tạo lại ViewModel.
- Hai bảng màu và control template nằm trong `Themes`; lệnh đổi giao diện thuộc ViewModel,
  còn thao tác tài nguyên WPF được cô lập sau `IThemeService`.
- Trang Công cụ và cài đặt dùng một luồng cuộn dọc; các thẻ cập nhật yt-dlp và
  FFmpeg xếp dọc để không cần cuộn ngang khi DPI hoặc chiều rộng khả dụng thay đổi.
- Trên Windows 11 build 22000 trở lên, code-behind đồng bộ thanh tiêu đề native bằng
  `DWMWA_USE_IMMERSIVE_DARK_MODE`; đây là view concern và không đi vào Core/Infrastructure.

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

```text
%LOCALAPPDATA%\SVVideoDownloader
  settings.json       cấu hình UI không chứa credential
  history.json        metadata tác vụ hoàn tất
  logs\               log xoay vòng đã redaction
  tools\              yt-dlp.exe, ffmpeg.exe, ffprobe.exe do người dùng/updater quản lý
```

Xóa queue/history chỉ thay đổi collection hoặc `history.json`; không có đường gọi
`File.Delete` nào nhắm tới `DownloadHistoryEntry.FilePath` hay output media.

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
- Logger ứng dụng che cookie, header authorization, bearer token, password,
  secret/API key, tùy chọn cookie và URL trước khi ghi; mỗi log tối đa 1 MiB và
  giữ 5 tệp. Đây là defense-in-depth, không thay thế rà soát thủ công trước chia sẻ.
- Không coi thành công của process là bằng chứng người dùng có quyền với nội dung.

Các yêu cầu còn mở: xác minh kiến trúc/checksum/chữ ký từ nguồn được phê duyệt,
và quản lý thư mục tạm riêng theo tác vụ với quy tắc dọn dẹp an toàn.

## 5. Cập nhật công cụ thủ công

### yt-dlp

- Không có timer, background checker hay silent update. Chỉ nút người dùng mới gọi updater.
- Nguồn cố định là bản phát hành ổn định `yt-dlp/yt-dlp`: `yt-dlp.exe` và
  `SHA2-256SUMS` từ endpoint `releases/latest/download` qua HTTPS.
- Giới hạn tải: 100 MiB cho executable, 1 MiB cho checksum.
- Artifact phải khớp SHA-256 của dòng `yt-dlp.exe` và chạy được `--version` trước
  khi thay thế.
- Tệp tạm nằm cùng thư mục đích để rename/replace không đi qua volume khác.
- Nếu có bản cũ, `File.Replace` tạo backup và thay thế nguyên tử. Bản đã cài được
  kiểm tra lần nữa; nếu lỗi, backup được đưa trở lại bằng `File.Replace`.
- Khi rollback thất bại, backup được giữ để phục hồi thủ công; không xóa im lặng.
- Chưa xác minh chữ ký GPG `SHA2-256SUMS.sig`. Checksum và artifact cùng trust
  boundary GitHub/TLS nên đây vẫn là hạn chế trước phân phối rộng.

### FFmpeg và ffprobe

- Không có timer, background checker hay silent update. Người dùng phải đọc thông
  tin nguồn/GPLv3, đánh dấu xác nhận và bấm nút trước mỗi lần cập nhật.
- [FFmpeg chỉ phát hành source và liên kết gyan.dev cho Windows](https://ffmpeg.org/download.html).
  Updater private-use dùng `ffmpeg-release-essentials.zip` và sidecar `.sha256`
  qua HTTPS từ [gyan.dev](https://www.gyan.dev/ffmpeg/builds/).
- Gói được chọn là release mới nhất, Windows x64, static, GPLv3 và chứa cả
  `ffmpeg.exe` lẫn `ffprobe.exe`. Gói không được commit hoặc bundle vào publish.
- Giới hạn tải là 256 MiB cho ZIP và 64 KiB cho checksum. Chỉ đúng một entry
  `*/bin/ffmpeg.exe` và một entry `*/bin/ffprobe.exe` được trích xuất; mỗi tệp
  giải nén tối đa 300 MiB. Không extract cây thư mục hoặc tin tên đường dẫn từ ZIP.
- Cả hai candidate phải chạy `-version` thành công trước khi thay đổi tệp đích.
  Hai replacement dùng backup riêng; lỗi ở tệp thứ hai hoặc hậu kiểm sẽ rollback
  cả cặp. Backup chỉ được giữ khi rollback không thể hoàn tất.
- Sidecar checksum và ZIP do cùng host cung cấp, chưa có chữ ký/attestation độc
  lập; do đó HTTPS + SHA-256 chỉ phát hiện hỏng/tamper ngoài trust boundary đó.

## 6. Phụ thuộc bên thứ ba và giấy phép

Ngày rà soát ban đầu: 2026-07-17. Đây không phải tư vấn pháp lý. Giấy phép phải được xác minh lại theo đúng phiên bản và artifact trước khi phân phối.

| Phụ thuộc | Vai trò | Tình trạng | Cân nhắc giấy phép/phân phối |
|---|---|---|---|
| [.NET 10 / WPF](https://github.com/dotnet/runtime) | Nền tảng ứng dụng | Dùng để build/chạy | Kiểm tra thông báo giấy phép của runtime và hình thức self-contained/framework-dependent khi đóng gói. |
| [Microsoft.Extensions.DependencyInjection 10.0.8](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/10.0.8) | Composition root của App | Package runtime | Giấy phép MIT; khóa phiên bản trong project và đưa vào SBOM/third-party notices của gói phát hành. |
| [yt-dlp](https://github.com/yt-dlp/yt-dlp#license) | Trích xuất metadata và tải nguồn công khai | Không bundle; người dùng có thể kích hoạt cập nhật thủ công | Mã nguồn chính dùng Unlicense, nhưng binary PyInstaller chính thức chứa thành phần khác và được phân phối theo GPLv3+; phải giữ thông báo/nguồn tương ứng nếu phân phối lại. Không suy ra rằng mọi artifact đều chỉ có Unlicense. |
| [FFmpeg/ffprobe](https://ffmpeg.org/legal.html) | Kiểm tra, ghép và chuyển đổi media | Updater private-use tải thủ công gói Release Essentials x64 từ gyan.dev; không bundle | Gyan công bố mọi build của họ là GPLv3; gói thực tế gồm nhiều thư viện/codec. Cần giữ nguồn, phiên bản, checksum và cấu hình build nếu phân phối lại; codec có thể kéo theo cân nhắc bằng sáng chế tùy nơi phân phối. |
| [xUnit.net](https://xunit.net/) | Kiểm thử | Package phát triển | Apache License 2.0. Không phân phối trong sản phẩm runtime. |
| `Microsoft.NET.Test.Sdk`, `coverlet.collector`, runner xUnit | Chạy/đo kiểm thử | Package phát triển | Khóa phiên bản trong project; xác minh giấy phép và security advisory khi nâng cấp. Không phân phối trong sản phẩm runtime. |
| [WiX Toolset v3.11](https://github.com/wixtoolset/wix3) | Biên dịch gói MSI x64 | Chỉ dùng trên máy build; không nhúng vào MSI | Microsoft Reciprocal License. WiX v3 đã hết hỗ trợ cộng đồng và kho nguồn đã lưu trữ; cần lập kế hoạch chuyển sang phiên bản còn được hỗ trợ, giữ nguyên UpgradeCode và kiểm thử nâng cấp. |

README chính thức của yt-dlp hiện cũng nêu JavaScript runtime/engine và `yt-dlp-ejs` là cần thiết cho hỗ trợ YouTube đầy đủ. Chưa dependency nào trong nhóm này được chấp thuận hoặc tải; đây là quyết định kiến trúc và giấy phép còn mở.

Quyết định private-use cho FFmpeg ghi nhận artifact URL ổn định trỏ đến release
mới nhất, SHA-256 sidecar, x64/static/GPLv3 và không phân phối lại. Trước khi
bundle hoặc phát hành rộng vẫn cần snapshot phiên bản cố định, checksum/chữ ký,
third-party notices, cấu hình build, source tương ứng và rà soát nghĩa vụ GPLv3.

## 7. Quản lý cấu hình và dữ liệu

- Không hard-code đường dẫn tuyệt đối trên máy phát triển.
- Root dữ liệu lấy từ `Environment.SpecialFolder.LocalApplicationData`.
- `settings.json` chỉ lưu thư mục output tuyệt đối, enum chất lượng mặc định và lựa chọn
  giao diện sáng/tối; tệp cũ thiếu trường giao diện mặc định được đọc như giao diện sáng.
- `history.json` chỉ lưu tác vụ hoàn tất; không lưu URL nguồn, cookie hoặc stderr.
- Save JSON dùng tệp tạm duy nhất và move-overwrite trong cùng thư mục.
- Không lưu credential trong tệp cấu hình thường.
- Mặc định không có telemetry.
- Secrets phục vụ kiểm thử tích hợp, nếu có, phải nằm ngoài kho mã.

## 8. Publish

Profile `win-x64.pubxml` tạo bản `Release`, self-contained, single-file, không trim
WPF và extract native library khi cần. Output mặc định là
`artifacts/publish/win-x64`. Profile không copy thư mục `%LOCALAPPDATA%`, tools,
log, history, settings hay media.

## 9. Đóng gói MSI và phiên bản

`Directory.Build.props` là nguồn phiên bản duy nhất cho assembly và MSI; dòng
phát hành đầu tiên là `1.0.0`. `installer/Product.wxs` tạo gói per-machine x64
vào Program Files, shortcut Start Menu/Desktop và mục gỡ cài đặt. MSI chỉ có
single-file executable từ publish; công cụ ngoài và LocalApplicationData nằm
ngoài quyền sở hữu của Windows Installer.

`UpgradeCode` và GUID component được giữ ổn định giữa các phiên bản. ProductCode
và PackageCode được tạo lại ở mỗi build. `MajorUpgrade` thay bản cũ khi tăng một
trong ba phần `MAJOR.MINOR.PATCH` và chặn downgrade. Không phát hành hai MSI khác
nhau với cùng ProductVersion vì Windows Installer chỉ so sánh ba phần này.
Shortcut theo máy dùng `CommonProgramsFolder` và `CommonDesktopFolder`, đồng
thời được quảng bá qua Windows Installer để không trộn dữ liệu theo người dùng
với component có KeyPath theo máy.

Build hiện dùng WiX v3.11 cài ngoài repository. Script mặc định chạy ICE; tùy
chọn `-SkipMsiValidation` chỉ dành cho môi trường hạn chế và artifact phải được
kiểm tra lại trên Windows Installer thực trước khi phát hành. Executable phải
được ký trước khi đóng gói, sau đó ký MSI; phiên bản 1.1.0 hiện chưa ký.

## 10. Quyết định kiến trúc đã ghi nhận

- ADR tạm thời 001: dùng WPF + MVVM trên .NET 10, Windows x64.
- ADR tạm thời 002: Core thuần .NET, không phụ thuộc hạ tầng.
- ADR tạm thời 003: công cụ media chạy ngoài tiến trình, chưa bundle.
- ADR tạm thời 004: giao diện hoàn toàn bằng tiếng Việt; định danh mã nguồn có thể bằng tiếng Anh.
- ADR tạm thời 005: dữ liệu riêng nằm trong LocalApplicationData; không telemetry.
- ADR tạm thời 006: yt-dlp và gói FFmpeg/ffprobe chỉ được cập nhật khi người dùng
  yêu cầu, có checksum và rollback; FFmpeg cần xác nhận nguồn/GPLv3 trước mỗi lần.
- ADR tạm thời 007: dùng bảng màu WPF nội bộ và `DynamicResource`, không thêm UI framework
  bên thứ ba; lựa chọn sáng/tối được nhớ trong LocalApplicationData.
- ADR tạm thời 008: private-use dùng MSI per-machine x64 với UpgradeCode ổn định;
  gỡ/nâng cấp ứng dụng không sở hữu hoặc xóa LocalApplicationData và media.

Các ADR trên cần tách thành tài liệu riêng nếu phạm vi hoặc nhóm phát triển mở rộng.

## 11. Rủi ro kiến trúc còn lại

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
- Kiểm tra `--version`/`-version` chỉ xác nhận hình dạng/danh tính cơ bản, không
  chứng minh nguồn gốc hay an toàn của executable.
- Hai updater kiểm tra SHA-256 nhưng chưa kiểm tra chữ ký GPG hoặc release
  attestation; checksum và artifact FFmpeg hiện nằm cùng trust boundary gyan.dev/TLS.
- Logger dùng redaction theo mẫu; chuỗi secret có định dạng mới vẫn có thể lọt qua.
- Self-contained single-file và updater cần smoke test trên Windows sạch, Defender,
  SmartScreen và thư mục có chính sách bảo vệ thực tế.
- MSI 1.1.0 và executable chưa ký; ICE đã chạy thành công, nhưng chưa kiểm tra cài
  mới/nâng cấp/hạ cấp/gỡ cài đặt trên máy Windows sạch.
- WiX v3.11 đã hết hỗ trợ; việc chuyển phiên bản công cụ có thể làm thay đổi output
  MSI và phải được kiểm thử mà không phá vỡ upgrade identity.
