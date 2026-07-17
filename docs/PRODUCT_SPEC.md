# Đặc tả sản phẩm SVVideoDownloader

## 1. Tóm tắt

SVVideoDownloader là ứng dụng desktop riêng tư bằng tiếng Việt cho Windows x64. Ứng dụng giúp người dùng tải video công khai do chính họ sở hữu hoặc được chủ sở hữu cho phép tải từ YouTube, TikTok và Facebook.

Phiên bản hiện tại có giao diện WPF/MVVM đầu tiên và adapter tải thật. Luồng chỉ
hoạt động khi nhà phát triển tự cung cấp các executable ngoài đã được rà soát;
kho mã không tải hoặc phân phối các binary đó.

## 2. Người dùng mục tiêu

- Người dùng Việt Nam quản lý nội dung do mình tạo.
- Nhóm nội bộ đã được chủ sở hữu nội dung cấp quyền lưu bản sao.
- Người dùng có hiểu biết cơ bản về URL và thư mục trên Windows.

Ứng dụng không nhắm đến việc tải hàng loạt nội dung của bên thứ ba hoặc né tránh biện pháp kiểm soát truy cập.

## 3. Mục tiêu sản phẩm

- Nhận một URL HTTPS thuộc YouTube, TikTok hoặc Facebook.
- Cho người dùng chọn định dạng/chất lượng hợp lệ mà nguồn công khai cung cấp.
- Tải xuống với tiến độ, khả năng hủy và thông báo lỗi bằng tiếng Việt.
- Dùng `yt-dlp`, `ffmpeg` và `ffprobe` dưới dạng executable ngoài tiến trình.
- Giữ lịch sử tối thiểu và bảo vệ dữ liệu riêng tư theo mặc định.

## 4. Ngoài phạm vi

- Vượt hoặc vô hiệu hóa DRM.
- Vượt tường phí, CAPTCHA, giới hạn tài khoản hoặc cơ chế kiểm soát truy cập.
- Truy cập tài khoản khi không có ủy quyền.
- Thu thập mật khẩu, token, cookie hoặc hồ sơ trình duyệt một cách tự động.
- Tải nội dung riêng tư chỉ vì người dùng có URL.
- Hỗ trợ nền tảng ngoài YouTube, TikTok và Facebook trong giai đoạn đầu.
- Tải hay đóng gói binary bên thứ ba trong khung hiện tại.

## 5. Luồng chính dự kiến

1. Người dùng dán liên kết HTTPS.
2. Ứng dụng nhận diện nền tảng và kiểm tra cấu trúc URL.
3. Người dùng xác nhận mình sở hữu nội dung hoặc có quyền tải.
4. Ứng dụng truy vấn metadata công khai qua `yt-dlp`.
5. Người dùng chọn chất lượng, định dạng và thư mục đích.
6. Ứng dụng tải, ghép/chuyển đổi khi cần và hiển thị tiến độ.
7. Ứng dụng thông báo kết quả bằng tiếng Việt và cho phép mở thư mục chứa tệp.

Các bước 3–7 đã có luồng MVP trong giao diện. Nội dung xác nhận quyền, chính sách
tệp trùng và phân phối binary vẫn phải được phê duyệt trước khi phát hành.

## 6. Yêu cầu chức năng cho MVP

- FR-01: Chấp nhận URL HTTPS hợp lệ của đúng ba nền tảng được hỗ trợ.
- FR-02: Từ chối host giả mạo dạng `youtube.com.example.org` và URL chứa thông tin đăng nhập.
- FR-03: Yêu cầu người dùng xác nhận quyền trước mỗi tác vụ tải.
- FR-04: Không thêm cờ `yt-dlp` cho phép vượt DRM hoặc sử dụng thông tin xác thực ngoài luồng được duyệt.
- FR-05: Hiển thị metadata, lựa chọn định dạng, tiến độ, trạng thái ghép và lỗi bằng tiếng Việt.
- FR-06: Cho phép hủy an toàn và dọn tệp tạm theo chính sách đã thống nhất.
- FR-07: Không ghi URL đầy đủ, cookie, token hoặc dữ liệu nhạy cảm vào log mặc định.
- FR-08: Kiểm tra sự tồn tại và phiên bản tương thích của công cụ ngoài trước khi chạy.

## 7. Yêu cầu phi chức năng

- NFR-01: Chạy trên Windows x64 với .NET 10 và WPF.
- NFR-02: Giao diện theo MVVM; Core không phụ thuộc giao diện hay hạ tầng.
- NFR-03: Bật nullable reference types và coi cảnh báo là lỗi.
- NFR-04: Process ngoài phải hỗ trợ timeout, hủy và thu thập đầu ra có giới hạn.
- NFR-05: Không ghép shell command từ dữ liệu người dùng.
- NFR-06: Mọi thông báo hiển thị cho người dùng phải bằng tiếng Việt.

## 8. Tiêu chí hoàn thành khung ban đầu

- Solution có đủ ba project ứng dụng và hai project kiểm thử.
- App mở được cửa sổ WPF tối thiểu theo MVVM.
- Core nhận diện URL hợp lệ mà không phụ thuộc WPF, process, filesystem hay `yt-dlp`.
- Infrastructure chỉ khai báo tên công cụ ngoài; chưa chạy hoặc tải chúng.
- Solution build thành công và toàn bộ kiểm thử qua trên .NET 10.

## 9. Giả định

- Ứng dụng được dùng riêng tư/nội bộ, không phát hành công khai ở giai đoạn đầu.
- Người dùng tự chịu trách nhiệm xác nhận quyền đối với nội dung.
- Nguồn video được truy cập không yêu cầu né biện pháp kỹ thuật hoặc truy cập tài khoản trái phép.
- Các executable ngoài sẽ là bản Windows x64 và được gọi trực tiếp, không qua shell.
- Kết nối mạng, điều khoản nền tảng và bộ trích xuất của `yt-dlp` có thể thay đổi độc lập với ứng dụng.

## 10. Quyết định chưa giải quyết

- Cơ chế cung cấp công cụ ngoài: người dùng tự cài, trình cài đặt riêng hay tải theo yêu cầu sau khi chấp thuận.
- Nguồn, phiên bản cố định, checksum và giấy phép chính xác của từng binary.
- Có cần JavaScript runtime và `yt-dlp-ejs` để duy trì hỗ trợ YouTube đầy đủ hay không.
- Chính sách cập nhật và quay lui `yt-dlp`/FFmpeg.
- Định nghĩa chính xác về video Facebook/TikTok “công khai” và cách xử lý URL chuyển hướng.
- Phê duyệt cuối cùng cho nội dung xác nhận quyền và tuyên bố pháp lý MVP.
- Định dạng mặc định, quy tắc đặt tên, xử lý trùng tệp và thư mục mặc định.
- Chính sách giữ lịch sử, log, telemetry và báo lỗi.
- Ma trận kiểm thử accessibility, high contrast, DPI và phiên bản Windows tối thiểu.
- Hình thức phân phối ứng dụng và ký mã.
