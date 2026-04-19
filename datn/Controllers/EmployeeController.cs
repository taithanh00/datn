using datn.Data;
using datn.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace datn.Controllers
{
    [Authorize(Roles = "Employee")]
    [Route("[controller]")]
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var username = User.Identity?.Name ?? "User";
            ViewBag.Username = username;
            ViewBag.Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            var employee = _context.Employees.Include(e => e.Account)
                .FirstOrDefault(e => e.Account.Username == username);
            ViewBag.UserAvatar = employee?.AvatarPath ?? "/images/lion_blue.png";

            base.OnActionExecuting(context);
        }

        private async Task<int?> GetCurrentEmployeeId()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;

            var employee = await _context.Employees
                .Include(e => e.Account)
                .FirstOrDefaultAsync(e => e.Account.Username == username);

            return employee?.Id;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Bảng điều khiển Giáo viên";
            return View();
        }

        [HttpGet("Api/DashboardStats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var employeeId = await GetCurrentEmployeeId();
            if (employeeId == null)
                return Json(new { success = false, message = "Không tìm thấy thông tin giáo viên." });

            var today = GetTodayVnt();

            // 1. Lớp học đang phụ trách (lấy lớp đầu tiên nếu có nhiều lớp)
            var activeAssignment = await _context.Assignments
                .Include(a => a.Class).ThenInclude(c => c.Students)
                .Where(a => a.EmployeeId == employeeId && a.StartDate <= today && (a.EndDate == null || a.EndDate >= today))
                .OrderByDescending(a => a.StartDate)
                .FirstOrDefaultAsync();

            int classSize = 0;
            int presentToday = 0;
            string className = "Chưa phân lớp";
            var rankingData = new List<object>();

            if (activeAssignment != null)
            {
                className = activeAssignment.Class.Name;
                classSize = activeAssignment.Class.Students.Count;
                presentToday = await _context.Attendances
                    .CountAsync(a => a.Date == today && a.Student.ClassId == activeAssignment.ClassId && a.Status == "Present");

                // Biểu đồ học lực (lấy từ StudyReport tháng gần nhất)
                var rankings = await _context.StudyReports
                    .Where(sr => sr.Student.ClassId == activeAssignment.ClassId)
                    .GroupBy(sr => sr.Ranking.Name)
                    .Select(g => new { label = g.Key, value = g.Count() })
                    .ToListAsync();
                
                rankingData = rankings.Cast<object>().ToList();
            }

            // 2. Chấm công bản thân hôm nay
            var myAttendance = await _context.WorkAttendances
                .FirstOrDefaultAsync(w => w.EmployeeId == employeeId && w.Date == today);

            // 3. Lương tháng trước
            var lastMonth = today.AddMonths(-1);
            var lastSalary = await _context.Salaries
                .Include(s => s.PayrollPeriod)
                .Where(s => s.EmployeeId == employeeId && s.PayrollPeriod.Month == lastMonth.Month && s.PayrollPeriod.Year == lastMonth.Year)
                .Select(s => s.SalaryAmount)
                .FirstOrDefaultAsync() ?? 0;

            // 4. Lịch học hôm nay
            var dayOfWeek = GetSchoolDayOfWeek(today);
            var todaySchedule = new List<object>();
            if (dayOfWeek.HasValue)
            {
                var schedules = await _context.ClassSchedules
                    .Where(cs => cs.EmployeeId == employeeId && cs.DayOfWeek == dayOfWeek.Value && cs.IsActive)
                    .Include(cs => cs.Class)
                    .Include(cs => cs.Subject)
                    .OrderBy(cs => cs.StartTime)
                    .ToListAsync();

                todaySchedule = schedules.Select(cs => new {
                    time = $"{cs.StartTime:HH:mm} - {cs.EndTime:HH:mm}",
                    className = cs.Class.Name,
                    subject = cs.Subject.Name
                }).Cast<object>().ToList();
            }

            return Json(new {
                success = true,
                stats = new {
                    className,
                    classSize,
                    presentToday,
                    checkInTime = myAttendance?.CheckInAtUtc?.ToString("HH:mm") ?? "--:--",
                    lastSalary
                },
                charts = new { ranking = rankingData },
                todaySchedule
            });
        }

        [HttpGet("Classes")]
        public IActionResult Classes()
        {
            ViewData["Title"] = "Lớp của tôi";
            return View();
        }

        [HttpGet("Attendance")]
        public IActionResult Attendance(int? classId)
        {
            ViewData["Title"] = "Điểm danh";
            ViewBag.SelectedClassId = classId;
            return View();
        }

        [HttpGet("WorkSchedule")]
        public IActionResult WorkSchedule(int? classId)
        {
            ViewData["Title"] = "Lịch làm việc";
            ViewBag.SelectedClassId = classId;
            return View();
        }

        // ============ API ENDPOINTS ============

        [HttpGet("Api/MyClasses")]
        public async Task<IActionResult> GetMyClasses()
        {
            try
            {
                var employeeId = await GetCurrentEmployeeId();
                if (employeeId == null) return Json(new { success = false, message = "Không tìm thấy thông tin giáo viên" });

                var today = GetTodayVnt();
                var assignments = await _context.Assignments
                    .Include(a => a.Class)
                    .Where(a => a.EmployeeId == employeeId
                        && a.StartDate <= today
                        && (a.EndDate == null || a.EndDate >= today))
                    .Select(a => new
                    {
                        classId = a.ClassId,
                        className = a.Class.Name,
                        role = a.RoleInClass ?? "Giáo viên",
                        studentCount = a.Class.Students.Count
                    })
                    .ToListAsync();

                return Json(new { success = true, data = assignments });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("Api/TodaySchedule")]
        public async Task<IActionResult> GetTodaySchedule()
        {
            var employeeId = await GetCurrentEmployeeId();
            if (employeeId == null)
                return Json(new { success = false, message = "Không tìm thấy thông tin giáo viên." });

            var today = GetTodayVnt();
            var dayOfWeek = GetSchoolDayOfWeek(today);

            if (dayOfWeek is null)
            {
                return Json(new { success = true, data = Array.Empty<object>(), date = today.ToString("dd/MM/yyyy") });
            }

            var schedules = await _context.ClassSchedules
                .Where(cs => cs.EmployeeId == employeeId
                    && cs.IsActive
                    && cs.DayOfWeek == dayOfWeek.Value
                    && cs.EffectiveFrom <= today
                    && (cs.EffectiveTo == null || cs.EffectiveTo >= today))
                .Include(cs => cs.Class)
                .Include(cs => cs.Subject)
                .OrderBy(cs => cs.StartTime)
                .ToListAsync();

            return Json(new
            {
                success = true,
                date = today.ToString("dd/MM/yyyy"),
                data = schedules.Select(cs => new
                {
                    id = cs.Id,
                    classId = cs.ClassId,
                    className = cs.Class.Name,
                    subjectName = cs.Subject.Name,
                    startTime = cs.StartTime.ToString("HH:mm"),
                    endTime = cs.EndTime.ToString("HH:mm"),
                    note = cs.Note
                })
            });
        }

        [HttpGet("Api/ManagedStudents/{classId:int}")]
        public async Task<IActionResult> GetManagedStudents(int classId)
        {
            try
            {
                var employeeId = await GetCurrentEmployeeId();
                if (employeeId == null)
                    return Json(new { success = false, message = "Không tìm thấy thông tin giáo viên." });

                var today = GetTodayVnt();
                var isAssigned = await HasActiveAssignmentAsync(employeeId.Value, classId, today);
                if (!isAssigned)
                    return Json(new { success = false, message = "Bạn không có quyền xem lớp này." });

                var students = await _context.Students
                    .Where(s => s.ClassId == classId)
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .Select(s => new
                    {
                        id = s.Id,
                        fullName = ((s.FirstName ?? "") + " " + (s.LastName ?? "")).Trim(),
                        gender = s.Gender.HasValue ? (s.Gender.Value ? "Nam" : "Nữ") : "Chưa xác định",
                        dateOfBirth = s.DateOfBirth != null ? s.DateOfBirth.Value.ToString("dd/MM/yyyy") : "N/A",
                        address = s.Address ?? "Chưa cập nhật",
                        enrollDate = s.EnrollDate != null ? s.EnrollDate.Value.ToString("dd/MM/yyyy") : "N/A",
                        avatarPath = s.AvatarPath ?? "/images/lion_orange.png",
                        attendanceStatus = _context.Attendances
                            .Where(a => a.StudentId == s.Id && a.Date == today)
                            .Select(a => a.Status)
                            .FirstOrDefault() ?? ""
                    })
                    .ToListAsync();

                return Json(new { success = true, data = students });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("Api/ClassStudents/{classId:int}")]
        public async Task<IActionResult> GetClassStudents(int classId)
        {
            try
            {
                var employeeId = await GetCurrentEmployeeId();
                if (employeeId == null)
                    return Json(new { success = false, message = "Không tìm thấy thông tin giáo viên" });

                var today = GetTodayVnt();
                var isAssigned = await HasActiveAssignmentAsync(employeeId.Value, classId, today);
                if (!isAssigned)
                    return Json(new { success = false, message = "Bạn không có quyền quản lý lớp này" });

                var students = await _context.Students
                    .Where(s => s.ClassId == classId)
                    .OrderBy(s => s.LastName)
                    .Select(s => new
                    {
                        id = s.Id,
                        fullName = ((s.FirstName ?? "") + " " + (s.LastName ?? "")).Trim(),
                        gender = s.Gender.HasValue ? (s.Gender.Value ? "Nam" : "Nữ") : "N/A",
                        attendanceStatus = _context.Attendances
                            .Where(a => a.StudentId == s.Id && a.Date == today)
                            .Select(a => a.Status)
                            .FirstOrDefault() ?? ""
                    })
                    .ToListAsync();

                return Json(new { success = true, data = students });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Api/SubmitAttendance")]
        public async Task<IActionResult> SubmitAttendance([FromBody] AttendanceSubmissionModel model)
        {
            try
            {
                var employeeId = await GetCurrentEmployeeId();
                if (employeeId == null) return Json(new { success = false, message = "Hết phiên đăng nhập" });
                if (model.Records == null || model.Records.Count == 0)
                    return Json(new { success = false, message = "Không có dữ liệu điểm danh." });

                var today = GetTodayVnt();
                var studentIds = model.Records.Select(r => r.StudentId).Distinct().ToList();
                var students = await _context.Students
                    .Where(s => studentIds.Contains(s.Id))
                    .Select(s => new { s.Id, s.ClassId })
                    .ToListAsync();

                if (students.Count != studentIds.Count || students.Any(s => s.ClassId == null))
                    return Json(new { success = false, message = "Có học sinh không hợp lệ hoặc chưa thuộc lớp nào." });

                var classIds = students.Select(s => s.ClassId!.Value).Distinct().ToList();
                foreach (var classId in classIds)
                {
                    var isAssigned = await HasActiveAssignmentAsync(employeeId.Value, classId, today);
                    if (!isAssigned)
                        return Json(new { success = false, message = "Bạn không có quyền điểm danh cho một hoặc nhiều lớp trong danh sách." });
                }

                foreach (var item in model.Records)
                {
                    var existing = await _context.Attendances
                        .FirstOrDefaultAsync(a => a.StudentId == item.StudentId && a.Date == today);

                    if (existing != null)
                    {
                        existing.Status = item.Status;
                        existing.TakenBy = employeeId;
                    }
                    else
                    {
                        _context.Attendances.Add(new Attendance
                        {
                            StudentId = item.StudentId,
                            Date = today,
                            Status = item.Status,
                            TakenBy = employeeId
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Lưu điểm danh thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<bool> HasActiveAssignmentAsync(int employeeId, int classId, DateOnly onDate)
        {
            return await _context.Assignments.AnyAsync(a =>
                a.EmployeeId == employeeId
                && a.ClassId == classId
                && a.StartDate <= onDate
                && (a.EndDate == null || a.EndDate >= onDate));
        }

        private static DateOnly GetTodayVnt()
        {
            var utcNow = DateTimeOffset.UtcNow;
            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }

            var localNow = TimeZoneInfo.ConvertTime(utcNow, tz);
            return DateOnly.FromDateTime(localNow.DateTime);
        }

        private static int? GetSchoolDayOfWeek(DateOnly date)
        {
            return date.DayOfWeek switch
            {
                DayOfWeek.Monday => 1,
                DayOfWeek.Tuesday => 2,
                DayOfWeek.Wednesday => 3,
                DayOfWeek.Thursday => 4,
                DayOfWeek.Friday => 5,
                _ => null
            };
        }
    }

    public class AttendanceSubmissionModel
    {
        public List<AttendanceRecordModel> Records { get; set; } = new();
    }

    public class AttendanceRecordModel
    {
        public int StudentId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
