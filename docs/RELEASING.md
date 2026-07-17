# Quy trình phát hành

## Phiên bản

SV Video Downloader dùng phiên bản ngữ nghĩa `MAJOR.MINOR.PATCH`. Phiên bản nguồn duy nhất nằm
trong `VersionPrefix` của `Directory.Build.props`; phiên bản đầu tiên là `1.0.0`.

- Tăng `PATCH` cho sửa lỗi tương thích ngược.
- Tăng `MINOR` cho chức năng mới tương thích ngược.
- Tăng `MAJOR` khi thay đổi không tương thích.
- Đồng bộ `AssemblyVersion` và `FileVersion` thành bốn phần khi chuẩn bị phát hành.
- Thêm mục mới vào `CHANGELOG.md` cùng ngày phát hành.

Windows Installer chỉ so sánh ba phần đầu của ProductVersion. Mỗi bản phát hành MSI phải tăng
ít nhất một trong ba phần này; không phát hành hai gói khác nhau cùng một ProductVersion.

## Danh tính nâng cấp MSI

`installer/Product.wxs` giữ một `UpgradeCode` cố định cho toàn bộ dòng sản phẩm. `Product Id="*"`
và `Package Id="*"` tạo danh tính mới cho từng lần build. Không thay `UpgradeCode` khi phát hành
phiên bản mới. GUID component cũng phải ổn định khi executable và vị trí cài đặt không thay đổi.

`MajorUpgrade` tự động gỡ phiên bản cũ trong giao dịch nâng cấp và từ chối hạ cấp. Gói cài theo
máy vào `%ProgramFiles%\SVVideoDownloader`, tạo shortcut ở Start Menu và Desktop, đồng thời hỗ
trợ gỡ cài đặt từ Windows Settings. Dữ liệu tại `%LOCALAPPDATA%\SVVideoDownloader` không thuộc
MSI nên nâng cấp hoặc gỡ cài đặt không xóa thiết lập, lịch sử, log, công cụ ngoài hay media.

## Tạo bản phát hành

Yêu cầu: .NET SDK theo `global.json` và WiX Toolset. Quy trình hiện tại dùng WiX Toolset v3.11
đã cài trên máy build.

```powershell
dotnet build .\SVVideoDownloader.sln --configuration Release
dotnet test .\SVVideoDownloader.sln --configuration Release --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\installer\build-installer.ps1 -NoRestore
```

`-NoRestore` dùng kết quả restore từ lệnh build trước đó và tránh truy cập NuGet lần nữa. Có thể bỏ
tham số này khi cần script tự restore trong môi trường có mạng. `ExecutionPolicy Bypass` chỉ áp
dụng cho tiến trình build hiện tại và không thay đổi chính sách hệ thống. Script cuối tự chạy
publish self-contained win-x64, biên dịch WiX và tạo:

```text
artifacts\installer\SVVideoDownloader-<version>-win-x64.msi
```

Mặc định WiX chạy bộ kiểm tra ICE của Windows Installer. Chỉ dùng `-SkipMsiValidation` trong môi
trường build hạn chế không truy cập được Windows Installer service; artifact đó phải được kiểm tra
ICE lại và cài thử trên Windows trước khi phát hành.

MSI chỉ chứa `SVVideoDownloader.App.exe`. Không thêm yt-dlp, FFmpeg, ffprobe, media, settings,
history, log, cookie, secret hoặc dữ liệu LocalApplicationData vào gói.

## Danh sách kiểm tra phát hành

1. Cập nhật `VersionPrefix`, `AssemblyVersion`, `FileVersion` và `CHANGELOG.md`.
2. Chạy toàn bộ build/test Release và tạo lại publish/MSI từ worktree đã rà soát.
3. Kiểm tra ProductName, ProductVersion, UpgradeCode, nền tảng x64 và nội dung bảng File của MSI.
4. Ký Authenticode cho executable trước khi tạo MSI, sau đó ký chính MSI bằng chứng thư ký mã.
5. Ghi SHA-256 của MSI vào ghi chú phát hành và lưu artifact ở vị trí được kiểm soát.
6. Cài mới trên máy Windows x64 sạch không có .NET; kiểm tra shortcut, khởi động và gỡ cài đặt.
7. Cài phiên bản trước rồi nâng cấp; xác nhận chỉ còn một phiên bản và dữ liệu LocalApplicationData
   vẫn còn.
8. Thử chạy gói cũ trên máy đã có phiên bản mới; xác nhận thông báo từ chối hạ cấp bằng tiếng Việt.
9. Kiểm tra Defender/SmartScreen, antivirus khóa tệp, rollback updater và chữ ký mã.
10. Kiểm tra tải nội dung được phép từ ba nền tảng, persistence, xóa lịch sử, bàn phím, screen
    reader, high contrast và DPI 125%/150%.

Không công bố rộng rãi khi chưa hoàn thành rà soát giấy phép, third-party notices, ký mã và smoke
test Windows sạch.

## Công cụ đóng gói và giấy phép

WiX Toolset chỉ là công cụ build, không được nhúng vào MSI. Mã nguồn WiX được phát hành theo
Microsoft Reciprocal License. Máy hiện tại có WiX v3.11; nhánh WiX v3 đã hết hỗ trợ cộng đồng và
kho nguồn đã lưu trữ, vì vậy cần lập kế hoạch chuyển sang một phiên bản WiX còn được hỗ trợ trước
khi duy trì/phát hành dài hạn. Việc chuyển công cụ không được thay `UpgradeCode` của sản phẩm và
phải được kiểm thử nâng cấp bằng MSI thực tế.
