using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace datn.Controllers
{
    [Authorize(Roles = "Manager")]
    [Route("[controller]")]
    public class ManagerController : BaseController
    {
        private static readonly TimeOnly SchoolStart = new(7, 0);
        private static readonly TimeOnly LunchStart = new(11, 0);
        private static readonly TimeOnly LunchEnd = new(13, 0);
        private static readonly TimeOnly SchoolEnd = new(16, 30);

        private readonly IStudentService _studentService;
        private readonly IParentService _parentService;

        public ManagerController(AppDbContext context, IStudentService studentService, IParentService parentService) : base(context)
        {
            _studentService = studentService;
            _parentService = parentService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Bảng điều khiển Quản lý";
            return View();
        }

        [HttpGet("Api/DashboardStats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var nowVnt = GetVntNow();
            var today = DateOnly.FromDateTime(nowVnt.DateTime);

            // 1. Stats Cards
            var totalStudents = await _context.Students.CountAsync();
            var totalTeachers = await _context.Employees
                .Include(e => e.Account).ThenInclude(a => a.Role)
                .CountAsync(e => e.Account.Role.Name == "Employee");
            var pendingLeaves = await _context.EmployeeLeaveRequests.CountAsync(r => r.Status == "Pending");

            // Doanh thu tháng hiện tại (Tổng từ TuitionDetails của các hóa đơn đã nộp)
            var currentMonthRevenue = await _context.TuitionDetails
                .Include(td => td.Tuition)
                .Where(td => td.Tuition.Month == nowVnt.Month && td.Tuition.Year == nowVnt.Year && td.Tuition.IsPaid)
                .SumAsync(td => td.TotalAmount);

            // 2. Doanh thu 6 tháng gần nhất (Biểu đồ đường)
            var revenueChart = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var d = nowVnt.AddMonths(-i);
                var rev = await _context.TuitionDetails
                    .Include(td => td.Tuition)
                    .Where(td => td.Tuition.Month == d.Month && td.Tuition.Year == d.Year && td.Tuition.IsPaid)
                    .SumAsync(td => td.TotalAmount);
                revenueChart.Add(new { label = $"Tháng {d.Month}/{d.Year}", value = rev });
            }

            // 3. Sĩ số học sinh hôm nay (Biểu đồ tròn)
            var presentStudents = await _context.Attendances.CountAsync(a => a.Date == today && a.Status == "Present");
            var absentStudents = totalStudents - presentStudents;

            // 4. Đơn nghỉ phép mới nhất
            var latestLeaves = await _context.EmployeeLeaveRequests
                .Include(r => r.Employee)
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.CreatedAtUtc)
                .Take(5)
                .Select(r => new {
                    id = r.Id,
                    name = r.Employee.LastName + " " + r.Employee.FirstName,
                    startDate = r.StartDate.ToString("dd/MM/yyyy"),
                    endDate = r.EndDate.ToString("dd/MM/yyyy"),
                    reason = r.Reason
                })
                .ToListAsync();

            return Json(new {
                success = true,
                stats = new {
                    totalStudents,
                    totalTeachers,
                    pendingLeaves,
                    monthlyRevenue = currentMonthRevenue,
                    teacherAttendanceToday = await _context.WorkAttendances.CountAsync(w => w.Date == today && w.CheckInAtUtc != null)
                },
                charts = new {
                    revenue = revenueChart,
                    attendance = new { present = presentStudents, absent = absentStudents }
                },
                latestLeaves
            });
        }

        private static DateTimeOffset GetVntNow()
        {
            var utcNow = DateTimeOffset.UtcNow;
            TimeZoneInfo tz;
            try { tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
            catch { tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
            return TimeZoneInfo.ConvertTime(utcNow, tz);
        }

        [HttpGet("Students")]
        public IActionResult Students()
        {
            ViewData["Title"] = "Danh sách Học sinh";
            return View();
        }

        [HttpGet("StudentDetail/{id:int}")]
        public async Task<IActionResult> StudentDetail(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null) return NotFound();
            
            ViewData["Title"] = $"Hồ sơ học sinh - {student.FirstName} {student.LastName}";
            return View(student);
        }

        [HttpGet("Teachers")]
        public IActionResult Teachers()
        {
            ViewData["Title"] = "Danh sách Giáo viên";
            return View();
        }

        [HttpGet("Assignments")]
        public IActionResult Assignments()
        {
            ViewData["Title"] = "Phân công Giảng dạy";
            return View();
        }

        [HttpGet("Classes")]
        public IActionResult Classes()
        {
            ViewData["Title"] = "Lớp học";
            return View();
        }

        [HttpGet("Subjects")]
        public IActionResult Subjects()
        {
            ViewData["Title"] = "Danh mục môn học";
            return View();
        }

        [HttpGet("Schedules")]
        public IActionResult Schedules()
        {
            ViewData["Title"] = "Thời khóa biểu";
            return View();
        }

        // ============ ASSIGNMENT API ============

        [HttpGet("Api/Assignments")]
        public async Task<IActionResult> GetAssignments()
        {
            try
            {
                var assignments = await _context.Assignments
                    .Include(a => a.Employee)
                    .Include(a => a.Class)
                    .OrderByDescending(a => a.StartDate)
                    .ToListAsync();

                var result = assignments.Select(a => new
                {
                    employeeId = a.EmployeeId,
                    employeeName = a.Employee?.FullName ?? "N/A",
                    classId = a.ClassId,
                    className = a.Class?.Name ?? "N/A",
                    startDate = a.StartDate.ToString("yyyy-MM-dd"),
                    endDate = a.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    roleInClass = a.RoleInClass ?? ""
                }).ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Api/Assignment")]
        public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                var startDate = DateOnly.Parse(model.StartDate);
                var exists = await _context.Assignments.AnyAsync(a =>
                    a.EmployeeId == model.EmployeeId && a.ClassId == model.ClassId && a.StartDate == startDate);
                if (exists) return Json(new { success = false, message = "Phân công này đã tồn tại" });

                var assignment = new Assignment
                {
                    EmployeeId = model.EmployeeId,
                    ClassId = model.ClassId,
                    StartDate = startDate,
                    EndDate = string.IsNullOrEmpty(model.EndDate) ? null : DateOnly.Parse(model.EndDate),
                    RoleInClass = model.RoleInClass
                };
                _context.Assignments.Add(assignment);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Phân công thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Api/Assignment")]
        public async Task<IActionResult> UpdateAssignment([FromBody] CreateAssignmentViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                var startDate = DateOnly.Parse(model.StartDate);
                
                var assignment = await _context.Assignments.FirstOrDefaultAsync(a =>
                    a.EmployeeId == model.EmployeeId && a.ClassId == model.ClassId && a.StartDate == startDate);

                if (assignment == null) return Json(new { success = false, message = "Không tìm thấy phân công để cập nhật" });

                assignment.EndDate = string.IsNullOrEmpty(model.EndDate) ? null : DateOnly.Parse(model.EndDate);
                assignment.RoleInClass = model.RoleInClass;

                _context.Assignments.Update(assignment);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật phân công thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Api/Assignment")]
        public async Task<IActionResult> DeleteAssignment(int employeeId, int classId, string startDate)
        {
            var sDate = DateOnly.Parse(startDate);
            var assignment = await _context.Assignments
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.ClassId == classId && a.StartDate == sDate);

            if (assignment == null) return Json(new { success = false, message = "Không tìm thấy phân công" });

            assignment.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn phân công giảng dạy thành công." });
        }

        [HttpPost("Api/Assignment/Reactivate")]
        public async Task<IActionResult> ReactivateAssignment(int employeeId, int classId, string startDate)
        {
            var sDate = DateOnly.Parse(startDate);
            var assignment = await _context.Assignments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.ClassId == classId && a.StartDate == sDate);

            if (assignment == null) return Json(new { success = false, message = "Không tìm thấy." });

            assignment.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục phân công giảng dạy thành công." });
        }

        // ============ STUDENT API ============

        [HttpGet("Api/Students")]
        public async Task<IActionResult> GetStudents(bool showInactive = false)
        {
            try
            {
                var query = _context.Students.AsQueryable();
                if (showInactive)
                {
                    query = query.IgnoreQueryFilters().Where(s => s.Status == StudentStatus.Inactive);
                }

                var students = await query
                    .Include(s => s.Class)
                    .Include(s => s.ParentStudents).ThenInclude(ps => ps.Parent)
                    .OrderBy(s => s.Id)
                    .ToListAsync();

                var result = students.Select(s => new
                {
                    id = s.Id,
                    studentCode = s.StudentCode,
                    fullName = $"{s.FirstName} {s.LastName}".Trim(),
                    gender = s.Gender ? "Nam" : "Nữ",
                    dateOfBirth = s.DateOfBirth.ToString("dd/MM/yyyy"),
                    address = s.Address ?? "N/A",
                    className = s.Class?.Name ?? "Chưa có lớp",
                    enrollDate = s.EnrollDate?.ToString("dd/MM/yyyy") ?? "N/A",
                    status = (int)s.Status,
                    statusText = s.Status == StudentStatus.Active ? "Đang học" : "Đã nghỉ",
                    createdAt = s.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    avatarPath = s.AvatarPath ?? "/images/lion_orange.png",
                    fatherName = s.ParentStudents
                        .Where(ps => ps.Relationship == "Bố")
                        .Select(ps => ps.Parent.LastName + " " + ps.Parent.FirstName)
                        .FirstOrDefault(),
                    motherName = s.ParentStudents
                        .Where(ps => ps.Relationship == "Mẹ")
                        .Select(ps => ps.Parent.LastName + " " + ps.Parent.FirstName)
                        .FirstOrDefault()
                }).ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("Api/Student/{id:int}")]
        public async Task<IActionResult> GetStudent(int id)
        {
            try
            {
                var s = await _context.Students.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
                if (s == null) return Json(new { success = false, message = "Không tìm thấy" });

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = s.Id,
                        firstName = s.FirstName,
                        lastName = s.LastName,
                        gender = s.Gender.ToString().ToLower(),
                        dateOfBirth = s.DateOfBirth.ToString("yyyy-MM-dd"),
                        address = s.Address,
                        classId = s.ClassId,
                        enrollDate = s.EnrollDate?.ToString("yyyy-MM-dd"),
                        status = (int)s.Status,
                        avatarPath = s.AvatarPath
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Api/Student/{id:int}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null) return Json(new { success = false, message = "Không tìm thấy" });

                student.Status = StudentStatus.Inactive;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã chuyển trạng thái học sinh sang 'Đã nghỉ'" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Api/Student/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateStudent(int id)
        {
            try
            {
                var student = await _context.Students.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
                if (student == null) return Json(new { success = false, message = "Không tìm thấy" });

                student.Status = StudentStatus.Active;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã khôi phục học sinh thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Api/Student")]
        public async Task<IActionResult> CreateStudent([FromForm] datn.DTOs.CreateStudentDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu nhập vào không hợp lệ" });
                }

                // 1. Kiểm tra trùng lặp nếu không phải ép buộc tạo mới
                if (!model.ForceCreate)
                {
                    var duplicate = await _studentService.CheckPotentialDuplicateAsync(model);
                    if (duplicate != null)
                    {
                        return StatusCode(409, new { 
                            success = false, 
                            message = $"Có một học sinh tên là {duplicate.FirstName} {duplicate.LastName}, ngày sinh {duplicate.DateOfBirth:dd/MM/yyyy} đã tồn tại trong hệ thống. Bạn có chắc muốn tạo mới không?",
                            existingStudentId = duplicate.Id
                        });
                    }
                }

                // 2. Tạo mới học sinh thông qua Service
                // Kiểm tra độ tuổi hợp lệ chung (2-6 tuổi)
                if (DateOnly.TryParse(model.DateOfBirth, out var dobStudent))
                {
                    var ageStudent = DateTime.Now.Year - dobStudent.Year;
                    if (ageStudent < 2 || ageStudent > 6)
                    {
                        return Json(new { success = false, message = $"Độ tuổi học sinh ({ageStudent} tuổi) không phù hợp để nhập học. Hệ thống chỉ nhận học sinh từ 2 đến 6 tuổi." });
                    }
                }

                // Kiểm tra các ràng buộc Lớp học (Tuổi, Sĩ số, Niên khóa)
                if (model.ClassId > 0)
                {
                    var validationError = await ValidateStudentClassAssignmentAsync(model.ClassId.Value, model.DateOfBirth, null);
                    if (validationError != null) return Json(new { success = false, message = validationError });
                }

                var student = await _studentService.CreateStudentAsync(model);
                
                return Json(new { success = true, message = "Thêm học sinh thành công", studentCode = student.StudentCode });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPut("Api/Student/{id:int}")]
        public async Task<IActionResult> UpdateStudent(int id, [FromForm] UpdateStudentViewModel model)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null) return Json(new { success = false, message = "Không tìm thấy" });

                var dob = string.IsNullOrEmpty(model.DateOfBirth) ? student.DateOfBirth : DateOnly.Parse(model.DateOfBirth);

                // Kiểm tra độ tuổi hợp lệ chung (2-6 tuổi)
                var ageStudentUpdate = DateTime.Now.Year - dob.Year;
                if (ageStudentUpdate < 2 || ageStudentUpdate > 6)
                {
                    return Json(new { success = false, message = $"Độ tuổi học sinh ({ageStudentUpdate} tuổi) không phù hợp. Hệ thống chỉ nhận học sinh từ 2 đến 6 tuổi." });
                }

                // Kiểm tra trùng lặp (loại trừ chính bản thân học sinh đang cập nhật)
                var isDuplicate = await _context.Students.AnyAsync(s => 
                    s.Id != id &&
                    s.FirstName == model.FirstName && 
                    s.LastName == model.LastName && 
                    s.DateOfBirth == dob);

                if (isDuplicate)
                {
                    return Json(new { success = false, message = "Thông tin cập nhật trùng với một học sinh khác đã tồn tại." });
                }

                // Kiểm tra các ràng buộc Lớp học nếu có thay đổi lớp
                if (model.ClassId > 0 && model.ClassId != student.ClassId)
                {
                    var validationError = await ValidateStudentClassAssignmentAsync(model.ClassId.Value, dob.ToString("yyyy-MM-dd"), id);
                    if (validationError != null) return Json(new { success = false, message = validationError });
                }

                student.FirstName = model.FirstName;
                student.LastName = model.LastName;
                student.Gender = model.Gender == "true";
                student.DateOfBirth = dob;
                student.Address = model.Address;
                student.ClassId = model.ClassId > 0 ? model.ClassId : null;
                student.Status = (StudentStatus)model.Status;
                student.EnrollDate = string.IsNullOrEmpty(model.EnrollDate) ? student.EnrollDate : DateOnly.Parse(model.EnrollDate);

                if (model.Avatar != null) student.AvatarPath = await SaveAvatar(model.Avatar, "student");

                _context.Students.Update(student);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<string?> ValidateStudentClassAssignmentAsync(int classId, string dobStr, int? studentId)
        {
            var classroom = await _context.Classes.Include(c => c.Students).FirstOrDefaultAsync(c => c.Id == classId);
            if (classroom == null) return "Không tìm thấy lớp học.";

            // 1. Kiểm tra Sĩ số
            var currentCount = classroom.Students.Count(s => s.Status == StudentStatus.Active);
            if (currentCount >= classroom.MaxCapacity && studentId == null) 
                return $"Lớp {classroom.Name} đã đủ sĩ số ({classroom.MaxCapacity}).";

            // 2. Kiểm tra Độ tuổi
            if (DateOnly.TryParse(dobStr, out var dob))
            {
                var yearNow = DateTime.Now.Year;
                var age = yearNow - dob.Year;
                
                if (classroom.AgeFrom.HasValue && age < classroom.AgeFrom.Value)
                    return $"Học sinh ({age} tuổi) nhỏ hơn độ tuổi quy định của lớp ({classroom.AgeFrom.Value}-{classroom.AgeTo} tuổi).";
                
                if (classroom.AgeTo.HasValue && age > classroom.AgeTo.Value)
                    return $"Học sinh ({age} tuổi) lớn hơn độ tuổi quy định của lớp ({classroom.AgeFrom}-{classroom.AgeTo.Value} tuổi).";
            }

            // 3. Kiểm tra Niên khóa (Chỉ cho phép thêm vào niên khóa hiện tại)
            var currentYear = DateTime.Now.Year;
            var nextYear = currentYear + 1;
            var currentSchoolYear = $"{currentYear}-{nextYear}";
            
            // Nếu niên khóa lớp không phải năm nay và không phải năm sau (đang tuyển sinh)
            if (!string.IsNullOrEmpty(classroom.SchoolYear) && !classroom.SchoolYear.Contains(currentYear.ToString()) && !classroom.SchoolYear.Contains(nextYear.ToString()))
            {
                // Cho phép sửa nếu là học sinh cũ đang ở trong lớp đó, nhưng không cho thêm mới
                if (studentId == null)
                    return $"Không thể thêm học sinh vào niên khóa cũ ({classroom.SchoolYear}).";
            }

            return null;
        }

        // ============ TEACHER API ============

        [HttpGet("Api/Teachers")]
        public async Task<IActionResult> GetTeachers(bool showInactive = false)
        {
            try
            {
                var query = _context.Employees.AsQueryable();
                
                if (showInactive)
                {
                    // Vượt rào để lấy các giáo viên đã nghỉ/khóa
                    query = query.IgnoreQueryFilters().Where(e => !e.IsActive);
                }

                var teachers = await query
                    .Include(e => e.Account)
                    .ThenInclude(a => a.Role)
                    .Where(e => e.Account.Role.Name == "Employee")
                    .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
                    .ToListAsync();

                var result = teachers.Select(t => new
                {
                    id = t.Id,
                    fullName = t.FullName,
                    phone = t.Phone,
                    position = t.Position ?? "Giáo viên",
                    teacherType = t.TeacherType,
                    baseSalary = t.BaseSalary,
                    avatarPath = t.AvatarPath ?? "/images/lion_blue.png",
                    isActive = t.IsActive, // Now using Employee.IsActive
                    gender = t.Gender
                }).ToList();
                
                var total = result.Count;
                return Json(new { success = true, data = result, total });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // PARENT MANAGEMENT
        // ==========================================

        [HttpGet("Parents")]
        public IActionResult Parents()
        {
            ViewData["Title"] = "Phụ huynh";
            return View();
        }

        [HttpGet("Api/Parents")]
        public async Task<IActionResult> GetParents(string search = "", int page = 1, int pageSize = 10, bool showInactive = false)
        {
            try
            {
                var query = _context.Parents.AsQueryable();
                
                if (showInactive)
                {
                    query = query.IgnoreQueryFilters().Where(p => !p.IsActive);
                }

                query = query
                    .Include(p => p.Account)
                    .Include(p => p.ParentStudents).ThenInclude(ps => ps.Student);

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(p => 
                        p.FirstName.ToLower().Contains(search) || 
                        p.LastName.ToLower().Contains(search) || 
                        (p.Phone != null && p.Phone.Contains(search)));
                }

                var total = await query.CountAsync();
                var data = await query
                    .OrderByDescending(p => p.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = data.Select(p => new {
                        id = p.Id,
                        email = p.Account?.Email ?? "N/A",
                        fullName = p.LastName + " " + p.FirstName,
                        gender = p.Gender,
                        phone = p.Phone ?? "N/A",
                        address = p.Address ?? "N/A",
                        avatarPath = p.AvatarPath ?? "/images/lion_orange.png",
                        isActive = p.IsActive,
                        createdAt = p.Account?.CreatedAt ?? DateTime.MinValue,
                        childrenCount = p.ParentStudents.Count,
                        children = p.ParentStudents.Select(ps => new {
                            id = ps.StudentId,
                            fullName = ps.Student.LastName + " " + ps.Student.FirstName
                        })
                    });

                return Json(new { success = true, data = result, total });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("ParentDetail/{id:int}")]
        public async Task<IActionResult> ParentDetail(int id)
        {
            var parent = await _context.Parents
                .Include(p => p.Account)
                .Include(p => p.ParentStudents).ThenInclude(ps => ps.Student).ThenInclude(s => s.Class)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (parent == null) return NotFound();

            ViewData["Title"] = "Chi tiết Phụ huynh";
            return View(parent);
        }

        [HttpGet("Api/Parent/{id:int}")]
        public async Task<IActionResult> GetParent(int id)
        {
            try
            {
                var p = await _context.Parents
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (p != null)
                {
                    p.Account = await _context.Accounts.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(a => a.Id == p.AccountId);
                    p.ParentStudents = await _context.ParentStudents
                        .Include(ps => ps.Student)
                        .Where(ps => ps.ParentId == p.Id)
                        .ToListAsync();
                }

                if (p == null) return Json(new { success = false, message = "Không tìm thấy" });

                var result = new {
                    id = p.Id,
                    username = p.Account.Username,
                    email = p.Account.Email,
                    firstName = p.FirstName,
                    lastName = p.LastName,
                    gender = p.Gender,
                    phone = p.Phone,
                    address = p.Address,
                    avatarPath = p.AvatarPath ?? "/images/lion_orange.png",
                    isActive = p.IsActive,
                    createdAt = p.Account.CreatedAt.ToString("dd/MM/yyyy"),
                    children = p.ParentStudents.Select(ps => new {
                        id = ps.StudentId,
                        fullName = ps.Student.LastName + " " + ps.Student.FirstName,
                        relationship = ps.Relationship
                    }).ToList()
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Api/Parent")]
        public async Task<IActionResult> CreateParent([FromForm] datn.DTOs.CreateParentDto model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            try
            {
                if (await _parentService.IsEmailOrUsernameExists(model.Email, model.Username))
                {
                    return Json(new { success = false, message = "Email hoặc Tên đăng nhập đã tồn tại" });
                }

                await _parentService.CreateParentAsync(model);
                return Json(new { success = true, message = "Tạo tài khoản phụ huynh thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Api/Parent/{id:int}")]
        public async Task<IActionResult> UpdateParent(int id, [FromForm] datn.DTOs.CreateParentDto model)
        {
            // Khi update, chúng ta không cho phép Manager sửa mật khẩu ở đây
            // nên xóa lỗi validation của Password nếu có
            ModelState.Remove("Password");

            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            try
            {
                if (await _parentService.IsEmailOrUsernameExists(model.Email, model.Username, id))
                {
                    return Json(new { success = false, message = "Email hoặc Tên đăng nhập đã tồn tại" });
                }

                var parent = await _parentService.UpdateParentAsync(id, model);
                if (parent == null) return Json(new { success = false, message = "Không tìm thấy" });

                return Json(new { success = true, message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Api/Parent/{id:int}")]
        public async Task<IActionResult> DeleteParent(int id)
        {
            var success = await _parentService.DeleteParentAsync(id);
            return Json(new { success, message = success ? "Vô hiệu hóa thành công" : "Lỗi vô hiệu hóa" });
        }

        [HttpPost("Api/Parent/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateParent(int id)
        {
            var parent = await _context.Parents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (parent != null)
            {
                parent.Account = await _context.Accounts.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(a => a.Id == parent.AccountId);
            }
            if (parent == null) return Json(new { success = false, message = "Không tìm thấy phụ huynh" });

            parent.IsActive = true;
            if (parent.Account != null)
            {
                parent.Account.IsActive = true;
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Kích hoạt tài khoản thành công" });
        }

        [HttpPost("Api/Parent/LinkStudent")]
        public async Task<IActionResult> LinkStudent([FromForm] int parentId, [FromForm] int studentId, [FromForm] string? relationship)
        {
            if (parentId <= 0 || studentId <= 0)
            {
                return Json(new { success = false, message = $"Du lieu khong hop le (parentId={parentId}, studentId={studentId})." });
            }

            if (!await _context.Parents.AnyAsync(p => p.Id == parentId))
            {
                return Json(new { success = false, message = "Khong tim thay phu huynh." });
            }

            if (!await _context.Students.AnyAsync(s => s.Id == studentId))
            {
                return Json(new { success = false, message = "Khong tim thay hoc sinh." });
            }

            try
            {
                var success = await _parentService.LinkStudentAsync(parentId, studentId, relationship ?? "");
            return Json(new { success, message = success ? "Liên kết thành công" : "Lỗi khi liên kết" });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = $"Loi CSDL khi lien ket: {ex.GetBaseException().Message}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Loi khi lien ket: {ex.Message}" });
            }
        }

        [HttpDelete("Api/Parent/UnlinkStudent")]
        public async Task<IActionResult> UnlinkStudent(int parentId, int studentId)
        {
            var success = await _parentService.UnlinkStudentAsync(parentId, studentId);
            return Json(new { success, message = success ? "Hủy liên kết thành công" : "Lỗi khi hủy liên kết" });
        }

        [HttpGet("Api/Students/Search")]
        public async Task<IActionResult> SearchStudents(string q)
        {
            var students = await _context.Students
                .Where(s => s.Status == StudentStatus.Active &&  
                    (s.FirstName.Contains(q) || s.LastName.Contains(q) || s.StudentCode.Contains(q)))
                .Take(10)
                .Select(s => new { id = s.Id, fullName = s.LastName + " " + s.FirstName, code = s.StudentCode })
                .ToListAsync();
            return Json(new { success = true, data = students });
        }

        [HttpGet("Api/Teacher/{id:int}")]
        public async Task<IActionResult> GetTeacher(int id)
        {
            var teacher = await _context.Employees
                .IgnoreQueryFilters()
                .Include(e => e.Account)
                .FirstOrDefaultAsync(e => e.Id == id);
                
            if (teacher == null)
                return Json(new { success = false, message = "Không tìm thấy giáo viên." });

            return Json(new
            {
                success = true,
                data = new
                {
                    id = teacher.Id,
                    firstName = teacher.FirstName,
                    lastName = teacher.LastName,
                    fullName = teacher.FullName,
                    email = teacher.Account?.Email,
                    username = teacher.Account?.Username,
                    gender = teacher.Gender,
                    phone = teacher.Phone,
                    position = teacher.Position,
                    teacherType = teacher.TeacherType,
                    baseSalary = teacher.BaseSalary,
                    avatarPath = teacher.AvatarPath,
                    isActive = teacher.Account?.IsActive ?? true,
                    bio = teacher.Bio,
                    qualifications = teacher.Qualifications,
                    experience = teacher.Experience,
                    philosophy = teacher.Philosophy,
                    specialty = teacher.Specialty,
                    showOnLanding = teacher.ShowOnLanding
                }
            });
        }

        [HttpGet("TeacherDetail/{id:int}")]
        public async Task<IActionResult> TeacherDetail(int id)
        {
            var teacher = await _context.Employees
                .Include(e => e.Account)
                .Include(e => e.Assignments).ThenInclude(a => a.Class)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (teacher == null) return NotFound();

            ViewData["Title"] = "Chi tiết Giáo viên";
            return View(teacher);
        }

        [HttpPost("Api/Teacher")]
        public async Task<IActionResult> CreateTeacher([FromForm] CreateTeacherViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }
            if (await _context.Accounts.AnyAsync(a => a.Username == model.Username))
                return Json(new { success = false, message = "Tên đăng nhập đã tồn tại." });

            if (await _context.Accounts.AnyAsync(a => a.Email == model.Email))
                return Json(new { success = false, message = "Email đã tồn tại." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var role = await _context.Roles.FirstAsync(r => r.Name == "Employee");
                
                var account = new Account
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password.Trim()),
                    PasswordSalt = "",
                    RoleId = role.Id
                };
                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();

                var teacher = new Employee
                {
                    AccountId = account.Id,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Gender = model.Gender,
                    Phone = model.Phone,
                    Position = model.Position,
                    TeacherType = model.TeacherType,
                    BaseSalary = model.BaseSalary,
                    Bio = model.Bio,
                    Qualifications = model.Qualifications,
                    Experience = model.Experience,
                    Philosophy = model.Philosophy,
                    Specialty = model.Specialty,
                    ShowOnLanding = model.ShowOnLanding
                };
                if (model.Avatar != null) teacher.AvatarPath = await SaveAvatar(model.Avatar, "teacher");

                _context.Employees.Add(teacher);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Json(new { success = true, message = "Thêm giáo viên thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Api/Teacher/{id:int}")]
        public async Task<IActionResult> UpdateTeacher(int id, [FromForm] UpdateTeacherViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var teacher = await _context.Employees.Include(e => e.Account).FirstOrDefaultAsync(e => e.Id == id);
                if (teacher == null)
                    return Json(new { success = false, message = "Không tìm thấy giáo viên." });

                // Kiểm tra email trùng (loại trừ chính mình)
                if (await _context.Accounts.AnyAsync(a => a.Email == model.Email && a.Id != teacher.AccountId))
                    return Json(new { success = false, message = "Email này đã được sử dụng bởi tài khoản khác." });

                teacher.FirstName = model.FirstName;
                teacher.LastName = model.LastName;
                teacher.Gender = model.Gender;
                teacher.Phone = model.Phone;
                teacher.Position = model.Position;
                teacher.TeacherType = model.TeacherType;
                teacher.BaseSalary = model.BaseSalary;
                teacher.Bio = model.Bio;
                teacher.Qualifications = model.Qualifications;
                teacher.Experience = model.Experience;
                teacher.Philosophy = model.Philosophy;
                teacher.Specialty = model.Specialty;
                teacher.ShowOnLanding = model.ShowOnLanding;
                
                if (teacher.Account != null)
                {
                    teacher.Account.Email = model.Email;
                }

                if (model.Avatar != null) teacher.AvatarPath = await SaveAvatar(model.Avatar, "teacher");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Json(new { success = true, message = "Cập nhật giáo viên thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Api/Teacher/{id:int}")]
        public async Task<IActionResult> DeactivateTeacher(int id)
        {
            try
            {
                var teacher = await _context.Employees.Include(e => e.Account).FirstOrDefaultAsync(e => e.Id == id);
                if (teacher == null)
                    return Json(new { success = false, message = "Không tìm thấy giáo viên." });

                // Vô hiệu hóa ở cả 2 cấp độ: Employee và Account
                teacher.IsActive = false;
                if (teacher.Account != null)
                {
                    teacher.Account.IsActive = false;
                    
                    // Thu hồi toàn bộ Refresh Token để đẩy giáo viên ra khỏi hệ thống
                    var activeTokens = await _context.RefreshTokens
                        .Where(r => r.AccountId == teacher.AccountId && !r.IsRevoked)
                        .ToListAsync();
                    
                    foreach (var token in activeTokens)
                    {
                        token.IsRevoked = true;
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã vô hiệu hóa giáo viên thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Api/Teacher/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateTeacher(int id)
        {
            try
            {
                // Sử dụng IgnoreQueryFilters để tìm bản ghi đang bị ẩn
                var teacher = await _context.Employees.IgnoreQueryFilters()
                    .Include(e => e.Account)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (teacher == null)
                    return Json(new { success = false, message = "Không tìm thấy giáo viên." });

                teacher.IsActive = true;
                if (teacher.Account != null)
                {
                    teacher.Account.IsActive = true;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã khôi phục giáo viên thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        // ============ CLASS MANAGEMENT API ============

        [HttpGet("Api/Classes")]
        public async Task<IActionResult> GetClasses(bool showInactive = false)
        {
            var query = _context.Classes.AsQueryable();
            if (showInactive)
            {
                query = query.IgnoreQueryFilters().Where(c => !c.IsActive);
            }

            var classes = await query
                .OrderBy(c => c.Name)
                .ToListAsync();
            return Json(new { success = true, data = classes.Select(c => new { 
                id = c.Id, 
                name = c.Name,
                ageFrom = c.AgeFrom,
                ageTo = c.AgeTo
            }) });
        }

        [HttpGet("Api/Classes/Overview")]
        public async Task<IActionResult> GetClassesOverview(bool showInactive = false)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            IQueryable<Class> query;
            if (showInactive)
            {
                query = _context.Classes.IgnoreQueryFilters().Where(c => !c.IsActive);
            }
            else
            {
                query = _context.Classes;
            }

            var classes = await query
                .Include(c => c.Students)
                .Include(c => c.Assignments)
                    .ThenInclude(a => a.Employee)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var data = classes.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                ageFrom = c.AgeFrom,
                ageTo = c.AgeTo,
                schoolYear = c.SchoolYear,
                maxCapacity = c.MaxCapacity,
                isActive = c.IsActive,
                studentCount = c.Students.Count,
                teachers = c.Assignments
                    .Where(a => a.StartDate <= today && (a.EndDate == null || a.EndDate >= today))
                    .Select(a => new
                    {
                        employeeId = a.EmployeeId,
                        teacherName = a.Employee.LastName + " " + a.Employee.FirstName,
                        roleInClass = a.RoleInClass ?? "Giáo viên"
                    })
                    .ToList()
            });

            return Json(new { success = true, data });
        }

        [HttpGet("Api/Class/{id:int}")]
        public async Task<IActionResult> GetClass(int id)
        {
            var classroom = await _context.Classes
                .Include(c => c.Assignments)
                .ThenInclude(a => a.Employee)
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (classroom == null)
                return Json(new { success = false, message = "Không tìm thấy lớp học." });

            var today = DateOnly.FromDateTime(DateTime.Now);

            return Json(new
            {
                success = true,
                data = new
                {
                    id = classroom.Id,
                    name = classroom.Name,
                    ageFrom = classroom.AgeFrom,
                    ageTo = classroom.AgeTo,
                    schoolYear = classroom.SchoolYear,
                    studentCount = classroom.Students.Count,
                    teachers = classroom.Assignments
                        .Where(a => a.StartDate <= today && (a.EndDate == null || a.EndDate >= today))
                        .Select(a => new
                        {
                            employeeId = a.EmployeeId,
                            teacherName = a.Employee.LastName + " " + a.Employee.FirstName,
                            roleInClass = a.RoleInClass ?? "Giáo viên"
                        })
                        .ToList()
                }
            });
        }

        [HttpPost("Api/Class")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass([FromBody] SaveClassViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Tên lớp không được để trống." });

            // Validate age range
            var validAgeRanges = new List<(int?, int?)> { (2, 3), (3, 4), (4, 5), (5, 6) };
            if (!validAgeRanges.Any(r => r.Item1 == model.AgeFrom && r.Item2 == model.AgeTo))
            {
                return Json(new { success = false, message = "Độ tuổi không hợp lệ. Vui lòng chọn trong danh sách cho phép." });
            }

            var duplicate = await _context.Classes.AnyAsync(c => c.Name == model.Name.Trim() && c.SchoolYear == model.SchoolYear);
            if (duplicate)
                return Json(new { success = false, message = "Đã tồn tại lớp cùng tên trong niên khóa này." });

            var classroom = new Class
            {
                Name = model.Name.Trim(),
                AgeFrom = model.AgeFrom,
                AgeTo = model.AgeTo,
                SchoolYear = model.SchoolYear?.Trim(),
                MaxCapacity = model.MaxCapacity > 0 ? model.MaxCapacity : 25,
                IsActive = true
            };

            _context.Classes.Add(classroom);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Tạo lớp học thành công." });
        }

        [HttpPut("Api/Class/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClass(int id, [FromBody] SaveClassViewModel model)
        {
            var classroom = await _context.Classes.FindAsync(id);
            if (classroom == null)
                return Json(new { success = false, message = "Không tìm thấy lớp học." });

            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Tên lớp không được để trống." });

            // Validate age range
            var validAgeRanges = new List<(int?, int?)> { (2, 3), (3, 4), (4, 5), (5, 6) };
            if (!validAgeRanges.Any(r => r.Item1 == model.AgeFrom && r.Item2 == model.AgeTo))
            {
                return Json(new { success = false, message = "Độ tuổi không hợp lệ. Vui lòng chọn trong danh sách cho phép." });
            }

            var duplicate = await _context.Classes.AnyAsync(c =>
                c.Id != id && c.Name == model.Name.Trim() && c.SchoolYear == model.SchoolYear);
            if (duplicate)
                return Json(new { success = false, message = "Đã tồn tại lớp cùng tên trong niên khóa này." });

            classroom.Name = model.Name.Trim();
            classroom.AgeFrom = model.AgeFrom;
            classroom.AgeTo = model.AgeTo;
            classroom.SchoolYear = model.SchoolYear?.Trim();
            classroom.MaxCapacity = model.MaxCapacity > 0 ? model.MaxCapacity : 25;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cập nhật lớp học thành công." });
        }

        [HttpDelete("Api/Class/{id:int}")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var classroom = await _context.Classes.FindAsync(id);
            if (classroom == null)
                return Json(new { success = false, message = "Không tìm thấy lớp học." });

            classroom.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn lớp học thành công." });
        }

        [HttpPost("Api/Class/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateClass(int id)
        {
            var classroom = await _context.Classes.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
            if (classroom == null) return Json(new { success = false, message = "Không tìm thấy lớp học." });

            classroom.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục lớp học thành công." });
        }

        // ============ SUBJECT API ============

        [HttpGet("Api/Subjects")]
        public async Task<IActionResult> GetSubjects(bool showInactive = false)
        {
            IQueryable<Subject> query;
            if (showInactive)
            {
                query = _context.Subjects.IgnoreQueryFilters().Where(s => !s.IsActive);
            }
            else
            {
                query = _context.Subjects;
            }

            var subjects = await query
                .OrderBy(s => s.Name)
                .ToListAsync();

            return Json(new
            {
                success = true,
                data = subjects.Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    code = s.Code,
                    description = s.Description,
                    isActive = s.IsActive
                })
            });
        }

        [HttpGet("Api/Subject/{id:int}")]
        public async Task<IActionResult> GetSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return Json(new { success = false, message = "Không tìm thấy môn học." });

            return Json(new
            {
                success = true,
                data = new
                {
                    id = subject.Id,
                    name = subject.Name,
                    code = subject.Code,
                    description = subject.Description,
                    isActive = subject.IsActive
                }
            });
        }

        [HttpPost("Api/Subject")]
        public async Task<IActionResult> CreateSubject([FromBody] SaveSubjectViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Code))
                return Json(new { success = false, message = "Tên môn và mã môn là bắt buộc." });

            var normalizedCode = model.Code.Trim().ToUpperInvariant();
            var duplicate = await _context.Subjects.AnyAsync(s => s.Code == normalizedCode);
            if (duplicate)
                return Json(new { success = false, message = "Mã môn đã tồn tại." });

            _context.Subjects.Add(new Subject
            {
                Name = model.Name.Trim(),
                Code = normalizedCode,
                Description = model.Description?.Trim(),
                IsActive = model.IsActive
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Tạo môn học thành công." });
        }

        [HttpPut("Api/Subject/{id:int}")]
        public async Task<IActionResult> UpdateSubject(int id, [FromBody] SaveSubjectViewModel model)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return Json(new { success = false, message = "Không tìm thấy môn học." });

            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Code))
                return Json(new { success = false, message = "Tên môn và mã môn là bắt buộc." });

            var normalizedCode = model.Code.Trim().ToUpperInvariant();
            var duplicate = await _context.Subjects.AnyAsync(s => s.Id != id && s.Code == normalizedCode);
            if (duplicate)
                return Json(new { success = false, message = "Mã môn đã tồn tại." });

            subject.Name = model.Name.Trim();
            subject.Code = normalizedCode;
            subject.Description = model.Description?.Trim();
            subject.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật môn học thành công." });
        }

        [HttpDelete("Api/Subject/{id:int}")]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return Json(new { success = false, message = "Không tìm thấy môn học." });

            subject.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn môn học thành công." });
        }

        [HttpPost("Api/Subject/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateSubject(int id)
        {
            var subject = await _context.Subjects.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
            if (subject == null) return Json(new { success = false, message = "Không tìm thấy." });

            subject.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục môn học thành công." });
        }

        // ============ CLASS SCHEDULE API ============

        [HttpGet("Api/ClassSchedules")]
        public async Task<IActionResult> GetClassSchedules(int classId)
        {
            var schedules = await _context.ClassSchedules
                .Where(cs => cs.ClassId == classId)
                .Include(cs => cs.Subject)
                .Include(cs => cs.Employee)
                .OrderBy(cs => cs.DayOfWeek)
                .ThenBy(cs => cs.StartTime)
                .ToListAsync();

            return Json(new
            {
                success = true,
                data = schedules.Select(cs => new
                {
                    id = cs.Id,
                    classId = cs.ClassId,
                    subjectId = cs.SubjectId,
                    subjectName = cs.Subject.Name,
                    employeeId = cs.EmployeeId,
                    teacherName = cs.Employee.FullName,
                    dayOfWeek = cs.DayOfWeek,
                    dayLabel = GetVietnameseDayLabel(cs.DayOfWeek),
                    startTime = cs.StartTime.ToString("HH:mm"),
                    endTime = cs.EndTime.ToString("HH:mm"),
                    locationId = cs.LocationId,
                    locationName = cs.Location?.Name,
                    effectiveFrom = cs.EffectiveFrom.ToString("yyyy-MM-dd"),
                    effectiveTo = cs.EffectiveTo?.ToString("yyyy-MM-dd"),
                    note = cs.Note,
                    isActive = cs.IsActive
                })
            });
        }

        [HttpGet("Api/ClassSchedule/{id:int}")]
        public async Task<IActionResult> GetClassSchedule(int id)
        {
            var schedule = await _context.ClassSchedules.FindAsync(id);
            if (schedule == null)
                return Json(new { success = false, message = "Không tìm thấy thời khóa biểu." });

            return Json(new
            {
                success = true,
                data = new
                {
                    id = schedule.Id,
                    classId = schedule.ClassId,
                    subjectId = schedule.SubjectId,
                    employeeId = schedule.EmployeeId,
                    dayOfWeek = schedule.DayOfWeek,
                    startTime = schedule.StartTime.ToString("HH:mm"),
                    endTime = schedule.EndTime.ToString("HH:mm"),
                    locationId = schedule.LocationId,
                    effectiveFrom = schedule.EffectiveFrom.ToString("yyyy-MM-dd"),
                    effectiveTo = schedule.EffectiveTo?.ToString("yyyy-MM-dd"),
                    note = schedule.Note,
                    isActive = schedule.IsActive
                }
            });
        }

        [HttpPost("Api/ClassSchedule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClassSchedule([FromBody] SaveClassScheduleViewModel model)
        {
            var validationMessage = await ValidateScheduleRequestAsync(model, null);
            if (validationMessage != null)
                return Json(new { success = false, message = validationMessage });

            var schedule = new ClassSchedule
            {
                ClassId = model.ClassId,
                SubjectId = model.SubjectId,
                EmployeeId = model.EmployeeId,
                DayOfWeek = model.DayOfWeek,
                StartTime = TimeOnly.Parse(model.StartTime),
                EndTime = TimeOnly.Parse(model.EndTime),
                LocationId = model.LocationId,
                EffectiveFrom = DateOnly.Parse(model.EffectiveFrom),
                EffectiveTo = string.IsNullOrWhiteSpace(model.EffectiveTo) ? null : DateOnly.Parse(model.EffectiveTo),
                Note = model.Note?.Trim(),
                IsActive = model.IsActive
            };

            _context.ClassSchedules.Add(schedule);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Tạo thời khóa biểu thành công." });
        }

        [HttpPut("Api/ClassSchedule/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClassSchedule(int id, [FromBody] SaveClassScheduleViewModel model)
        {
            var schedule = await _context.ClassSchedules.FindAsync(id);
            if (schedule == null)
                return Json(new { success = false, message = "Không tìm thấy thời khóa biểu." });

            var validationMessage = await ValidateScheduleRequestAsync(model, id);
            if (validationMessage != null)
                return Json(new { success = false, message = validationMessage });

            schedule.ClassId = model.ClassId;
            schedule.SubjectId = model.SubjectId;
            schedule.EmployeeId = model.EmployeeId;
            schedule.DayOfWeek = model.DayOfWeek;
            schedule.StartTime = TimeOnly.Parse(model.StartTime);
            schedule.EndTime = TimeOnly.Parse(model.EndTime);
            schedule.LocationId = model.LocationId;
            schedule.EffectiveFrom = DateOnly.Parse(model.EffectiveFrom);
            schedule.EffectiveTo = string.IsNullOrWhiteSpace(model.EffectiveTo) ? null : DateOnly.Parse(model.EffectiveTo);
            schedule.Note = model.Note?.Trim();
            schedule.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thời khóa biểu thành công." });
        }

        [HttpDelete("Api/ClassSchedule/{id:int}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var schedule = await _context.ClassSchedules.FindAsync(id);
            if (schedule == null) return Json(new { success = false, message = "Không tìm thấy lịch học" });

            schedule.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn lịch học thành công." });
        }

        [HttpPost("Api/ClassSchedule/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateSchedule(int id)
        {
            var schedule = await _context.ClassSchedules.IgnoreQueryFilters().FirstOrDefaultAsync(cs => cs.Id == id);
            if (schedule == null) return Json(new { success = false, message = "Không tìm thấy." });

            schedule.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục lịch học thành công." });
        }

        private async Task<string?> ValidateScheduleRequestAsync(SaveClassScheduleViewModel model, int? scheduleId)
        {
            if (!await _context.Classes.AnyAsync(c => c.Id == model.ClassId))
                return "Lớp học không tồn tại.";

            if (!await _context.Subjects.AnyAsync(s => s.Id == model.SubjectId && s.IsActive))
                return "Môn học không tồn tại hoặc đã ngừng sử dụng.";

            if (!await _context.Employees.AnyAsync(e => e.Id == model.EmployeeId))
                return "Giáo viên không tồn tại.";

            if (model.DayOfWeek < 1 || model.DayOfWeek > 5)
                return "Chỉ được tạo lịch từ Thứ 2 đến Thứ 6.";

            if (!TimeOnly.TryParse(model.StartTime, out var startTime) || !TimeOnly.TryParse(model.EndTime, out var endTime))
                return "Khung giờ không hợp lệ.";

            if (!DateOnly.TryParse(model.EffectiveFrom, out var effectiveFrom))
                return "Ngày hiệu lực bắt đầu không hợp lệ.";

            DateOnly? effectiveTo = null;
            if (!string.IsNullOrWhiteSpace(model.EffectiveTo))
            {
                if (!DateOnly.TryParse(model.EffectiveTo, out var parsedEffectiveTo))
                    return "Ngày hiệu lực kết thúc không hợp lệ.";
                effectiveTo = parsedEffectiveTo;
            }

            if (endTime <= startTime)
                return "Giờ kết thúc phải lớn hơn giờ bắt đầu.";

            if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
                return "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.";

            if (startTime < SchoolStart || endTime > SchoolEnd)
                return "Chỉ được xếp lịch trong khung 07:00 - 16:30.";

            if (startTime < LunchEnd && endTime > LunchStart)
                return "Thời khóa biểu không được chồng lên khung nghỉ trưa 11:00 - 13:00.";

            var activeAssignment = await _context.Assignments.AnyAsync(a =>
                a.EmployeeId == model.EmployeeId
                && a.ClassId == model.ClassId
                && a.StartDate <= (effectiveTo ?? DateOnly.MaxValue)
                && (a.EndDate == null || a.EndDate >= effectiveFrom));

            if (!activeAssignment)
                return "Giáo viên phải được phân công cho lớp trong khoảng thời gian của lịch dạy.";

            var sameDaySchedules = await _context.ClassSchedules
                .Where(cs => cs.DayOfWeek == model.DayOfWeek
                    && cs.Id != (scheduleId ?? 0)
                    && cs.IsActive)
                .ToListAsync();

            var classOverlapSchedule = sameDaySchedules.FirstOrDefault(cs =>
                cs.ClassId == model.ClassId
                && DateRangesOverlap(cs.EffectiveFrom, cs.EffectiveTo, effectiveFrom, effectiveTo)
                && TimeRangesOverlap(cs.StartTime, cs.EndTime, startTime, endTime));

            if (classOverlapSchedule != null)
            {
                var subject = await _context.Subjects.FindAsync(classOverlapSchedule.SubjectId);
                return $"Lớp học đã có tiết '{subject?.Name}' trùng khung giờ này ({classOverlapSchedule.StartTime:HH:mm} - {classOverlapSchedule.EndTime:HH:mm}).";
            }

            var teacherOverlapSchedule = sameDaySchedules.FirstOrDefault(cs =>
                cs.EmployeeId == model.EmployeeId
                && DateRangesOverlap(cs.EffectiveFrom, cs.EffectiveTo, effectiveFrom, effectiveTo)
                && TimeRangesOverlap(cs.StartTime, cs.EndTime, startTime, endTime));

            if (teacherOverlapSchedule != null)
            {
                return $"Giáo viên đã có lịch dạy khác trùng khung giờ này ({teacherOverlapSchedule.StartTime:HH:mm} - {teacherOverlapSchedule.EndTime:HH:mm}).";
            }

            if (model.LocationId.HasValue)
            {
                var locationOverlap = sameDaySchedules.Any(cs =>
                    cs.LocationId == model.LocationId
                    && DateRangesOverlap(cs.EffectiveFrom, cs.EffectiveTo, effectiveFrom, effectiveTo)
                    && TimeRangesOverlap(cs.StartTime, cs.EndTime, startTime, endTime));

                if (locationOverlap)
                    return "Phòng học/Địa điểm này đã được sử dụng cho lớp khác trong khung giờ này.";
            }

            return null;
        }

        private static bool DateRangesOverlap(DateOnly leftStart, DateOnly? leftEnd, DateOnly rightStart, DateOnly? rightEnd)
        {
            var normalizedLeftEnd = leftEnd ?? DateOnly.MaxValue;
            var normalizedRightEnd = rightEnd ?? DateOnly.MaxValue;
            return leftStart <= normalizedRightEnd && rightStart <= normalizedLeftEnd;
        }

        private static bool TimeRangesOverlap(TimeOnly leftStart, TimeOnly leftEnd, TimeOnly rightStart, TimeOnly rightEnd)
        {
            return leftStart < rightEnd && rightStart < leftEnd;
        }

        private static string GetVietnameseDayLabel(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "Thứ 2",
                2 => "Thứ 3",
                3 => "Thứ 4",
                4 => "Thứ 5",
                5 => "Thứ 6",
                _ => "Không xác định"
            };
        }

        // ============ LOCATION API ============

        [HttpGet("Api/Locations")]
        public async Task<IActionResult> GetLocations(bool showInactive = false)
        {
            var query = _context.Locations.AsQueryable();
            if (!showInactive) query = query.Where(l => l.IsActive);
            
            var locations = await query.OrderBy(l => l.Name).ToListAsync();
            return Json(new { success = true, data = locations });
        }

        [HttpPost("Api/Location")]
        public async Task<IActionResult> CreateLocation([FromBody] Location model)
        {
            if (string.IsNullOrWhiteSpace(model.Name)) return Json(new { success = false, message = "Tên địa điểm không được để trống" });
            model.IsActive = true;
            _context.Locations.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã thêm địa điểm" });
        }

        [HttpDelete("Api/Location/{id:int}")]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return Json(new { success = false, message = "Không tìm thấy." });

            location.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn địa điểm thành công." });
        }

        [HttpPost("Api/Location/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateLocation(int id)
        {
            var location = await _context.Locations.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == id);
            if (location == null) return Json(new { success = false, message = "Không tìm thấy." });

            location.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục địa điểm thành công." });
        }

        // ============ ACTIVITY API ============

        [HttpGet("Activities")]
        public IActionResult Activities()
        {
            return View();
        }

        [HttpGet("Api/Activities")]
        public async Task<IActionResult> GetActivities(bool showInactive = false)
        {
            var query = _context.Activities
                .Include(a => a.Location)
                .Include(a => a.Organizer)
                .Include(a => a.ClassActivities)
                    .ThenInclude(ca => ca.Class)
                .AsQueryable();

            if (!showInactive) query = query.Where(a => a.IsActive);

            var activities = await query
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            var data = activities.Select(a => new
            {
                id = a.Id,
                name = a.Name,
                description = a.Description,
                date = a.Date?.ToString("yyyy-MM-dd"),
                locationName = a.Location?.Name,
                organizerName = a.Organizer?.FullName,
                isActive = a.IsActive,
                classes = a.ClassActivities.Select(ca => new { id = ca.ClassId, name = ca.Class.Name })
            });

            return Json(new { success = true, data });
        }

        [HttpPost("Api/Activity")]
        public async Task<IActionResult> CreateActivity([FromBody] SaveActivityViewModel model)
        {
            var activity = new Activity
            {
                Name = model.Name,
                Description = model.Description,
                Date = DateOnly.Parse(model.Date),
                LocationId = model.LocationId,
                OrganizerId = model.OrganizerId,
                IsActive = true
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            if (model.ClassIds != null && model.ClassIds.Any())
            {
                foreach (var classId in model.ClassIds)
                {
                    _context.ClassActivities.Add(new ClassActivity { ActivityId = activity.Id, ClassId = classId });
                }
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Đã tạo hoạt động thành công" });
        }

        [HttpPut("Api/Activity/{id:int}")]
        public async Task<IActionResult> UpdateActivity(int id, [FromBody] SaveActivityViewModel model)
        {
            var activity = await _context.Activities
                .Include(a => a.ClassActivities)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null) return Json(new { success = false, message = "Không tìm thấy hoạt động" });

            activity.Name = model.Name;
            activity.Description = model.Description;
            activity.Date = DateOnly.Parse(model.Date);
            activity.LocationId = model.LocationId;
            activity.OrganizerId = model.OrganizerId;

            // Update ClassActivities
            _context.ClassActivities.RemoveRange(activity.ClassActivities);
            if (model.ClassIds != null && model.ClassIds.Any())
            {
                foreach (var classId in model.ClassIds)
                {
                    _context.ClassActivities.Add(new ClassActivity { ActivityId = id, ClassId = classId });
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã cập nhật hoạt động" });
        }

        [HttpDelete("Api/Activity/{id:int}")]
        public async Task<IActionResult> DeleteActivity(int id)
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null) return Json(new { success = false, message = "Không tìm thấy hoạt động" });

            activity.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn hoạt động thành công." });
        }

        [HttpPost("Api/Activity/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateActivity(int id)
        {
            var activity = await _context.Activities.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id);
            if (activity == null) return Json(new { success = false, message = "Không tìm thấy." });

            activity.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục hoạt động thành công." });
        }

        // ============ CURRICULUM API ============

        [HttpGet("Curriculums")]
        public IActionResult Curriculums()
        {
            return View();
        }

        [HttpGet("Api/Curriculums")]
        public async Task<IActionResult> GetCurriculums(bool showInactive = false)
        {
            var query = _context.Curriculums.Include(c => c.Subject).AsQueryable();
            if (!showInactive) query = query.Where(c => c.IsActive);
            var curriculums = await query.OrderBy(c => c.Title).ToListAsync();

            var data = curriculums.Select(c => new
            {
                id = c.Id,
                title = c.Title,
                description = c.Description,
                content = c.Content,
                subjectId = c.SubjectId,
                subjectName = c.Subject?.Name,
                ageFrom = c.AgeFrom,
                ageTo = c.AgeTo,
                isActive = c.IsActive
            });

            return Json(new { success = true, data });
        }

        [HttpPost("Api/Curriculum")]
        public async Task<IActionResult> CreateCurriculum([FromBody] Curriculum model)
        {
            model.IsActive = true;
            _context.Curriculums.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã tạo chương trình học" });
        }

        [HttpPut("Api/Curriculum/{id:int}")]
        public async Task<IActionResult> UpdateCurriculum(int id, [FromBody] Curriculum model)
        {
            var cur = await _context.Curriculums.FindAsync(id);
            if (cur == null) return Json(new { success = false, message = "Không tìm thấy" });

            cur.Title = model.Title;
            cur.Description = model.Description;
            cur.Content = model.Content;
            cur.SubjectId = model.SubjectId;
            cur.AgeFrom = model.AgeFrom;
            cur.AgeTo = model.AgeTo;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã cập nhật" });
        }

        [HttpDelete("Api/Curriculum/{id:int}")]
        public async Task<IActionResult> DeleteCurriculum(int id)
        {
            var cur = await _context.Curriculums.FindAsync(id);
            if (cur == null) return Json(new { success = false, message = "Không tìm thấy." });

            cur.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn chương trình học thành công." });
        }

        [HttpPost("Api/Curriculum/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateCurriculum(int id)
        {
            var cur = await _context.Curriculums.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
            if (cur == null) return Json(new { success = false, message = "Không tìm thấy." });

            cur.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục chương trình học thành công." });
        }

        // ============ TEACHING PLAN API ============

        [HttpGet("TeachingPlans")]
        public IActionResult TeachingPlans()
        {
            return View();
        }

        [HttpGet("Api/TeachingPlans")]
        public async Task<IActionResult> GetTeachingPlans(int? classId, bool showInactive = false)
        {
            var query = _context.TeachingPlans
                .Include(tp => tp.Class)
                .Include(tp => tp.Curriculum)
                .AsQueryable();

            if (classId.HasValue) query = query.Where(tp => tp.ClassId == classId);
            if (!showInactive) query = query.Where(tp => tp.IsActive);

            var plans = await query.OrderByDescending(tp => tp.StartDate).ToListAsync();
            var data = plans.Select(tp => new
            {
                classId = tp.ClassId,
                className = tp.Class.Name,
                curriculumId = tp.CurriculumId,
                curriculumTitle = tp.Curriculum.Title,
                startDate = tp.StartDate.ToString("yyyy-MM-dd"),
                endDate = tp.EndDate?.ToString("yyyy-MM-dd"),
                status = tp.Status,
                isActive = tp.IsActive
            });

            return Json(new { success = true, data });
        }

        [HttpPost("Api/TeachingPlan")]
        public async Task<IActionResult> CreateTeachingPlan([FromBody] TeachingPlan model)
        {
            if (await _context.TeachingPlans.AnyAsync(tp => tp.ClassId == model.ClassId && tp.CurriculumId == model.CurriculumId && tp.StartDate == model.StartDate))
                return Json(new { success = false, message = "Kế hoạch này đã tồn tại" });
            
            model.IsActive = true;
            _context.TeachingPlans.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã lập kế hoạch giảng dạy" });
        }

        [HttpPut("Api/TeachingPlan")]
        public async Task<IActionResult> UpdateTeachingPlan([FromBody] TeachingPlan model)
        {
            var plan = await _context.TeachingPlans.FirstOrDefaultAsync(tp => 
                tp.ClassId == model.ClassId && 
                tp.CurriculumId == model.CurriculumId && 
                tp.StartDate == model.StartDate);

            if (plan == null) return Json(new { success = false, message = "Không tìm thấy kế hoạch để cập nhật" });

            plan.EndDate = model.EndDate;
            plan.Status = model.Status;

            _context.TeachingPlans.Update(plan);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật kế hoạch thành công" });
        }

        [HttpDelete("Api/TeachingPlan")]
        public async Task<IActionResult> DeleteTeachingPlan(int classId, int curriculumId, string startDate)
        {
            var sDate = DateOnly.Parse(startDate);
            var plan = await _context.TeachingPlans.FirstOrDefaultAsync(tp => tp.ClassId == classId && tp.CurriculumId == curriculumId && tp.StartDate == sDate);
            if (plan == null) return Json(new { success = false, message = "Không tìm thấy" });

            plan.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn kế hoạch giảng dạy thành công." });
        }

        [HttpPost("Api/TeachingPlan/Reactivate")]
        public async Task<IActionResult> ReactivateTeachingPlan(int classId, int curriculumId, string startDate)
        {
            var sDate = DateOnly.Parse(startDate);
            var plan = await _context.TeachingPlans.IgnoreQueryFilters()
                .FirstOrDefaultAsync(tp => tp.ClassId == classId && tp.CurriculumId == curriculumId && tp.StartDate == sDate);

            if (plan == null) return Json(new { success = false, message = "Không tìm thấy." });

            plan.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục kế hoạch giảng dạy thành công." });
        }

        private async Task<string> SaveAvatar(IFormFile file, string prefix)
        {
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            Directory.CreateDirectory(folderPath);

            var fileName = $"{prefix}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(folderPath, fileName);
            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/avatars/{fileName}";
        }
    }

    public class CreateAssignmentViewModel
    {
        public int EmployeeId { get; set; }
        public int ClassId { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string? EndDate { get; set; }
        public string? RoleInClass { get; set; }
    }

    public class CreateStudentViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string EnrollDate { get; set; } = string.Empty;
        public IFormFile? Avatar { get; set; }
    }

    public class UpdateStudentViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int? ClassId { get; set; }
        public string EnrollDate { get; set; } = string.Empty;
        public int Status { get; set; } = 0;
        public IFormFile? Avatar { get; set; }
    }

    public class CreateTeacherViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string Username { get; set; } = string.Empty;
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(9, ErrorMessage = "Mật khẩu phải có ít nhất 9 ký tự")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[!@#$%^&*()_+=\-\[\]{}|;:'"",.<>?/\\\\]).+$", 
            ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ hoa và 1 ký tự đặc biệt")]
        public string Password { get; set; } = string.Empty;
        [Required(ErrorMessage = "Họ đệm không được để trống")]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Tên không được để trống")]
        public string LastName { get; set; } = string.Empty;
        public bool Gender { get; set; }
        public string? Phone { get; set; }
        public string? Position { get; set; }
        public TeacherType TeacherType { get; set; }
        public decimal? BaseSalary { get; set; }
        public IFormFile? Avatar { get; set; }
        public string? Bio { get; set; }
        public string? Qualifications { get; set; }
        public string? Experience { get; set; }
        public string? Philosophy { get; set; }
        public string? Specialty { get; set; }
        public bool ShowOnLanding { get; set; }
    }

    public class UpdateTeacherViewModel
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Họ đệm không được để trống")]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Tên không được để trống")]
        public string LastName { get; set; } = string.Empty;
        public bool Gender { get; set; }
        public string? Phone { get; set; }
        public string? Position { get; set; }
        public TeacherType TeacherType { get; set; }
        public decimal? BaseSalary { get; set; }
        public IFormFile? Avatar { get; set; }
        public string? Bio { get; set; }
        public string? Qualifications { get; set; }
        public string? Experience { get; set; }
        public string? Philosophy { get; set; }
        public string? Specialty { get; set; }
        public bool ShowOnLanding { get; set; }
    }

    public class SaveClassViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int? AgeFrom { get; set; }
        public int? AgeTo { get; set; }
        public string? SchoolYear { get; set; }
        public int MaxCapacity { get; set; } = 25;
    }

    public class SaveSubjectViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SaveClassScheduleViewModel
    {
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public int EmployeeId { get; set; }
        public int DayOfWeek { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public int? LocationId { get; set; }
        public string EffectiveFrom { get; set; } = string.Empty;
        public string? EffectiveTo { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SaveActivityViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Date { get; set; } = string.Empty;
        public int? LocationId { get; set; }
        public int? OrganizerId { get; set; }
        public List<int>? ClassIds { get; set; }
    }
}
