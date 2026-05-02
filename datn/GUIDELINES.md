# Nguyên tắc Phát triển Dự án (Guidelines)

Để đảm bảo chất lượng code và tính nhất quán của hệ thống, Antigravity AI cần tuân thủ các quy tắc sau:

## 1. Kiểm tra trước khi báo cáo thành công
- **Luôn luôn build dự án** (`dotnet build`) sau khi thay đổi code để đảm bảo không có lỗi cú pháp, lỗi tham chiếu hoặc lỗi logic nghiêm trọng.
- Tuyệt đối không thông báo hoàn thành nhiệm vụ nếu chưa xác nhận dự án biên dịch thành công.

## 2. Nhất quán về UI/UX
- Mọi giao diện (UI) sinh ra sau này phải tuân thủ tuyệt đối phong cách hiện tại của dự án.
- **Animation**: Sử dụng các hiệu ứng trượt, mờ dần mượt mà (như `cubic-bezier(0.16, 1, 0.3, 1)` hoặc `ease-out`).
- **Notification/Alert**: Sử dụng cấu trúc thông báo cao cấp (Premium Alert) đã được thiết kế (có bo góc, đổ bóng, icon rõ ràng và hiệu ứng trượt).
- **Style**: Tuân thủ bảng màu và hệ thống typography (font-size, font-weight) đang sử dụng.

## 3. Tính Đáp ứng (Responsive)
- Mọi trang web và thành phần UI phải được thiết kế responsive, hoạt động tốt trên cả màn hình máy tính, máy tính bảng và điện thoại di động.
- Sử dụng Media Queries và Flexbox/Grid một cách hợp lý để điều chỉnh bố cục.

## 4. Tính ổn định của hệ thống
- Khi thực hiện sửa lỗi (fix bug), phải kiểm tra kỹ để đảm bảo thay đổi **không làm ảnh hưởng tới các tính năng khác** đang hoạt động bình thường.
- Ưu tiên các giải pháp an toàn và ít gây tác động phụ nhất cho hệ thống.
