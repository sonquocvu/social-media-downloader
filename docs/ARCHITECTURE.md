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
- Chứa mô hình nguồn video, quy tắc nhận diện nền tảng và các hợp đồng nghiệp vụ trong tương lai.
- Không tham chiếu WPF, `System.Diagnostics.Process`, filesystem, mạng hoặc tên/cú pháp của `yt-dlp`.
- Hiện có `VideoSource` và `SupportedPlatform`.

### `SVVideoDownloader.Infrastructure`

- Target `net10.0` và chỉ phụ thuộc Core.
- Sẽ sở hữu việc tìm executable, chạy process, đọc/ghi filesystem, parse JSON từ `yt-dlp`, kiểm tra phiên bản và ánh xạ lỗi.
- Hiện chỉ khai báo tên mặc định của `yt-dlp.exe`, `ffmpeg.exe` và `ffprobe.exe`; không chạy hay tải binary.

### `SVVideoDownloader.App`

- Target `net10.0-windows`, WPF, RID `win-x64`, `PlatformTarget` x64.
- Là composition root, chứa View, ViewModel và adapter dành riêng cho giao diện.
- Code-behind chỉ làm nhiệm vụ giao diện/vòng đời; trạng thái và lệnh nằm trong ViewModel.
- Hiện chỉ có màn hình khởi đầu và lệnh thông báo chưa triển khai.

### Kiểm thử

- `SVVideoDownloader.Core.Tests` kiểm tra nhận diện URL, các trường hợp URL không an toàn và tham chiếu assembly bị cấm.
- `SVVideoDownloader.Infrastructure.Tests` kiểm tra cấu hình nền tảng ban đầu.
- Cả hai dùng xUnit và target `net10.0`.

## 3. Luồng tích hợp dự kiến

```text
View -> ViewModel -> dịch vụ ứng dụng/Core
                    -> cổng Infrastructure
                    -> yt-dlp (JSON/progress)
                    -> ffprobe (metadata media)
                    -> ffmpeg (ghép/chuyển đổi)
```

Không đưa kiểu `Process`, đường dẫn tệp hoặc đối số `yt-dlp` vào Core. Infrastructure chuyển kết quả công cụ ngoài thành mô hình Core trước khi trả về App.

## 4. Nguyên tắc an toàn cho process ngoài

Phần này là yêu cầu cho giai đoạn triển khai, chưa phải chức năng hiện có.

- Gọi executable trực tiếp bằng `ProcessStartInfo.ArgumentList`; không qua `cmd.exe` hoặc PowerShell.
- Không nội suy URL/đường dẫn vào chuỗi lệnh shell.
- Chỉ cho phép tập tùy chọn `yt-dlp` được ứng dụng định nghĩa.
- Không bật cờ vượt DRM và không tự đọc cookie trình duyệt/netrc.
- Xác thực đường dẫn executable, phiên bản, kiến trúc và checksum từ nguồn đã phê duyệt.
- Đọc `stdout`/`stderr` bất đồng bộ, giới hạn kích thước, hỗ trợ hủy/timeout và kết thúc cây process khi cần.
- Che URL query, token, cookie và đường dẫn nhạy cảm trong log.
- Dùng thư mục tạm riêng cho mỗi tác vụ; xác minh đường dẫn trước khi dọn dẹp.
- Không coi thành công của process là bằng chứng người dùng có quyền với nội dung.

## 5. Phụ thuộc bên thứ ba và giấy phép

Ngày rà soát ban đầu: 2026-07-17. Đây không phải tư vấn pháp lý. Giấy phép phải được xác minh lại theo đúng phiên bản và artifact trước khi phân phối.

| Phụ thuộc | Vai trò | Tình trạng | Cân nhắc giấy phép/phân phối |
|---|---|---|---|
| [.NET 10 / WPF](https://github.com/dotnet/runtime) | Nền tảng ứng dụng | Dùng để build/chạy | Kiểm tra thông báo giấy phép của runtime và hình thức self-contained/framework-dependent khi đóng gói. |
| [yt-dlp](https://github.com/yt-dlp/yt-dlp#license) | Trích xuất metadata và tải nguồn công khai | Chưa tải, chưa tích hợp | Mã nguồn chính dùng Unlicense, nhưng binary PyInstaller chính thức chứa thành phần khác và được phân phối theo GPLv3+; phải giữ thông báo/nguồn tương ứng nếu phân phối lại. Không suy ra rằng mọi artifact đều chỉ có Unlicense. |
| [FFmpeg/ffprobe](https://ffmpeg.org/legal.html) | Kiểm tra, ghép và chuyển đổi media | Chưa tải, chưa tích hợp | FFmpeg mặc định là LGPL 2.1+; cấu hình có thành phần GPL làm GPL áp dụng cho toàn bộ build. Phải lưu cấu hình build, nguồn, phiên bản và giấy phép của binary đã chọn. Codec có thể kéo theo cân nhắc bằng sáng chế tùy nơi phân phối. |
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
- Nền tảng có thể thay đổi URL, điều khoản hoặc biện pháp chống tự động hóa mà không báo trước.
- Hỗ trợ YouTube đầy đủ có thể làm tăng dependency và nghĩa vụ giấy phép do JavaScript runtime/`yt-dlp-ejs`.
- Binary FFmpeg khác nhau có tập codec và giấy phép khác nhau.
- Xử lý hủy process và tệp tạm sai có thể để lại process/tệp hoặc xóa nhầm dữ liệu.
- URL và metadata có thể chứa dữ liệu nhạy cảm; thiết kế log cần được threat-model trước.
