# Sơ đồ DFD & ERD — DATN (4 hình riêng)

> **Nguồn:** `final.sql` · Bỏ `Menus`, `MenuOverrides` · `Subjects` bỏ `Code`, `FeeAmount`  
> **Chỉnh sửa:** Mở từng file `.mmd` trong `docs/diagrams/` hoặc khối Mermaid bên dưới → [mermaid.live](https://mermaid.live) → Export SVG/PNG

---

## Ký hiệu ERD (không dùng chân chim)

| Ký hiệu trên cạnh | Ý nghĩa |
|-------------------|---------|
| **1 : 1** | Một bản ghi bên A tương ứng tối đa một bản ghi bên B (ví dụ `Accounts` ↔ `Employees`) |
| **1 : n** | Một bên A có nhiều bản ghi bên B (ví dụ `Classes` → nhiều `Students`) |
| **n : n** | Hai bảng liên kết qua bảng trung gian: mỗi phía **1 : n** vào bảng đó (ví dụ `Parents` ↔ `ParentStudents` ↔ `Students`) |

---

## Hình 1 — DFD mức ngữ cảnh

File: [`docs/diagrams/01-dfd-ngu-canh.mmd`](diagrams/01-dfd-ngu-canh.mmd)

```mermaid
flowchart TB
    E1["Quản trị viên"]
    E2["Giáo viên / Nhân viên"]
    E3["Phụ huynh"]
    E4["Cổng thanh toán"]

    P0(("0<br/>Hệ thống quản lý mầm non DATN"))

    E1 -->|"Yêu cầu quản trị, cấu hình, báo cáo tổng hợp"| P0
    P0 -->|"Kết quả, báo cáo, xác nhận"| E1

    E2 -->|"Điểm danh, báo cáo ngày, TKB, đơn nghỉ, chấm công"| P0
    P0 -->|"Lịch, phản hồi duyệt, phiếu lương"| E2

    E3 -->|"Tra cứu con, học phí, thanh toán, thông báo"| P0
    P0 -->|"Báo cáo con, hóa đơn, trạng thái thanh toán"| E3

    E4 -->|"Webhook / kết quả giao dịch"| P0
    P0 -->|"Yêu cầu thanh toán"| E4
```

---

## Hình 2 — DFD mức 0

File: [`docs/diagrams/02-dfd-muc-0.mmd`](diagrams/02-dfd-muc-0.mmd)

```mermaid
flowchart TB
    E1["Quản trị viên"]
    E2["Giáo viên / Nhân viên"]
    E3["Phụ huynh"]
    E4["Cổng thanh toán"]

    P1(("1.0<br/>Xác thực & phân quyền"))
    P2(("2.0<br/>Quản lý nhân sự"))
    P3(("3.0<br/>Quản lý lớp & học sinh"))
    P4(("4.0<br/>Giảng dạy & lịch học"))
    P5(("5.0<br/>Chăm sóc & theo dõi"))
    P6(("6.0<br/>Quản lý học phí"))
    P7(("7.0<br/>Lương & chấm công"))
    P8(("8.0<br/>Hoạt động"))
    P9(("9.0<br/>Thông báo & audit"))

    D1[("D1<br/>Tài khoản & vai trò")]
    D2[("D2<br/>Nhân viên & phụ huynh")]
    D3[("D3<br/>Lớp, học sinh, địa điểm")]
    D4[("D4<br/>Môn, TKB, giáo trình")]
    D5[("D5<br/>Điểm danh, báo cáo, SK")]
    D6[("D6<br/>Học phí")]
    D7[("D7<br/>Lương, chấm công")]
    D8[("D8<br/>Hoạt động")]
    D9[("D9<br/>Nhật ký hệ thống")]

    E1 --> P1 & P2 & P3 & P4 & P6 & P7 & P9
    E2 --> P1 & P2 & P4 & P5 & P7 & P8
    E3 --> P1 & P3 & P5 & P6 & P8 & P9
    E4 <-->|"Thanh toán học phí"| P6

    P1 <-->|"Đăng nhập, token"| D1
    P1 -->|"Thông tin phiên"| D2
    P2 <-->|"Hồ sơ NV, phân công"| D2
    P2 <-->|"Lớp được gán"| D3
    P3 <-->|"Lớp, HS, PH"| D2
    P3 <-->|"Lớp, HS"| D3
    P4 <-->|"TKB, GT, báo cáo học tập"| D4
    P4 <-->|"Lớp"| D3
    P4 <-->|"Giáo viên"| D2
    P5 <-->|"Điểm danh, BC ngày, SK"| D5
    P5 <-->|"Học sinh"| D3
    P5 <-->|"Người ghi nhận"| D2
    P6 <-->|"Hóa đơn, khoản phí"| D6
    P6 <-->|"Học sinh"| D3
    P7 <-->|"Chấm công, lương, nghỉ"| D7
    P7 <-->|"Nhân viên"| D2
    P8 <-->|"Sự kiện, đăng ký"| D8
    P8 <-->|"Lớp, HS"| D3
    P9 <-->|"Thông báo"| D2
    P9 -->|"Ghi log"| D9
    P1 & P6 -->|"Sự kiện bảo mật / TT"| D9
```

---

## Hình 3 — DFD mức 1

File: [`docs/diagrams/03-dfd-muc-1.mmd`](diagrams/03-dfd-muc-1.mmd)

```mermaid
flowchart TB
    subgraph EXT["Thực thể ngoài"]
        E1["Quản trị viên"]
        E2["Giáo viên"]
        E3["Phụ huynh"]
        E4["Cổng thanh toán"]
    end

    subgraph P1G["1.0 Xác thực & phân quyền"]
        P11(("1.1 Đăng nhập"))
        P12(("1.2 Quản lý token"))
        P13(("1.3 Phân quyền"))
    end

    subgraph P2G["2.0 Quản lý nhân sự"]
        P21(("2.1 Hồ sơ NV"))
        P22(("2.2 Phân công lớp"))
    end

    subgraph P3G["3.0 Quản lý lớp & học sinh"]
        P31(("3.1 Lớp học"))
        P32(("3.2 Học sinh"))
        P33(("3.3 Liên kết PH–HS"))
        P34(("3.4 Phụ huynh"))
    end

    subgraph P4G["4.0 Giảng dạy & lịch học"]
        P41(("4.1 Môn & giáo trình"))
        P42(("4.2 Kế hoạch dạy"))
        P43(("4.3 Thời khóa biểu"))
        P44(("4.4 Dạy thay"))
        P45(("4.5 Báo cáo học tập"))
    end

    subgraph P5G["5.0 Chăm sóc & theo dõi"]
        P51(("5.1 Điểm danh HS"))
        P52(("5.2 Báo cáo ngày"))
        P53(("5.3 Sức khỏe"))
    end

    subgraph P6G["6.0 Học phí"]
        P61(("6.1 Khoản phí"))
        P62(("6.2 Cấu hình phí HS"))
        P63(("6.3 Hóa đơn tháng"))
        P64(("6.4 Thanh toán"))
    end

    subgraph P7G["7.0 Lương & chấm công"]
        P71(("7.1 Chấm công"))
        P72(("7.2 Đơn nghỉ"))
        P73(("7.3 Kỳ lương"))
        P74(("7.4 Tính lương"))
    end

    subgraph P8G["8.0 Hoạt động"]
        P81(("8.1 Sự kiện"))
        P82(("8.2 Gán lớp/HS"))
    end

    subgraph P9G["9.0 Thông báo & audit"]
        P91(("9.1 Gửi thông báo"))
        P92(("9.2 Ghi audit"))
    end

    D1[("D1 Tài khoản")]
    D2[("D2 NV & PH")]
    D3[("D3 Lớp & HS")]
    D4[("D4 Giảng dạy")]
    D5[("D5 Chăm sóc")]
    D6[("D6 Học phí")]
    D7[("D7 Lương")]
    D8[("D8 Hoạt động")]
    D9[("D9 Audit")]

    E1 --> P11 & P21 & P31 & P41 & P61 & P73 & P91
    E2 --> P11 & P43 & P51 & P71 & P72 & P81
    E3 --> P33 & P52 & P63 & P64 & P91
    E4 <--> P64

    P11 --> P12 --> P13
    P11 & P12 & P13 <--> D1
    P13 --> D2

    P21 <--> D1 & D2
    P22 <--> D2 & D3
    P21 --> P22

    P31 & P32 <--> D3
    P34 <--> D1 & D2
    P33 <--> D2 & D3
    P32 --> P33

    P41 <--> D4
    P42 <--> D3 & D4
    P43 & P44 <--> D4 & D3
    P45 <--> D4 & D3 & D2
    P41 --> P42 --> P43
    P43 --> P44
    P43 --> P45

    P51 & P52 & P53 <--> D5 & D3
    P51 --> D2

    P61 & P62 <--> D6
    P63 <--> D6 & D3
    P64 <--> D6
    P61 --> P62 --> P63 --> P64

    P71 & P72 <--> D7 & D2
    P73 & P74 <--> D7
    P73 --> P74
    P71 & P72 --> P74

    P81 <--> D8 & D2
    P82 <--> D8 & D3
    P81 --> P82

    P91 <--> D2
    P92 --> D9
    P11 & P64 --> P92
```

---

## Hình 4 — ERD (1-1, 1-n, n-n)

File: [`docs/diagrams/04-erd.mmd`](diagrams/04-erd.mmd)

**Bảng quan hệ n-n** (mỗi cặp có hai nhánh 1-n vào bảng liên kết):

| Cặp thực thể | Bảng trung gian |
|--------------|-----------------|
| Parents ↔ Students | ParentStudents |
| Employees ↔ Classes | Assignments |
| Classes ↔ Curriculums | TeachingPlans |
| Classes ↔ Activities | ClassActivities |
| Students ↔ Activities | StudentActivities |

```mermaid
flowchart TB
    subgraph G1["I. Tài khoản & người dùng"]
        Roles["Roles"]
        Accounts["Accounts"]
        RefreshTokens["RefreshTokens"]
        Employees["Employees"]
        Parents["Parents"]
        Notifications["Notifications"]
    end

    subgraph G2["II. Lớp & học sinh"]
        Classes["Classes"]
        Students["Students"]
        ParentStudents["ParentStudents"]
        Locations["Locations"]
        Assignments["Assignments"]
    end

    subgraph G3["III. Giảng dạy"]
        Subjects["Subjects"]
        Curriculums["Curriculums"]
        TeachingPlans["TeachingPlans"]
        ClassSchedules["ClassSchedules"]
        Substitutions["Substitutions"]
        Rankings["Rankings"]
        StudyReports["StudyReports"]
        Holidays["Holidays"]
    end

    subgraph G4["IV. Chăm sóc"]
        Attendances["Attendances"]
        DailyReports["DailyReports"]
        HealthRecords["HealthRecords"]
    end

    subgraph G5["V. Học phí"]
        FeeItems["FeeItems"]
        StudentFeeConfigs["StudentFeeConfigs"]
        Tuitions["Tuitions"]
        TuitionDetails["TuitionDetails"]
    end

    subgraph G6["VI. Lương & chấm công"]
        PayrollPeriods["PayrollPeriods"]
        Salaries["Salaries"]
        WorkAttendances["WorkAttendances"]
        EmployeeLeaveRequests["EmployeeLeaveRequests"]
    end

    subgraph G7["VII. Hoạt động"]
        Activities["Activities"]
        ClassActivities["ClassActivities"]
        StudentActivities["StudentActivities"]
    end

    AuditLogs["AuditLogs"]

    Accounts ---|"1 : 1"| Employees
    Accounts ---|"1 : 1"| Parents
    Roles ---|"1 : n"| Accounts
    Accounts ---|"1 : n"| RefreshTokens
    Accounts ---|"1 : n"| Notifications

    Employees ---|"1 : n"| Classes
    Classes ---|"1 : n"| Students
    Parents ---|"1 : n"| ParentStudents
    Students ---|"1 : n"| ParentStudents
    Employees ---|"1 : n"| Assignments
    Classes ---|"1 : n"| Assignments

    Subjects ---|"1 : n"| Curriculums
    Classes ---|"1 : n"| TeachingPlans
    Curriculums ---|"1 : n"| TeachingPlans
    Classes ---|"1 : n"| ClassSchedules
    Subjects ---|"1 : n"| ClassSchedules
    Employees ---|"1 : n"| ClassSchedules
    Locations ---|"1 : n"| ClassSchedules
    ClassSchedules ---|"1 : n"| Substitutions
    Employees ---|"1 : n (GV gốc)"| Substitutions
    Employees ---|"1 : n (GV thay)"| Substitutions
    Students ---|"1 : n"| StudyReports
    Rankings ---|"1 : n"| StudyReports
    Employees ---|"1 : n"| StudyReports

    Students ---|"1 : n"| Attendances
    Employees ---|"1 : n"| Attendances
    Students ---|"1 : n"| DailyReports
    Students ---|"1 : n"| HealthRecords

    Students ---|"1 : n"| StudentFeeConfigs
    FeeItems ---|"1 : n"| StudentFeeConfigs
    Students ---|"1 : n"| Tuitions
    Tuitions ---|"1 : n"| TuitionDetails
    FeeItems ---|"1 : n"| TuitionDetails
    Subjects ---|"1 : n"| TuitionDetails

    Employees ---|"1 : n"| WorkAttendances
    Employees ---|"1 : n"| EmployeeLeaveRequests
    PayrollPeriods ---|"1 : n"| Salaries
    Employees ---|"1 : n"| Salaries

    Locations ---|"1 : n"| Activities
    Employees ---|"1 : n"| Activities
    Classes ---|"1 : n"| ClassActivities
    Activities ---|"1 : n"| ClassActivities
    Students ---|"1 : n"| StudentActivities
    Activities ---|"1 : n"| StudentActivities
```

---

## File ảnh đã xuất (PNG + SVG)

Thư mục: `docs/diagrams/`

| Hình | PNG | SVG | Nguồn chỉnh sửa |
|------|-----|-----|-----------------|
| DFD ngữ cảnh | `01-dfd-ngu-canh.png` | `01-dfd-ngu-canh.svg` | `01-dfd-ngu-canh.mmd` |
| DFD mức 0 | `02-dfd-muc-0.png` | `02-dfd-muc-0.svg` | `02-dfd-muc-0.mmd` |
| DFD mức 1 | `03-dfd-muc-1.png` | `03-dfd-muc-1.svg` | `03-dfd-muc-1.mmd` |
| ERD | `04-erd.png` | `04-erd.svg` | `04-erd.mmd` |

Sửa file `.mmd` rồi chạy lại trong thư mục `docs/diagrams`:

```powershell
npx @mermaid-js/mermaid-cli -i ten-file.mmd -o ten-file.png -b white -w 2400
```
