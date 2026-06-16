# 📚 DATN PROJECT - TOÀN BỘ PHÂN TÍCH HỆ THỐNG

**Ngày phân tích:** 2026-06-07  
**Dự án:** Hệ thống quản lý mầm non (School Management System)

---

# **PHẦN 1: DATABASE SCHEMA ANALYSIS** 🗄️

## 1.1 **Danh sách 37 Tables với Mô tả Chức Năng**

### **A. HỆ THỐNG & QUẢN LÝ TÀI KHOẢN**

| Table | PK | Chức Năng | 
|-------|-----|----------|
| **Roles** | Id | Định nghĩa vai trò (Manager, Employee, Parent) |
| **Accounts** | Id | Tài khoản đăng nhập (Username, Email, Password Hash) |
| **RefreshTokens** | Id | Token để refresh session (JwtToken, ExpiresUtc) |
| **AuditLogs** | Id | Ghi lịch sử thay đổi (UserId, Action, EntityName, OldValues, NewValues, IpAddress) |

### **B. QUẢN LÝ NHÂN SỰ & CÔNG VIỆC**

| Table | PK | Chức Năng |
|-------|-----|----------|
| **Employees** | Id | Giáo viên & nhân viên (FirstName, Email, Phone, Degree, Specialization, EmployeeCode) |
| **WorkAttendances** | (EmployeeId, Date) | Check-in/Check-out hàng ngày (CheckInAtUtc, CheckOutAtUtc, LateMinutes) |
| **EmployeeLeaveRequests** | Id | Đơn xin nghỉ phép (StartDate, EndDate, Reason, Status) |
| **Assignments** | (EmployeeId, ClassId, StartDate) | Phân công dạy lớp nào (EndDate, IsActive) |
| **ClassSchedules** | Id | Lịch biểu (ClassId, SubjectId, DayOfWeek, TimeStart, TimeEnd, LocationId) |
| **Substitutions** | Id | Giáo viên thay thế (LeaveRequestId, CoveringEmployeeId, Date) |
| **Salaries** | (EmployeeId, PayrollPeriodId) | Bảng lương (BaseSalary, Deductions, Bonuses, NetSalary) |
| **PayrollPeriods** | Id | Kỳ tính lương (StartDate, EndDate, Status) |
| **ClassCoverageBonuses** | Id | Thưởng khi thay thế lớp (Bonus, Status) |

### **C. QUẢN LÝ LỚP HỌC & HỌC SINH**

| Table | PK | Chức Năng |
|-------|-----|----------|
| **Classes** | Id | Lớp học (ClassName, AgeGroup, YearStarted, Capacity, LeadTeacherId) |
| **Students** | Id | Học sinh (FirstName, LastName, DateOfBirth, StudentCode, ClassId, IsActive) |
| **Parents** | Id | Phụ huynh (FirstName, Email, Phone, Relationship, Occupation) |
| **ParentStudents** | (ParentId, StudentId) | Liên kết cha-mẹ-con (Relationship: "Cha"/"Mẹ"/"Ông"/"Bà") |
| **Subjects** | Id | Môn học (SubjectName, SubjectCode, Description) |
| **Curriculums** | Id | Chương trình học (CurriculumName, GradeLevel, Description) |

### **D. ĐIỂM DANH & BÁO CÁO HỌC TẬP**

| Table | PK | Chức Năng |
|-------|-----|----------|
| **Attendances** | (StudentId, Date) | Điểm danh (Status: Present/Absent/Late, Note) |
| **StudyReports** | (StudentId, Date) | Báo cáo học tập (RankingId, Comment) |
| **Rankings** | Id | Xếp loại (RankingName: "Excellent"/"Good"/"Average"/"Poor") |
| **HealthRecords** | (StudentId, Date) | Sức khỏe (Temperature, Weight, Height, HealthNotes) |
| **DailyReports** | Id | Báo cáo hàng ngày (StudentId, Date, EatingStatus, SleepingStatus, MoodNote) |

