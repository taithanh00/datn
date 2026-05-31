# Manager Validation Plan

## Mục tiêu
Xác định các object/endpoint do `ManagerController` quản lý cần bổ sung validation, liệt kê tên field và cách validate.

## 1. Class
Endpoint: `CreateClass`, `UpdateClass`

Fields cần validate:
- `Name`
  - Required: không null/empty sau trim
  - Trim().Length >= 2 (hoặc >=3 theo quy định)
  - Không chỉ chứa whitespace
  - Không chứa ký tự không hợp lệ nếu cần (ví dụ chỉ cho phép chữ, số, khoảng trắng, dấu gạch ngang)
- `AgeFrom`, `AgeTo`
  - Required: không null
  - Phải là một trong các cặp hợp lệ: (2,3), (3,4), (4,5), (5,6)
  - `AgeFrom < AgeTo`
- `SchoolYear`
  - Optional nhưng nếu có thì phải theo pattern `yyyy-yyyy`
  - Có thể validate bằng regex `^\d{4}-\d{4}$`
- `MaxCapacity`
  - Nếu có giá trị: phải >= 1
  - Giới hạn trên hợp lý, ví dụ `<= 60` hoặc `<= 100`
- Unique
  - Kiểm tra trong DB: `Class.Name.Trim()` + `SchoolYear` không trùng lặp trong cùng niên khóa

## 2. Subject
Endpoint: `CreateSubject`, `UpdateSubject`

Fields cần validate:
- `Name`
  - Required
  - Trim().Length >= 2
  - Không chứa toàn whitespace
- `Code`
  - Required
  - Trim().ToUpperInvariant()
  - Pattern hợp lệ, ví dụ `^[A-Z0-9\-]{2,10}$`
- `Description`
  - Optional
  - Trim nếu không null
- Unique
  - `Code` không trùng với subject khác

## 3. ClassSchedule
Endpoint: `CreateClassSchedule`, `UpdateClassSchedule`

Fields cần validate:
- `ClassId`
  - Required, > 0
  - Class tồn tại trong DB
- `SubjectId`
  - Required, > 0
  - Subject tồn tại trong DB
- `EmployeeId`
  - Required, > 0
  - Employee tồn tại trong DB
- `DayOfWeek`
  - Required
  - Nằm trong khoảng 1..7 (hoặc 1..5 nếu chỉ dùng ngày thứ 2-6)
- `StartTime`, `EndTime`
  - Required
  - Parse được bằng `TimeOnly.Parse` hoặc `TryParse`
  - `StartTime < EndTime`
- `EffectiveFrom`
  - Required
  - Parse được bằng `DateOnly.Parse` hoặc `TryParse`
- `EffectiveTo`
  - Nếu có, parse được
  - Nếu có thì `EffectiveTo >= EffectiveFrom`
- `Note`
  - Optional
  - Trim nếu không null
- Business logic thêm
  - Kiểm tra trùng thời khóa biểu cùng lớp/giáo viên hợp lệ theo `ValidateScheduleRequestAsync`

## 4. Activity
Endpoint: `CreateActivity`, `UpdateActivity`

Fields cần validate:
- `Name`
  - Required
  - Trim().Length >= 2
- `Date`
  - Required
  - Parse được thành `DateOnly`
- `LocationId`
  - Optional hoặc Required tùy nghiệp vụ
  - Nếu > 0 thì Location phải tồn tại
- `OrganizerId`
  - Optional hoặc Required tùy nghiệp vụ
  - Nếu > 0 thì Employee phải tồn tại
- `ClassIds`
  - Nếu gửi thì phải là danh sách hợp lệ
  - Mỗi `classId` phải tồn tại
- `Description`
  - Optional
  - Trim nếu không null

## 5. Location
Endpoint: `CreateLocation`, `UpdateLocation`

Fields cần validate:
- `Name`
  - Required
  - Trim().Length >= 1
  - Không chỉ whitespace
- `Capacity`
  - Required
  - >= 1
  - Giới hạn trên hợp lý, ví dụ <= 100
- Optional: kiểm tra unique `Name` nếu cần

