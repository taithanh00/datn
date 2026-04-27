# Tài liệu Hướng dẫn Phát triển - Dự án SenHồng

Tài liệu này cung cấp cái nhìn tổng quan về kiến trúc, logic nghiệp vụ và luồng hoạt động của hệ thống Quản lý Trường học SenHồng dành cho lập trình viên mới.

## 1. Công nghệ & Kiến trúc (Tech Stack)
- **Framework:** .NET 10.0 (ASP.NET Core MVC).
- **ORM:** Entity Framework Core (SQL Server) - Chiến lược Code First.
- **Authentication:** JWT Authentication (lưu trong HttpOnly Cookie).
- **Real-time:** SignalR (RealtimeHub).
- **Background Jobs:** Hosted Services (Tính lương tự động, Dọn dẹp Token).
- **Frontend:** Razor Views, Vanilla CSS (Design System riêng), JavaScript (Module-based).

## 2. Luồng Bảo mật & Xác thực (Auth Workflow)
Hệ thống sử dụng cơ chế **JWT với Double Cookies**:
1. **Login:** `AuthController` xác thực user -> Trả về `access_token` (ngắn hạn) và `refresh_token` (dài hạn).
2. **Middleware:** `RefreshTokenMiddleware` kiểm tra mọi request. Nếu `access_token` hết hạn nhưng `refresh_token` hợp lệ -> Tự động cấp mới token mà không làm gián đoạn trải nghiệm người dùng.
3. **Claims:** Thông tin `Username`, `Role`, `EmployeeId/ParentId` và `Avatar` được nhúng trực tiếp vào Token để giảm tải truy vấn DB.

## 3. Vai trò & Phân quyền (Roles & Permissions)
Hệ thống có 3 Role chính, được quản lý bằng **Policies**:
- **Manager (Quản lý):** Quyền tối cao. Quản lý nhân sự, học sinh, lớp học, tài chính (lương, học phí) và phê duyệt yêu cầu.
- **Employee (Giáo viên/Nhân viên):** Thực hiện nghiệp vụ sư phạm (điểm danh, nhận xét), quản lý lịch dạy, chấm công và xin nghỉ phép.
- **Parent (Phụ huynh):** Theo dõi quá trình học tập, chuyên cần và đóng học phí cho con em.

## 4. Các Module Cốt lõi & Logic Nghiệp vụ

### 4.1. Quản lý Đào tạo & Lịch dạy
- **Thực thể chính:** `Class`, `Curriculum`, `ClassSchedule`, `Substitution`.
- **Logic Dạy thay (Substitution):** Khi một giáo viên nghỉ, Manager phân công người dạy thay. Hệ thống sử dụng bảng `Substitution` để "ghi đè" (override) lịch gốc trong API `GetTodaySchedule` của giáo viên. Người dạy thay sẽ được cộng thêm **0.2 công** vào lương.

### 4.2. Chấm công & Tiền lương (Hệ thống WorkUnit)
Đây là logic phức tạp nhất của dự án:
- **Đơn vị công (WorkUnit):** 
    - Ngày làm việc bình thường / Nghỉ lễ / Nghỉ phép có lương = **1.0**.
    - Tiết dạy thay = **+0.2**.
- **Tính lương:** `PayrollAutoCalculationService` chạy tự động vào ngày 5 hàng tháng. Lương được tính bằng: `(Lương cơ bản / Ngày chuẩn) * Tổng WorkUnit trong tháng`.
- **Nghỉ lễ (Holiday):** Khi Manager tạo một ngày lễ, hệ thống tự động sinh bản ghi `WorkAttendance` (Approved, WorkUnit=1.0) cho toàn bộ giáo viên đang hoạt động.

### 4.3. Phê duyệt Nghỉ phép (Leave Management)
- **Quy tắc:** Giáo viên chỉ được phép tạo tối đa **1 đơn nghỉ phép có lương** trong 1 tháng. Nghỉ không lương không giới hạn nhưng phải được duyệt.
- **Workflow:** Gửi đơn -> Manager duyệt -> Nếu duyệt, hệ thống tự động tạo bản ghi `WorkAttendance` và sinh `Substitution` nếu có tiết dạy bị ảnh hưởng.

### 4.4. Thông báo Real-time (Notifications)
- Sử dụng `INotificationService`.
- Hỗ trợ gửi thông báo cho 1 User cụ thể, theo Role, hoặc toàn bộ hệ thống qua SignalR.
- Mọi thông báo đều được lưu vào DB để tra cứu lịch sử.

## 5. Cấu trúc Database (Lưu ý cho Dev)
Hệ thống sử dụng nhiều **Composite Keys** (Khóa chính tổ hợp), cần chú ý khi `Find` hoặc `Update`:
- `WorkAttendance`: `{ EmployeeId, Date }`
- `Salary`: `{ EmployeeId, PayrollPeriodId }`
- `Assignment`: `{ EmployeeId, ClassId, StartDate }`
- `Attendance (Student)`: `{ StudentId, Date }`

## 6. Quy ước Coding
- **Controller:** Sử dụng `BaseController` để tự động nạp thông tin User/Avatar vào `ViewBag`.
- **Service Layer:** Logic nghiệp vụ nặng (tính lương, xử lý token) phải nằm ở `Services`.
- **CSS/JS:** Được tổ chức theo trang tại `wwwroot/css/pages/` và `wwwroot/js/pages/`.

---
*Tài liệu này được cập nhật lần cuối vào ngày 27/04/2026 bởi Antigravity AI.*