### **E. QUẢN LÝ HỌC PHÍ & THANH TOÁN**

| Table | PK | Chức Năng |
|-------|-----|----------|
| **FeeItems** | Id | Loại phí (FeeItemName, Description, Amount, IsActive) |
| **Tuitions** | (StudentId, Month, Year) | Hóa đơn học phí (IsPaid, PaidAt, PaymentMethod, ExtraFee, TransactionId) |
| **TuitionDetails** | Id | Chi tiết từng khoản phí (TuitionId, FeeItemId, Amount, TotalAmount) |
| **StudentFeeConfigs** | Id | Tùy chỉnh phí per học sinh (StudentId, FeeItemId, DiscountPercent, CustomAmount) |

### **F. HOẠT ĐỘNG & DINH DƯỠNG**

| Table | PK | Chức Năng |
|-------|-----|----------|
| **Activities** | Id | Hoạt động ngoài giờ (ActivityName, Date, Location, Description) |
| **ClassActivities** | (ClassId, ActivityId) | Liên kết lớp-hoạt động |
| **StudentActivities** | (StudentId, ActivityId) | Liên kết học sinh-hoạt động (IsParticipated, Note) |
| **Menus** | Id | Thực đơn (DayOfWeek: 1-5, MealType, DishName, Ingredients, Calories) |
| **MenuOverrides** | Id | Ghi đè thực đơn (MenuId, StudentId/ClassId, OverrideDish, Reason) |

### **G. CƠ SỞ HẠ TẦNG & KHÁC**

| Table | PK | Chức Năng |
|-------|-----|----------|
| **Locations** | Id | Địa điểm (LocationName: "Phòng 101", "Sân chơi", Description) |
| **Holidays** | Id | Ngày lễ (HolidayName, StartDate, EndDate, IsPublic) |
| **Notifications** | Id | Thông báo (RecipientId/RecipientRole, Title, Message, Type, Url, IsRead, CreatedAt) |

---

## 1.2 **Mối Liên Kết Giữa Các Table**

### **ONE-TO-ONE (1:1)**

```
Account (1) ←──────→ (1) Employee
  FK: Employee.AccountId → Account.Id
  Example: Account "nguyenvana" → Employee "Nguyễn Văn A"
  Action: DELETE Account → CASCADE delete Employee

Account (1) ←──────→ (1) Parent
  FK: Parent.AccountId → Account.Id
  Example: Account "phuhuynh01" → Parent "Trần Thị B"
```

### **ONE-TO-MANY (1:N)**

```
Role (1) ──────→ (N) Accounts
  Example: Role "Employee" → 100+ accounts

Class (1) ──────→ (N) Students
  Example: Class "Lớp 1A" → 25 học sinh

Student (1) ──────→ (N) Tuitions
  Example: Student "Hùng" → Tuition_Jan, Tuition_Feb, ...
  UNIQUE: (StudentId, Month, Year) - 1 hóa đơn/tháng

Employee (1) ──────→ (N) Assignments
  Example: Employee "Cô Lan" → {Lớp 1A, Lớp 2B, Lớp 3C}

Employee (1) ──────→ (N) WorkAttendances
  Example: Employee "Thầy X" → {2025-06-01: 8:05am, 2025-06-02: 8:00am, ...}

Tuition (1) ──────→ (N) TuitionDetails
  Example: Tuition_Jan → {HocPhi, AnCom, MuongKhoac}
```

### **MANY-TO-MANY (N:N)**

```
Parent (N) ←──Junction──→ (N) Student via ParentStudents
  PK: (ParentId, StudentId)
  Example: Parent "Bình" → {Student "Tuấn" (Cha), Student "Bình" (Cha)}

Class (N) ←──Junction──→ (N) Activity via ClassActivities
  Example: Class "1A" → {Activity: Picnic, Activity: Festival}

Student (N) ←──Junction──→ (N) Activity via StudentActivities
  Example: Student "Tuấn" → {Activity: Picnic, Activity: Singing}
```

### **Composite Primary Keys**