## 6. Parent
Endpoint: `CreateParent`, `UpdateParent`

Fields cần validate (đã có nhiều nhưng cần rà soát lại):
- `Username`
  - Required
  - Trim().Length >= 3
  - Không chứa ký tự lạ
- `Email`
  - Required
  - EmailAddress
  - Không trùng trong DB
- `Password`
  - Required (chỉ khi Create)
  - MinLength 9
  - Ít nhất 1 chữ hoa và 1 ký tự đặc biệt
- `FirstName`, `LastName`
  - Required
  - Trim().Length >= 1
- `Phone`
  - Optional
  - Phone format nếu có
- `Avatar`
  - Optional
  - Kiểm tra file type dung lượng nếu cần

## 9. Teacher
Endpoint: `CreateTeacher`, `UpdateTeacher`

Fields cần validate:
- `Email`
  - Required
  - EmailAddress
  - Unique
- `Username`
  - Required (create)
  - Unique
- `Password`
  - Required create
  - MinLength 9
  - Chứa ít nhất 1 chữ hoa và 1 ký tự đặc biệt
- `FirstName`, `LastName`
  - Required
- `TeacherType`
  - Required
  - `Enum.IsDefined(typeof(TeacherType), model.TeacherType)`
- `Phone`
  - Optional
  - Format điện thoại nếu có
- `Avatar`
  - Optional
  - Nếu có validate file type/size

## 10. Student
Endpoint: `CreateStudent`, `UpdateStudent`

Fields cần validate:
- `FirstName`, `LastName`
  - Required
  - StringLength(100)
- `Gender`
  - Required
  - Giá trị hợp lệ (`true` hoặc `false` nếu form dùng string, hoặc enum)
- `DateOfBirth`
  - Required
  - Parse được thành `DateOnly`
  - Tuổi trong khoảng 2..6
- `Address`
  - Optional
  - StringLength 255
- `ClassId`
  - Optional
  - Nếu >0 thì Class tồn tại
  - Kiểm tra ràng buộc lớp và tuổi học sinh với `ValidateStudentClassAssignmentAsync`
- `EnrollDate`
  - Optional
  - Nếu cung cấp, parse được

## 11. Assignment
Endpoint: `CreateAssignment`, `UpdateAssignment`

Fields cần validate:
- `EmployeeId`, `ClassId`
  - Required, > 0
  - Employee và Class tồn tại
- `StartDate`
  - Required
  - Parse được
- `EndDate`
  - Optional
  - Nếu có, parse được
  - `EndDate >= StartDate`
- `RoleInClass`
  - Optional nhưng nếu dùng giá trị cụ thể thì kiểm tra trong tập giá trị cho phép
- Business logic:
  - Kiểm tra duplicate assignment
  - Kiểm tra chỉ 1 `Chủ nhiệm` cho cùng lớp/trong cùng thời điểm

## 12. Ghi chú tổng quát
- Mọi endpoint `Create` / `Update` trong `ManagerController` cần thêm validation server-side, không chỉ dựa vào HTML/JS client-side.
- Nên dùng `ModelState.IsValid` cho các view model được trang bị attribute metadata.
- Với data nhận từ `FromBody`, cần parse an toàn bằng `TryParse` thay vì `Parse` trực tiếp.
- Với field liên quan FK (`ClassId`, `SubjectId`, `EmployeeId`, `LocationId`), hãy kiểm tra sự tồn tại của bản ghi tương ứng trong DB.
- Nếu có trường date/time, validate cả format và giá trị logic trước khi lưu.

---

### Gợi ý triến khai nhanh
1. Bổ sung validation logic cho `Activity`, `Location`.
2. Cập nhật `Class` và `Subject` để thêm kiểm tra độ dài và định dạng.
3. Bổ sung `ModelState.IsValid` cho `UpdateStudent`.
4. Đảm bảo `CreateClassSchedule`/`UpdateClassSchedule` dùng chính xác `TryParse` và kiểm tra FK.
5. Chạy thử toàn bộ endpoint manager bằng manual request hoặc unit test.
