# Hướng dẫn Phát triển cho Gemini CLI - Dự án DATN

Dự án này là một Hệ thống Quản lý Trường học (trọng tâm là Mầm non/Tiểu học) được xây dựng trên nền tảng ASP.NET Core MVC.

## 1. Tổng quan Dự án
- **Công nghệ:** .NET 10.0, ASP.NET Core MVC.
- **Cơ sở dữ liệu:** SQL Server sử dụng Entity Framework Core (Code First).
- **Bảo mật:** JWT Authentication (lưu trữ trong Cookie) kết hợp Refresh Token Middleware.
- **Tính năng Real-time:** SignalR Hub cho các thông báo tức thời.
- **Dịch vụ nền (Background Services):** 
    - `PayrollAutoCalculationService`: Tự động tính lương vào ngày 5 hàng tháng.
    - `TokenCleanupService`: Dọn dẹp Refresh Tokens hết hạn.

## 2. Danh sách Tính năng & Nghiệp vụ (Đầy đủ)

### A. Quản lý Nhân sự & Tiền lương
- **Quản lý Giáo viên/Nhân viên:** Quản lý thông tin chi tiết, vị trí và lương cơ bản.
- **Chấm công Nhân viên:** Hệ thống Check-in/Check-out hàng ngày (`WorkAttendance`).
- **Quản lý Nghỉ phép:** Luồng gửi yêu cầu và phê duyệt nghỉ phép cho nhân viên.
- **Tính lương Tự động:** Chốt lương theo chu kỳ (`PayrollPeriod`), tính toán dựa trên ngày công và lương cơ bản. Có tính năng khóa bảng lương sau khi chốt.

### B. Quản lý Đào tạo & Giảng dạy
- **Quản lý Lớp học:** Danh sách lớp, sĩ số và thông tin lớp.
- **Kế hoạch Giảng dạy:** Liên kết lớp học với chương trình học (`Curriculum`) và thời gian thực hiện (`TeachingPlan`).
- **Phân công Giảng dạy:** Chỉ định giáo viên phụ trách từng lớp (`Assignment`).

### C. Quản lý Học sinh & Phụ huynh
- **Hồ sơ Học sinh:** Thông tin cá nhân, ngày nhập học, địa chỉ.
- **Điểm danh Học sinh:** Theo dõi chuyên cần hàng ngày (`Attendance`).
- **Báo cáo Học tập:** Đánh giá định kỳ kết quả học tập (`StudyReport`) kết hợp xếp hạng (`Ranking`).
- **Hồ sơ Sức khỏe:** Theo dõi chỉ số sức khỏe của học sinh (`HealthRecord`).
- **Liên kết Gia đình:** Quản lý thông tin phụ huynh và mối quan hệ với học sinh.

### D. Quản lý Hoạt động & Cơ sở vật chất
- **Sự kiện & Hoạt động:** Tổ chức các hoạt động chung của trường hoặc riêng từng lớp (`Activity`, `ClassActivity`).
- **Quản lý Địa điểm:** Quản lý các phòng học, khu vực chức năng trong trường (`Location`).

### E. Quản lý Tài chính
- **Học phí:** Lập kế hoạch thu học phí (`TuitionPlan`) và theo dõi tình trạng đóng phí của từng học sinh (`Tuition`), bao gồm cả các khoản phí phát sinh.

## 3. Cấu trúc Thư mục Chính
- `/Controllers`: Xử lý logic yêu cầu HTTP.
- `/Models`: Định nghĩa thực thể database và cấu trúc dữ liệu.
- `/Views`: Giao diện người dùng (Razor Pages).
- `/Data`: `AppDbContext` quản lý kết nối và cấu hình thực thể EF Core.
- `/Services`: Chứa logic nghiệp vụ (JWT, Tính lương...).
- `/Middleware`: Xử lý trung gian (Refresh Token).
- `/Hubs`: Các Hub cho SignalR.
- `/wwwroot`: Chứa tài nguyên tĩnh (CSS, JS, Images). CSS được chia theo `base` và `pages`.

## 4. Lệnh Phát triển Quan trọng
- **Build dự án:** `dotnet build`
- **Chạy dự án:** `dotnet run` hoặc `dotnet watch`
- **Database:** `dotnet ef migrations add <Name>` / `dotnet ef database update`

## 5. Quy ước & Tiêu chuẩn Code
- **Ngôn ngữ:** Code (Tiếng Anh), Comment/Tài liệu (Tiếng Việt).
- **Phân quyền (Policies):** 
    - `ManagerOnly`: Quyền quản trị tối cao (Quản lý GV, HS, Lương, Tài chính).
    - `EmployeeOnly`: Quyền cho giáo viên (Điểm danh, Báo cáo học tập).
    - `ParentOnly`: Quyền cho phụ huynh (Xem thông tin con em).

## 6. Ghi chú cho Gemini CLI
- Khi hỗ trợ sửa lỗi, ưu tiên kiểm tra tính nhất quán trong `AppDbContext` và các Ràng buộc khóa ngoại.
- Tuân thủ cấu trúc CSS tách biệt trong `wwwroot/css/pages/` khi tạo UI mới.
- Chú ý đặc biệt đến luồng tính lương vì nó liên quan đến nhiều bảng dữ liệu và dịch vụ chạy ngầm.
