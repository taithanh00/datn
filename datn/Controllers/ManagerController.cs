using datn.Data;
using datn.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace datn.Controllers
{
    [Authorize(Roles = "Manager")]
    [Route("[controller]")]
    public class ManagerController : Controller
    {
        private static readonly TimeOnly SchoolStart = new(7, 0);
        private static readonly TimeOnly LunchStart = new(11, 0);
        private static readonly TimeOnly LunchEnd = new(13, 0);
        private static readonly TimeOnly SchoolEnd = new(16, 30);

        private readonly AppDbContext _context;

        public ManagerController(AppDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var username = User.Identity?.Name ?? "User";
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            ViewBag.Username = username;
            ViewBag.Role = role;

            var employee = _context.Employees.Include(e => e.Account)
                .FirstOrDefault(e => e.Account.Username == username);
            ViewBag.UserAvatar = employee?.AvatarPath ?? "/images/lion_orange.png";

            base.OnActionExecuting(context);
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

            // Doanh thu tháng hiện tại (TuitionPlan Amount * số học sinh đã nộp)
            var currentMonthRevenue = await _context.Tuitions
                .Where(t => t.Month == nowVnt.Month && t.Year == nowVnt.Year && t.IsPaid)
                .Join(_context.TuitionPlans, t => t.TuitionPlanId, tp => tp.Id, (t, tp) => tp.Amount)
                .SumAsync();

            // 2. Doanh thu 6 tháng gần nhất (Biểu đồ đường)
            var revenueChart = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var d = nowVnt.AddMonths(-i);
                var rev = await _context.Tuitions
                    .Where(t => t.Month == d.Month && t.Year == d.Year && t.IsPaid)
                    .Join(_context.TuitionPlans, t => t.TuitionPlanId, tp => tp.Id, (t, tp) => tp.Amount)
                    .SumAsync();
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
                    name = r.Employee.FullName,
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

        [HttpDelete("Api/Assignment")]
        public async Task<IActionResult> DeleteAssignment(int employeeId, int classId, string startDate)
        {
            try
            {
                if (!DateOnly.TryParse(startDate, out var parsedStartDate))
                    return Json(new { success = false, message = "Ngày bắt đầu không hợp lệ." });

                var assignment = await _context.Assignments.FirstOrDefaultAsync(a =>
                    a.EmployeeId == employeeId && a.ClassId == classId && a.StartDate == parsedStartDate);

                if (assignment == null)
                    return Json(new { success = false, message = "Không tìm thấy phân công." });

                _context.Assignments.Remove(assignment);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa phân công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============ STUDENT API ============

        [HttpGet("Api/Students")]
        public async Task<IActionResult> GetStudents()
        {
            try
            {
                var students = await _context.Students.Include(s => s.Class).OrderBy(s => s.Id).ToListAsync();
                var result = students.Select(s => new
                {
                    id = s.Id,
                    fullName = $"{s.FirstName} {s.LastName}".Trim(),
                    gender = s.Gender.HasValue ? (s.Gender.Value ? "Nam" : "Nữ") : "Chưa xác định",
                    dateOfBirth = s.DateOfBirth?.ToString("dd/MM/yyyy") ?? "N/A",
                    className = s.Class?.Name ?? "Chưa có lớp",
                    enrollDate = s.EnrollDate?.ToString("dd/MM/yyyy") ?? "N/A",
                    avatarPath = s.AvatarPath ?? "/images/lion_orange.png"
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
                var s = await _context.Students.FindAsync(id);
                if (s == null) return Json(new { success = false, message = "Không tìm thấy" });

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = s.Id,
                        firstName = s.FirstName,
                        lastName = s.LastName,
                        gender = s.Gender?.ToString().ToLower(),
                        dateOfBirth = s.DateOfBirth?.ToString("yyyy-MM-dd"),
                        address = s.Address,
                        classId = s.ClassId,
                        enrollDate = s.EnrollDate?.ToString("yyyy-MM-dd"),
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

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa học sinh thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Api/Student")]
        public async Task<IActionResult> CreateStudent([FromForm] CreateStudentViewModel model)
        {
            try
            {
                var student = new Student
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Gender = model.Gender == "true",
                    DateOfBirth = string.IsNullOrEmpty(model.DateOfBirth) ? null : DateOnly.Parse(model.DateOfBirth),
                    Address = model.Address,
                    ClassId = model.ClassId > 0 ? model.ClassId : null,
                    EnrollDate = string.IsNullOrEmpty(model.EnrollDate) ? null : DateOnly.Parse(model.EnrollDate)
                };

                if (model.Avatar != null) student.AvatarPath = await SaveAvatar(model.Avatar, "student");

                _context.Students.Add(student);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Thêm học sinh thành công" });
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

                student.FirstName = model.FirstName;
                student.LastName = model.LastName;
                student.Gender = model.Gender == "true";
                if (!string.IsNullOrEmpty(model.DateOfBirth)) student.DateOfBirth = DateOnly.Parse(model.DateOfBirth);
                student.Address = model.Address;
                student.ClassId = model.ClassId > 0 ? model.ClassId : null;
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

        // ============ TEACHER API ============

        [HttpGet("Api/Teachers")]
        public async Task<IActionResult> GetTeachers()
        {
            try
            {
                var teachers = await _context.Employees
                    .Include(e => e.Account)
                    .ThenInclude(a => a.Role)
                    .Where(e => e.Account.Role.Name == "Employee")
                    .OrderBy(e => e.FullName)
                    .ToListAsync();

                var result = teachers.Select(t => new
                {
                    id = t.Id,
                    fullName = t.FullName,
                    phone = t.Phone,
                    position = t.Position ?? "Giáo viên",
                    baseSalary = t.BaseSalary,
                    avatarPath = t.AvatarPath ?? "/images/lion_blue.png"
                }).ToList();
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("Api/Teacher/{id:int}")]
        public async Task<IActionResult> GetTeacher(int id)
        {
            var teacher = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
            if (teacher == null)
                return Json(new { success = false, message = "Không tìm thấy giáo viên." });

            return Json(new
            {
                success = true,
                data = new
                {
                    id = teacher.Id,
                    fullName = teacher.FullName,
                    phone = teacher.Phone,
                    position = teacher.Position,
                    baseSalary = teacher.BaseSalary,
                    avatarPath = teacher.AvatarPath
                }
            });
        }

        [HttpPost("Api/Teacher")]
        public async Task<IActionResult> CreateTeacher([FromForm] CreateTeacherViewModel model)
        {
            try
            {
                var role = await _context.Roles.FirstAsync(r => r.Name == "Employee");
                var seedName = string.IsNullOrWhiteSpace(model.FullName) ? "teacher" : model.FullName.Replace(" ", "").ToLowerInvariant();
                var account = new Account
                {
                    Username = seedName + Guid.NewGuid().ToString("N")[..4],
                    Email = $"{Guid.NewGuid():N}".Substring(0, 8) + "@school.edu",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    PasswordSalt = "",
                    RoleId = role.Id
                };
                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();

                var teacher = new Employee
                {
                    AccountId = account.Id,
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Position = model.Position,
                    BaseSalary = model.BaseSalary
                };
                if (model.Avatar != null) teacher.AvatarPath = await SaveAvatar(model.Avatar, "teacher");

                _context.Employees.Add(teacher);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Thêm giáo viên thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Api/Teacher/{id:int}")]
        public async Task<IActionResult> UpdateTeacher(int id, [FromForm] UpdateTeacherViewModel model)
        {
            try
            {
                var teacher = await _context.Employees.FindAsync(id);
                if (teacher == null)
                    return Json(new { success = false, message = "Không tìm thấy giáo viên." });

                teacher.FullName = model.FullName;
                teacher.Phone = model.Phone;
                teacher.Position = model.Position;
                teacher.BaseSalary = model.BaseSalary;

                if (model.Avatar != null) teacher.AvatarPath = await SaveAvatar(model.Avatar, "teacher");

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật giáo viên thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Api/Teacher/{id:int}")]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            try
            {
                var teacher = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
                if (teacher == null)
                    return Json(new { success = false, message = "Không tìm thấy giáo viên." });

                var hasDependencies = await _context.Assignments.AnyAsync(a => a.EmployeeId == id)
                    || await _context.ClassSchedules.AnyAsync(s => s.EmployeeId == id)
                    || await _context.WorkAttendances.AnyAsync(w => w.EmployeeId == id);

                if (hasDependencies)
                    return Json(new { success = false, message = "Không thể xóa giáo viên đang có phân công, lịch dạy hoặc chấm công." });

                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == teacher.AccountId);
                _context.Employees.Remove(teacher);
                if (account != null)
                {
                    _context.Accounts.Remove(account);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa giáo viên thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============ CLASS MANAGEMENT API ============

        [HttpGet("Api/Classes")]
        public async Task<IActionResult> GetClasses()
        {
            var classes = await _context.Classes
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Json(new
            {
                success = true,
                data = classes.Select(c => new { id = c.Id, name = c.Name })
            });
        }

        [HttpGet("Api/Classes/Overview")]
        public async Task<IActionResult> GetClassesOverview()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            var classes = await _context.Classes
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
                studentCount = c.Students.Count,
                teachers = c.Assignments
                    .Where(a => a.StartDate <= today && (a.EndDate == null || a.EndDate >= today))
                    .Select(a => new
                    {
                        employeeId = a.EmployeeId,
                        teacherName = a.Employee.FullName,
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
                            teacherName = a.Employee.FullName,
                            roleInClass = a.RoleInClass ?? "Giáo viên"
                        })
                        .ToList()
                }
            });
        }

        [HttpPost("Api/Class")]
        public async Task<IActionResult> CreateClass([FromBody] SaveClassViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Tên lớp không được để trống." });

            var duplicate = await _context.Classes.AnyAsync(c => c.Name == model.Name.Trim() && c.SchoolYear == model.SchoolYear);
            if (duplicate)
                return Json(new { success = false, message = "Đã tồn tại lớp cùng tên trong niên khóa này." });

            var classroom = new Class
            {
                Name = model.Name.Trim(),
                AgeFrom = model.AgeFrom,
                AgeTo = model.AgeTo,
                SchoolYear = model.SchoolYear?.Trim()
            };

            _context.Classes.Add(classroom);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Tạo lớp học thành công." });
        }

        [HttpPut("Api/Class/{id:int}")]
        public async Task<IActionResult> UpdateClass(int id, [FromBody] SaveClassViewModel model)
        {
            var classroom = await _context.Classes.FindAsync(id);
            if (classroom == null)
                return Json(new { success = false, message = "Không tìm thấy lớp học." });

            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Tên lớp không được để trống." });

            var duplicate = await _context.Classes.AnyAsync(c =>
                c.Id != id && c.Name == model.Name.Trim() && c.SchoolYear == model.SchoolYear);
            if (duplicate)
                return Json(new { success = false, message = "Đã tồn tại lớp cùng tên trong niên khóa này." });

            classroom.Name = model.Name.Trim();
            classroom.AgeFrom = model.AgeFrom;
            classroom.AgeTo = model.AgeTo;
            classroom.SchoolYear = model.SchoolYear?.Trim();
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cập nhật lớp học thành công." });
        }

        [HttpDelete("Api/Class/{id:int}")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var classroom = await _context.Classes.FindAsync(id);
            if (classroom == null)
                return Json(new { success = false, message = "Không tìm thấy lớp học." });

            var hasStudents = await _context.Students.AnyAsync(s => s.ClassId == id);
            var hasAssignments = await _context.Assignments.AnyAsync(a => a.ClassId == id);
            var hasSchedules = await _context.ClassSchedules.AnyAsync(s => s.ClassId == id);

            if (hasStudents || hasAssignments || hasSchedules)
                return Json(new { success = false, message = "Không thể xóa lớp đang có học sinh, phân công hoặc thời khóa biểu." });

            _context.Classes.Remove(classroom);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa lớp học." });
        }

        // ============ SUBJECT API ============

        [HttpGet("Api/Subjects")]
        public async Task<IActionResult> GetSubjects()
        {
            var subjects = await _context.Subjects
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
            if (subject == null)
                return Json(new { success = false, message = "Không tìm thấy môn học." });

            var hasSchedules = await _context.ClassSchedules.AnyAsync(cs => cs.SubjectId == id);
            if (hasSchedules)
                return Json(new { success = false, message = "Không thể xóa môn học đang được dùng trong thời khóa biểu." });

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa môn học." });
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
                    effectiveFrom = schedule.EffectiveFrom.ToString("yyyy-MM-dd"),
                    effectiveTo = schedule.EffectiveTo?.ToString("yyyy-MM-dd"),
                    note = schedule.Note,
                    isActive = schedule.IsActive
                }
            });
        }

        [HttpPost("Api/ClassSchedule")]
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
            schedule.EffectiveFrom = DateOnly.Parse(model.EffectiveFrom);
            schedule.EffectiveTo = string.IsNullOrWhiteSpace(model.EffectiveTo) ? null : DateOnly.Parse(model.EffectiveTo);
            schedule.Note = model.Note?.Trim();
            schedule.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thời khóa biểu thành công." });
        }

        [HttpDelete("Api/ClassSchedule/{id:int}")]
        public async Task<IActionResult> DeleteClassSchedule(int id)
        {
            var schedule = await _context.ClassSchedules.FindAsync(id);
            if (schedule == null)
                return Json(new { success = false, message = "Không tìm thấy thời khóa biểu." });

            _context.ClassSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa thời khóa biểu." });
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

            var classOverlap = sameDaySchedules.Any(cs =>
                cs.ClassId == model.ClassId
                && DateRangesOverlap(cs.EffectiveFrom, cs.EffectiveTo, effectiveFrom, effectiveTo)
                && TimeRangesOverlap(cs.StartTime, cs.EndTime, startTime, endTime));

            if (classOverlap)
                return "Lớp học đã có tiết khác trùng khung giờ này.";

            var teacherOverlap = sameDaySchedules.Any(cs =>
                cs.EmployeeId == model.EmployeeId
                && DateRangesOverlap(cs.EffectiveFrom, cs.EffectiveTo, effectiveFrom, effectiveTo)
                && TimeRangesOverlap(cs.StartTime, cs.EndTime, startTime, endTime));

            if (teacherOverlap)
                return "Giáo viên đã có lịch dạy khác trùng khung giờ này.";

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
        public IFormFile? Avatar { get; set; }
    }

    public class CreateTeacherViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Position { get; set; }
        public decimal? BaseSalary { get; set; }
        public IFormFile? Avatar { get; set; }
    }

    public class UpdateTeacherViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Position { get; set; }
        public decimal? BaseSalary { get; set; }
        public IFormFile? Avatar { get; set; }
    }

    public class SaveClassViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int? AgeFrom { get; set; }
        public int? AgeTo { get; set; }
        public string? SchoolYear { get; set; }
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
        public string EffectiveFrom { get; set; } = string.Empty;
        public string? EffectiveTo { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
