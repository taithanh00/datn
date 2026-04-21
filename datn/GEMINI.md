# Hướng dẫn Phát triển cho Gemini CLI - Dự án DATN

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

Hệ thống sử dụng 3 vai trò chính được định nghĩa trong `AppDbContext` và `Program.cs`:

### A. Manager (Quản lý) - Policy: `ManagerOnly`
Quyền hạn cao nhất, chịu trách nhiệm quản trị toàn bộ hệ thống.
- **Quản lý Nhân sự:** CRUD Giáo viên/Nhân viên, quản lý tài khoản, trạng thái hoạt động.
- **Quản lý Học sinh:** CRUD Hồ sơ học sinh, quản lý nhập học.
- **Quản lý Đào tạo:** 
    - CRUD Lớp học, Môn học (`Subject`), Thời khóa biểu (`ClassSchedule`).
    - Quản lý Chương trình học (`Curriculum`) và Kế hoạch giảng dạy (`TeachingPlan`).
    - Phân công giảng dạy (`Assignment`) giáo viên vào lớp.
- **Quản lý Tài chính:** Thiết lập kế hoạch học phí (`TuitionPlan`), theo dõi đóng phí, xác nhận thanh toán.
- **Quản lý Tiền lương:** Chốt lương (`PayrollPeriod`), tính toán lại lương, khóa bảng lương và xem phiếu lương.
- **Phê duyệt:** Duyệt/Từ chối yêu cầu nghỉ phép và các yêu cầu sửa đổi điểm danh từ nhân viên.
- **Hoạt động & Cơ sở vật chất:** Quản lý Địa điểm (`Location`) và các Sự kiện/Hoạt động (`Activity`) toàn trường.

### B. Employee (Giáo viên / Nhân viên) - Policy: `EmployeeOnly`
Dành cho cán bộ giảng dạy và nhân viên trường.
- **Nghiệp vụ Giảng dạy:**
    - Xem danh sách lớp và học sinh phụ trách.
    - Điểm danh học sinh hàng ngày.
    - Gửi báo cáo học tập (`StudyReport`) và xếp hạng (`Ranking`) định kỳ cho học sinh.
    - Theo dõi kế hoạch giảng dạy và thời khóa biểu cá nhân.
- **Quản lý Hoạt động:** Quản lý người tham gia trong các hoạt động được phân công.
- **Cá nhân:**
    - Chấm công (`WorkAttendance`): Check-in/Check-out hàng ngày.
    - Quản lý nghỉ phép: Gửi yêu cầu nghỉ phép (`EmployeeLeaveRequest`).
    - Xem phiếu lương (`Salary`) cá nhân hàng tháng.

### C. Parent (Phụ huynh) - Policy: `ParentOnly`
Dành cho phụ huynh theo dõi thông tin con em.
- **Theo dõi Học tập:** Xem báo cáo học tập, nhận xét của giáo viên và xếp hạng của con.
- **Chuyên cần:** Theo dõi lịch sử điểm danh hàng ngày của con.
- **Học phí:** Xem thông báo và tình trạng đóng học phí (`MyTuition`).
- **Hoạt động:** Xem các hoạt động/sự kiện mà con tham gia.
- **Thông tin đào tạo:** Xem chương trình học và kế hoạch giảng dạy của lớp con đang theo học.

## 3. Cấu trúc Thực thể & Quan hệ Chính
- **Account:** Liên kết 1:1 với `Employee` hoặc `Parent`. Chứa thông tin đăng nhập.
- **Student:** Trung tâm của hệ thống, liên kết với `Class`, `Parent` (qua `ParentStudent`), và các bản ghi: `Attendance`, `StudyReport`, `Tuition`, `HealthRecord`, `StudentActivity`.
- **Class:** Liên kết với `Student`, `Assignment` (giáo viên), `ClassSchedule`, `TeachingPlan`, `ClassActivity`.
- **Assignment:** Thực thể trung gian (N:N) giữa `Employee` và `Class` kèm thời gian bắt đầu.
- **Tuition:** Theo dõi học phí theo từng tháng/năm cho mỗi học sinh.

## 4. Cấu trúc Thư mục Chính
- `/Controllers`: Xử lý logic theo vai trò (Manager, Employee, Parent, TeacherSalary, Tuition...).
- `/Models`: Định nghĩa thực thể database và ViewModels cho từng tính năng.
- `/Data`: `AppDbContext` cấu hình Fluent API và quan hệ thực thể.
- `/Services`: `JwtService`, `NotificationService`, `PayrollAutoCalculationService`.
- `/Middleware`: `RefreshTokenMiddleware`.
- `/wwwroot`: `css/pages/` chứa style riêng cho từng module (Manager, Employee, Student...).

## 5. Quy ước & Tiêu chuẩn Code
- **Bảo mật:** Token lưu trong Cookie (`access_token`, `refresh_token`).
- **Real-time:** Sử dụng SignalR để đẩy thông báo (`Notification`) tới `RecipientId` tương ứng.
- **Database:** Luôn tạo Migration khi thay đổi Model. Sử dụng `OnDelete(DeleteBehavior.Restrict)` cho các quan hệ quan trọng để tránh xóa nhầm dữ liệu.

## 6. Ghi chú cho Gemini CLI
- Khi sửa lỗi logic, hãy kiểm tra Policy trong Controller để đảm bảo đúng vai trò.
- Khi thêm tính năng mới cho một Role, hãy cập nhật cả Controller tương ứng và View trong thư mục cùng tên.
- Chú ý các Composite Key trong `AppDbContext` (ví dụ: `Attendance`, `StudyReport`, `Salary`) khi thực hiện truy vấn hoặc cập nhật.
