# Quy trình phát hành

## Phiên bản

SV Video Downloader dùng phiên bản ngữ nghĩa `MAJOR.MINOR.PATCH`. Nguồn phiên
bản duy nhất là `VersionPrefix` trong `Directory.Build.props`.

- Tăng `PATCH` cho sửa lỗi tương thích ngược.
- Tăng `MINOR` cho chức năng mới tương thích ngược.
- Tăng `MAJOR` khi thay đổi không tương thích.
- Đồng bộ `AssemblyVersion` và `FileVersion` thành bốn phần khi phát hành.
- Thêm mục có ngày phát hành vào `CHANGELOG.md`.

Không phát hành hai installer có nội dung khác nhau với cùng một phiên bản.

## Danh tính nâng cấp

`installer/SVVideoDownloader.iss` giữ một `AppId` cố định cho dòng installer
Inno Setup từ phiên bản 1.3.0. Không thay `AppId` khi phát hành phiên bản mới.
Installer đọc `DisplayVersion` và từ chối hạ cấp nếu máy đã có phiên bản Inno
Setup mới hơn.

Phiên bản 1.0.0 và 1.1.0 dùng WiX/MSI. Installer 1.3.0 giữ `UpgradeCode` cũ và
hai ProductCode đã phát hành để:

1. chặn hạ cấp khi phát hiện MSI mới hơn phiên bản đang chạy;
2. gỡ yên lặng MSI 1.0.0/1.1.0 trước khi cài bằng Inno Setup;
3. dừng cài đặt nếu còn MSI thử nghiệm không được nhận diện, tránh hai cơ chế
   cùng sở hữu `%ProgramFiles%\SVVideoDownloader`.

Cài đặt theo máy vào `%ProgramFiles%\SVVideoDownloader`, tạo shortcut Start
Menu và shortcut Desktop theo lựa chọn, đồng thời đăng ký gỡ cài đặt trong
Windows Settings. Dữ liệu `%LOCALAPPDATA%\SVVideoDownloader` và media nằm ngoài
quyền sở hữu của installer nên nâng cấp/gỡ cài đặt không xóa chúng.

## Tạo bản phát hành

Yêu cầu:

- .NET SDK theo `global.json`;
- Inno Setup 7.0.2 x64 từ nguồn chính thức.

```powershell
dotnet build .\SVVideoDownloader.sln --configuration Release
dotnet test .\SVVideoDownloader.sln --configuration Release --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\installer\build-installer.ps1 -NoRestore
```

`-NoRestore` dùng kết quả restore từ bước build. Có thể bỏ tham số khi môi
trường có mạng và cần restore lại. `ExecutionPolicy Bypass` chỉ áp dụng cho
tiến trình build hiện tại.

Script tự đọc phiên bản, publish self-contained win-x64, biên dịch Inno Setup
và tạo:

```text
artifacts\installer\SVVideoDownloader-<version>-win-x64-setup.exe
artifacts\installer\SVVideoDownloader-<version>-win-x64-setup.exe.sha256
```

Có thể truyền `-InnoSetupCompilerPath` nếu `ISCC.exe` không nằm trong vị trí
Program Files hoặc LocalAppData chuẩn. `-SkipPublish` chỉ dành cho vòng lặp
phát triển khi publish hiện có chắc chắn khớp phiên bản.

Installer chỉ lấy `SVVideoDownloader.App.exe` từ publish. Không thêm yt-dlp,
FFmpeg, ffprobe, media, settings, history, log, cookie, secret hoặc dữ liệu
LocalApplicationData.

## Danh sách kiểm tra phát hành

1. Cập nhật `VersionPrefix`, `AssemblyVersion`, `FileVersion` và `CHANGELOG.md`.
2. Chạy toàn bộ build/test Release và tạo lại publish/installer từ worktree đã rà soát.
3. Kiểm tra ProductName, ProductVersion, AppId, kiến trúc x64 và nội dung installer.
4. Ký Authenticode cho executable trước khi đóng gói, sau đó ký installer EXE.
5. Kiểm tra SHA-256 và lưu artifact ở vị trí được kiểm soát.
6. Cài mới trên Windows x64 sạch; kiểm tra wizard tiếng Việt sáng/tối, shortcut,
   khởi động và gỡ cài đặt.
7. Nâng cấp từ MSI 1.0.0 và 1.1.0; xác nhận chỉ còn một mục gỡ cài đặt và dữ
   liệu LocalApplicationData vẫn còn.
8. Nâng cấp từ bản Inno Setup trước; xác nhận đường dẫn, lựa chọn shortcut và dữ
   liệu được giữ.
9. Chạy installer cũ trên máy có phiên bản mới; xác nhận thông báo từ chối hạ
   cấp bằng tiếng Việt.
10. Kiểm tra Defender/SmartScreen, antivirus khóa tệp và chữ ký mã.
11. Kiểm tra tải nội dung được phép, persistence, xóa lịch sử, bàn phím, screen
    reader, high contrast và DPI 125%/150%.

Không công bố rộng rãi khi chưa hoàn thành rà soát giấy phép, third-party
notices, ký mã và smoke test Windows sạch.

## Công cụ đóng gói và giấy phép

Quy trình đóng gói dùng Inno Setup 7.0.2 x64, tải từ trang phát hành chính thức.
Đây là công cụ build; compiler/IDE không được đưa vào sản phẩm. Installer tạo ra
có phần runtime Inno Setup cần thiết để cài/gỡ ứng dụng.

Inno Setup dùng Inno Setup License (SPDX `InnoSetup`). Nhà phát triển công cụ đề
nghị người dùng thương mại mua giấy phép, kể cả dùng nội bộ; giấy phép thương
mại không thêm tính năng. Bản private-use hiện có thể dùng để thử nghiệm, nhưng
phải hoàn tất quyết định mua giấy phép và rà soát điều khoản theo mô hình sử
dụng thực tế trước khi phát hành thương mại.