| Table | PK | Lý do |
|-------|-----|-------|
| Tuitions | (StudentId, Month, Year) | 1 hóa đơn/tháng/học sinh |
| Attendances | (StudentId, Date) | 1 bản ghi/ngày/học sinh |
| WorkAttendances | (EmployeeId, Date) | 1 bản ghi/ngày/nhân viên |
| Assignments | (EmployeeId, ClassId, StartDate) | 1 phân công/ngày bắt đầu |
| Salaries | (EmployeeId, PayrollPeriodId) | 1 bảng lương/kỳ |
| ParentStudents | (ParentId, StudentId) | 1 mối quan hệ/cặp |
| ClassActivities | (ClassId, ActivityId) | 1 bản ghi/cặp |
| StudentActivities | (StudentId, ActivityId) | 1 bản ghi/cặp |

---

# **PHẦN 2: VIEW & CONTROLLER ANALYSIS** 🎨

## 2.1 **Dashboard Views & Data Binding**

### **Manager Dashboard** (`/Manager/Index`)

**Dữ liệu:**
- Revenue chart (6 tháng)
- Attendance chart (today)
- Stats cards
- Latest leaves table

**Tables Used:**
- Tuitions, TuitionDetails, Attendances, EmployeeLeaveRequests, Students, Employees

**Display:**
```
Stats:
  - Tổng học sinh: Students.Count()
  - Tổng giáo viên: Employees.Count(role=="Employee")
  - Đơn xin nghỉ chờ duyệt: EmployeeLeaveRequests.Count(status=="Pending")
  - Doanh thu tháng: TuitionDetails.Sum(currentMonth, IsPaid=true)
  - GV check-in hôm nay: WorkAttendances.Count(today, CheckInAtUtc!=null)

Charts:
  - Revenue: 6 tháng gần nhất
  - Attendance: {present: 120, absent: 5, late: 2}

Table:
  - Top 5 pending leaves (Name, DateRange, Reason)
```

### **Employee Dashboard** (`/Employee/Index`)

**Tables Used:**
- Assignments, Classes, ClassSchedules, Attendances, StudyReports, Rankings, Salaries

**Display:**
```
Stats:
  - Lớp phụ trách: Current assigned class
  - Sĩ số hôm nay: Attendances.Count(today, status=="Present")
  - Check-in lúc: WorkAttendances.CheckInAtUtc.ToString("HH:mm")
  - Lương tháng trước: Salaries.Sum(lastMonth)

Charts:
  - Student Ranking: {Excellent, Good, Average, Poor}

Table:
  - Today schedule (Time, Class, Subject)
```

### **Parent Dashboard** (`/Parent/Children`)

**Model:** ParentChildrenViewModel

**Tables Used:**
- ParentStudents, Students, Assignments, Attendances, Tuitions, DailyReports

**Display:**
```
- Danh sách con + lịch dạy hôm nay
- Study reports
- Tuition status (outstanding, paid)
- Attendance rate
```

---

## 2.2 **Controllers (15+, 100+ Endpoints)**

### **Main Controllers**

| Controller | Key Endpoints |
|-----------|---|
| **HomeController** | GET /Home/Index (redirect by role) |
| **TuitionController** | GET/POST /Tuition/, POST /Tuition/CreateMoMoPayment/{id}, GET/POST MoMo callback |
| **NotificationController** | GET /Notification/Api/Latest, POST /Notification/Api/MarkRead/{id} |
| **ManagerController** | GET /Manager/, GET /Manager/Api/DashboardStats |
| **EmployeeController** | GET /Employee/, GET /Employee/Api/DashboardStats |
| **ParentController** | GET /Parent/Children, GET /Parent/StudyReports |

### **Manager Subfolder (7 Controllers)**
- StudentController, ActivityController, ClassScheduleController
- NutritionController, LeaveApprovalController
- HolidayManagementController, SystemLogController

### **Employee Subfolder (5 Controllers)**
- DailyReportController, TimeAttendanceController
- LeaveRequestController, TeacherSalaryController

