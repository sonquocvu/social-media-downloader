# Hướng dẫn làm việc trong kho mã

## Phạm vi

Các quy tắc này áp dụng cho toàn bộ kho mã SVVideoDownloader.

## Ranh giới bắt buộc

- Giữ `SVVideoDownloader.Core` độc lập với WPF, process, filesystem, mạng và `yt-dlp`.
- `SVVideoDownloader.Infrastructure` có thể phụ thuộc `Core`; chiều ngược lại bị cấm.
- `SVVideoDownloader.App` là composition root và chứa mã WPF/MVVM.
- Không thêm cơ chế vượt DRM, tường phí, CAPTCHA hoặc truy cập tài khoản trái phép.
- Không tự động lấy cookie, token, mật khẩu hoặc hồ sơ trình duyệt.
- Không tải hay commit `yt-dlp`, `ffmpeg`, `ffprobe` hoặc binary bên thứ ba khi chưa có quyết định phân phối và rà soát giấy phép.

## Ngôn ngữ và chất lượng

- Mọi chuỗi có thể hiển thị cho người dùng phải bằng tiếng Việt.
- Tên lớp, namespace, metadata và tên tệp có thể dùng tiếng Anh.
- Nullable reference types phải được bật.
- Cảnh báo của project do ứng dụng sở hữu phải được xử lý như lỗi; không vô hiệu hóa cảnh báo chỉ để build qua.
- Tách logic nghiệp vụ khỏi code-behind. Code-behind chỉ phục vụ vòng đời hoặc hành vi thuần giao diện.

## Xác minh thay đổi

Chạy từ thư mục gốc:

```powershell
dotnet build .\SVVideoDownloader.sln --configuration Release
dotnet test .\SVVideoDownloader.sln --configuration Release --no-build
```

Khi thay đổi tích hợp công cụ ngoài, phải bổ sung kiểm thử cho đối số process, hủy tác vụ, timeout, ánh xạ lỗi và làm sạch log. Không dùng shell command được ghép từ dữ liệu người dùng.

## Tài liệu

- Ghi quyết định sản phẩm vào `docs/PRODUCT_SPEC.md`.
- Ghi thay đổi dependency hoặc ranh giới thành phần vào `docs/ARCHITECTURE.md`.
- Cập nhật trạng thái và quyết định còn mở trong `docs/TASKS.md`.
- Mọi dependency mới phải có mục đích, phiên bản, nguồn, giấy phép và tác động phân phối được ghi nhận.
