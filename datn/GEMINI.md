# Hướng dẫn Phát triển cho Dự án DATN - Hệ thống Quản lý Trường học

Dự án này là một Hệ thống Quản lý Trường học (trọng tâm là Mầm non/Tiểu học) được xây dựng trên nền tảng ASP.NET Core MVC.

## 1. Tổng quan Dự án
- **Công nghệ:** .NET 10.0, ASP.NET Core MVC.
- **Cơ sở dữ liệu:** SQL Server sử dụng Entity Framework Core (Code First).
- **Bảo mật:** JWT Authentication (lưu trữ trong Cookie) kết hợp Refresh Token Middleware.
- **Tính năng Real-time:** SignalR Hub (`RealtimeHub`) cho các thông báo tức thời.
- **Dịch vụ nền (Background Services):** 
    - `PayrollAutoCalculationService`: Tự động tính lương vào ngày 5 hàng tháng.
    - `TokenCleanupService`: Dọn dẹp Refresh Tokens hết hạn.

## 2. Phân quyền & Vai trò (Roles)
Hệ thống sử dụng 3 vai trò chính:
- **Manager (Quản lý):** Quyền hạn cao nhất, quản trị nhân sự, học sinh, đào tạo, tài chính, và phê duyệt các yêu cầu.
- **Employee (Giáo viên / Nhân viên):** Thực hiện nghiệp vụ giảng dạy, điểm danh học sinh, gửi báo cáo, chấm công cá nhân và xin nghỉ phép.
- **Parent (Phụ huynh):** Theo dõi học tập, chuyên cần, học phí và các hoạt động của con em.

## 3. Cấu trúc Thực thể & Quan hệ Chính
Hệ thống có cấu trúc DB phức tạp với nhiều quan hệ và Composite Keys:
- **Account:** Trung tâm định danh, liên kết 1:1 với `Employee` hoặc `Parent`.
- **Student:** Liên kết với `Class`, `ParentStudent`, và các bản ghi hoạt động/học tập.
- **Class:** Trung tâm của đào tạo, liên kết với `Student`, `Assignment`, `ClassSchedule`, `TeachingPlan`.
- **Composite Keys quan trọng (Cần lưu ý khi truy vấn/cập nhật):**
    - `Attendance`: `{ StudentId, Date }`
    - `StudyReport`: `{ StudentId, Date }`
    - `WorkAttendance`: `{ EmployeeId, Date }`
    - `Salary`: `{ EmployeeId, PayrollPeriodId }`
    - `Assignment`: `{ EmployeeId, ClassId, StartDate }`
    - `TeachingPlan`: `{ ClassId, CurriculumId, StartDate }`

## 4. Cấu trúc Thư mục Chính
- `/Controllers`: Logic xử lý theo vai trò và module (Manager, Employee, Parent, TeacherSalary, Tuition...).
- `/Models`: Định nghĩa thực thể database và ViewModels.
- `/Data`: `AppDbContext` cấu hình Fluent API và quan hệ thực thể.
- `/Services`: Các dịch vụ logic nghiệp vụ (JWT, Notification, Student, Parent).
- `/Middleware`: `RefreshTokenMiddleware` xử lý gia hạn token tự động.
- `/wwwroot`: Tài nguyên tĩnh, CSS theo module tại `css/pages/`.

## 5. Quy ước & Tiêu chuẩn Phát triển
- **Database:** Sử dụng Entity Framework Core. Khi thay đổi Model, luôn chạy `dotnet ef migrations add <Name>` và `dotnet ef database update`.
- **Bảo mật:** 
    - Token được lưu trong Cookie (`access_token`, `refresh_token`).
    - Middleware tự động kiểm tra và làm mới token nếu cần.
    - Phân quyền dựa trên Policy (`ManagerOnly`, `EmployeeOnly`, `ParentOnly`).
- **Real-time:** Sử dụng `INotificationService` để đẩy thông báo qua SignalR Hub.
- **Frontend:** ASP.NET Core MVC (Razor Views). CSS được tổ chức theo module để tránh xung đột.

## 6. Lệnh Building và Running (Dự kiến)
- **Chạy ứng dụng:** `dotnet run`
- **Build:** `dotnet build`
- **Cập nhật Database:** `dotnet ef database update`
- **Thêm Migration:** `dotnet ef migrations add <MigrationName>`

## 7. Ghi chú Quan trọng cho Gemini CLI
- Luôn kiểm tra Policy trong Controller trước khi thực hiện các thay đổi logic để đảm bảo đúng phân quyền.
- Khi làm việc với các thực thể có Composite Key, hãy đảm bảo cung cấp đủ các trường định danh trong điều kiện `Where` hoặc `Find`.
- Adhere strictly to the existing architectural patterns (MVC + Service Layer).