---

## 2.3 **Services (10+)**

| Service | Key Methods |
|---------|---|
| **NotificationService** | SendToUserAsync, SendToRoleAsync, SendToAllAsync, GetUserNotificationsAsync |
| **EmailService** | SendEmailAsync (Gmail SMTP) |
| **MoMoService** | CreatePaymentAsync, ValidateSignature (HMACSHA256) |
| **DailyReportService** | GetReportAsync, SaveReportAsync, GetClassReportsAsync |
| **HealthService** | GetHistoryAsync, SaveRecordAsync, GetLatestRecordAsync |
| **StudentService** | GenerateStudentCodeAsync, CreateStudentAsync, CheckPotentialDuplicateAsync |
| **PayrollAutoCalculationService** | Auto-calculate salary (daily @ 1 AM VNT) |
| **ClassCoverageService** | ProcessLeaveApprovalAsync, CanClassOperateOnDateAsync |
| **JwtService** | GenerateToken, ValidateToken |

---

# **PHẦN 3: EMAIL & NOTIFICATION SYSTEM** 📧

## 3.1 **Email Configuration**

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderName": "Trường Mầm Non Sen Hồng",
    "SenderEmail": "thanhbinhngh@gmail.com",
    "Username": "thanhbinhngh@gmail.com",
    "Password": "oomoebrhmlmpwhzj"  // Google App Password
  }
}
```

**Library:** MailKit.Net.Smtp  
**Protocol:** STARTTLS (port 587)

## 3.2 **Email Triggers**

| Event | Template | Sender |
|-------|----------|--------|
| Password Reset | HTML with reset link (1h expiry) | AuthController |
| Contact Form | Styled HTML email | LandingPageController |

## 3.3 **Notification System**

**Database:**
```csharp
{
  Id: int,
  RecipientId: int?,           // Specific user
  RecipientRole: string?,      // All users with role
  Title: string,
  Message: string,
  Type: "info"/"success"/"warning"/"error",
  Url: string?,                // Click → redirect
  CreatedAt: DateTime,
  IsRead: bool
}
```

**Real-time via SignalR:**
```
RealtimeHub (3 groups):
  - Managers
  - Employees
  - Parents

Method: ReceiveNotification
Data: {id, title, message, type, url, createdAt}
```

**Notification Triggers:**

| Event | Service | Recipient |
|-------|---------|-----------|
| MoMo payment success | TuitionController.MoMoIPN | Parent (SendToUserAsync) |
| Leave approved/denied | LeaveRequestController | Manager (SendToRoleAsync) |
| Monthly salary calculated | SalaryController | Employee (SendToRoleAsync) |
| Class cancelled (teacher on leave) | ClassCoverageService | Parent (SendToRoleAsync) |

---

# **PHẦN 4: MOMO PAYMENT INTEGRATION** 💳

## 4.1 **Complete Payment Flow**

```
1. Parent clicks "THANH TOÁN MOMO"
   POST /Tuition/CreateMoMoPayment/{tuitionId}
   ↓
2. Backend calculates:
   Amount = TuitionDetails.Sum() + ExtraFee
   OrderInfo = "Thanh toan hoc phi thang {month}/{year} cho be {name}"
   ↓
3. MoMoService.CreatePaymentAsync()
   - Generate orderId, requestId
   - Encode extraData to base64
   - Compute HMACSHA256 signature
   - POST to MoMo API
   ↓
4. Return: payUrl (MoMo gateway link)
   ↓
5. Frontend redirects: window.location = payUrl
   ↓
6. Parent pays on MoMo
   ↓
7. Two callbacks:
   a) Redirect: GET /Tuition/MoMoReturn (for display only)
      ⚠️ NOT source of truth
   
   b) Webhook: POST /Tuition/MoMoIPN (async, SOURCE OF TRUTH)
      ✅ Validate HMACSHA256 signature
      ✅ Update DB: Tuition.IsPaid = true
      ✅ Send notification to parent
```

## 4.2 **Configuration**

```json
{
  "MoMo": {
    "PartnerCode": "MOMO",
    "AccessKey": "F8BBA842ECF85",
    "SecretKey": "K951B6PE1waDMi640xX08PD3vg6EkVlz",
    "PaymentUrl": "https://test-payment.momo.vn/v2/gateway/api/create",
    "ReturnUrl": "https://tributary-pacifier-dish.ngrok-free.dev/Tuition/MoMoReturn",
    "IpnUrl": "https://tributary-pacifier-dish.ngrok-free.dev/Tuition/MoMoIPN"
  }
}
```

## 4.3 **Security: HMACSHA256 Signature**

**Request Signature:**
```
rawHash = "accessKey=...&amount=1000000&extraData=...&ipnUrl=...&orderId=TUITION_1_xxx&orderInfo=Thanh_toan_hoc_phi&partnerCode=MOMO&redirectUrl=...&requestId=...&requestType=captureWallet"

signature = HMACSHA256(rawHash, secretKey)
         = "f3d4e5c6a1b2..." (hex, 64 chars, lowercase)
```

**IPN Validation:**
```csharp
public bool ValidateSignature(string signature, string rawHash)
{
    string computedSig = ComputeHmacSha256(rawHash, secretKey);
    return computedSig.Equals(signature, StringComparison.InvariantCultureIgnoreCase);
    // ✅ True → webhook is valid
}
```

## 4.4 **Backend MoMo Update**

```csharp
[HttpPost("MoMoIPN")]
public async Task<IActionResult> MoMoIPN([FromBody] JsonElement requestBody)
{
    // 1. Validate signature
    if (!ValidateSignature(...)) return BadRequest();
    
    // 2. If resultCode == "0" (success)
    var tuition = await _context.Tuitions.FindAsync(tuitionId);
    if (tuition != null && !tuition.IsPaid)
    {
        tuition.IsPaid = true;
        tuition.PaymentMethod = "MoMo";
        tuition.TransactionId = transId;
        tuition.PaidAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();  // Auto audit log!
        
        // 3. Send notification to parent
        await _notificationService.SendToUserAsync(
            parentAccountId,
            "Xác nhận đã đóng học phí",
            $"Hệ thống đã nhận được học phí tháng {tuition.Month}/{tuition.Year}...",
            "success",
            "/Tuition/MyTuition"
        );
    }
    
    return NoContent();  // 204 for MoMo webhook
}
```

---

# **PHẦN 5: AUDIT LOG SYSTEM** 📝

## 5.1 **AuditLog Table**

```csharp
public class AuditLog
{
  Id: int,
  UserId: string?,              // Người thực hiện
  UserName: string?,            // Username
  Action: "Added"/"Modified"/"Deleted",
  EntityName: string,           // Tên bảng
  EntityId: string?,            // ID bản ghi (JSON)
  OldValues: string?,           // Giá trị cũ (JSON)
  NewValues: string?,           // Giá trị mới (JSON)
  IpAddress: string?,           // IP người thực hiện
  CreatedAtUtc: DateTime        // Thời gian ghi
}
```

## 5.2 **Auto-logging via AppDbContext**

**Mechanism:**
1. Mỗi `SaveChangesAsync()` call
2. `ChangeTracker` phát hiện thay đổi
3. Tự động tạo AuditLog entry
4. Ghi: người dùng, IP, action, old/new values

**Tracked Entities:**
- Students, Parents, Employees, Accounts
- Tuitions, TuitionDetails, FeeItems
- Attendances, StudyReports, DailyReports, HealthRecords
- Assignments, ClassSchedules, Classes, Subjects
- EmployeeLeaveRequests, Salaries, PayrollPeriods
- Menus, MenuOverrides, Activities
- Notifications, Holidays, Locations
- **Tất cả entities** (except AuditLog)

## 5.3 **Viewing Audit Logs**

**URL:** `/Manager/SystemLog`

**Features:**
- 📊 Bảng log với phân trang (50 bản ghi/trang)
- 🔍 Tìm kiếm toàn văn
- 🎯 Bộ lọc nâng cao:
  - Ngày từ-đến
  - Entity type
  - Username
  - Action (Added/Modified/Deleted)
- 📋 Xem chi tiết: Old → New (JSON diff)

**API:**
```
GET /Manager/SystemLog/GetData?page=1&pageSize=50&search=...&entityName=Student&userName=...&logAction=Modified
```

---

# **PHẦN 6: STATISTICS & CHARTS** 📊

## 6.1 **Manager Dashboard**

### **Revenue Chart (6 months)**
```json
[
  {label: "Tháng 1/2026", value: 50000000},
  {label: "Tháng 2/2026", value: 55000000},
  ...
]
```
Source: `TuitionDetails.Where(IsPaid).Sum(Amount)` per month

### **Attendance Chart (Today)**
```json
{present: 120, absent: 5, late: 2}
```
Source: `Attendances.Where(date==today).GroupBy(Status)`

### **Stats Cards**
```
- Học sinh: Students.Count()
- Giáo viên: Employees.Count(role=="Employee")
- Đơn xin nghỉ: EmployeeLeaveRequests.Count(status=="Pending")
- Doanh thu tháng: TuitionDetails.Sum(currentMonth, IsPaid=true)
- GV check-in: WorkAttendances.Count(today, CheckInAtUtc!=null)
```

### **Latest Leaves Table**
- Top 5 pending EmployeeLeaveRequests
- Columns: Name, Date range, Reason

## 6.2 **Employee Dashboard**

### **Student Ranking Chart**
```json
[
  {label: "Excellent", value: 10},
  {label: "Good", value: 15},
  {label: "Average", value: 20},
  {label: "Poor", value: 5}
]
```

### **Stats Cards**
```
- Lớp phụ trách: Current assigned class
- Sĩ số hôm nay: Attendances.Count(today, class==myClass)
- Check-in lúc: WorkAttendances.CheckInAtUtc.ToString("HH:mm")
- Lương tháng trước: Salaries.Sum(lastMonth)
```

### **Today Schedule**
- Classes today (DayOfWeek = current day)
- Columns: Time, Class, Subject

## 6.3 **Parent Dashboard**

- Tuition Status: Outstanding, Paid
- Children Ranking Chart
- Attendance Rate Chart

---

# **PHẦN 7: TECHNOLOGY STACK** 🏗️

| Component | Technology |
|-----------|----------|
| **Backend** | ASP.NET Core 8.0 (C#) |
| **Frontend** | ASP.NET Core MVC + JavaScript |
| **Charts** | Chart.js (CDN) |
| **Database** | SQL Server |
| **ORM** | Entity Framework Core |
| **Real-time** | SignalR (WebSocket) |
| **Email** | MailKit (SMTP) |
| **Payment** | MoMo API (HMACSHA256) |
| **Authentication** | JWT + Refresh Token |

---

# **QUICK REFERENCE** 📌

| Khía cạnh | Chi tiết |
|-----------|---------|
| **Database Tables** | 37 tables, composite keys, soft delete |
| **Relationships** | 1:1 (Account-Employee), 1:N (Class-Students), N:N (Parent-Students) |
| **Dashboards** | Manager, Employee, Parent (+ 40+ CRUD views) |
| **Charts** | Revenue, Attendance, Ranking, Salary (Chart.js) |
| **Audit Log** | Auto via ChangeTracker, view at `/Manager/SystemLog` |
| **Notification** | Real-time via SignalR (3 groups) + Database storage |
| **Email** | Gmail SMTP (port 587, STARTTLS) + reset password + contact form |
| **MoMo Payment** | HMACSHA256 signature, IPN webhook, Amount from TuitionDetails |
| **Security** | JWT + Refresh token, Role-based access control |
| **Background Jobs** | Payroll calculation (1 AM daily), Token cleanup |

---

**Document Version:** 1.0  
**Last Updated:** 2026-06-07
