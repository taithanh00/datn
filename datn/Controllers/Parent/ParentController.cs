using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using datn.Controllers;
using System.Security.Claims;

namespace datn.Controllers.Parent
{
    [Authorize(Roles = "Parent")]
    [Route("[controller]")]
    public class ParentController : BaseController
    {
        public ParentController(AppDbContext context) : base(context)
        {
        }

        private async Task<int?> GetCurrentParentId()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;

            var parent = await _context.Parents
                .Include(p => p.Account)
                .FirstOrDefaultAsync(p => p.Account.Username == username);

            return parent?.Id;
        }

        private async Task<Account?> GetCurrentAccountAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;

            return await _context.Accounts.FirstOrDefaultAsync(a => a.Username == username);
        }

        [HttpGet("Children")]
        public async Task<IActionResult> Children()
        {
            ViewData["Title"] = "Thông tin con";
            var parentId = await GetCurrentParentId();
            if (parentId == null) return RedirectToAction("Login", "Auth");

            var students = await _context.ParentStudents
                .Include(ps => ps.Student).ThenInclude(s => s.Class)
                .Where(ps => ps.ParentId == parentId)
                .Select(ps => ps.Student)
                .ToListAsync();

            var todayDate = DateOnly.FromDateTime(DateTime.Today);
            var result = new List<ParentChildrenViewModel>();

            foreach (var student in students)
            {
                var todayLessons = new List<TodayLessonViewModel>();
                if (student.ClassId.HasValue)
                {
                    // Lấy thời khóa biểu hôm nay của lớp
                    var dayOfWeek = (int)DateTime.Today.DayOfWeek;
                    var schedules = await _context.ClassSchedules
                        .Include(cs => cs.Subject)
                        .Where(cs => cs.ClassId == student.ClassId && cs.DayOfWeek == dayOfWeek && cs.IsActive)
                        .OrderBy(cs => cs.StartTime)
                        .ToListAsync();

                    foreach (var s in schedules)
                    {
                        todayLessons.Add(new TodayLessonViewModel {
                            SubjectName = s.Subject?.Name ?? "N/A",
                            Time = $"{s.StartTime:HH:mm}"
                        });
                    }
                }

                result.Add(new ParentChildrenViewModel {
                    Student = student,
                    TodayLessons = todayLessons
                });
            }

            return View("~/Views/Dashboard/Parent/Parent/Children.cshtml", result);
        }

        [HttpGet("ClassClosure")]
        public async Task<IActionResult> ClassClosure(int classId, DateOnly date)
        {
            ViewData["Title"] = "Chi ti\u1ebft ngh\u1ec9 h\u1ecdc";
            var parentId = await GetCurrentParentId();
            var account = await GetCurrentAccountAsync();
            if (parentId == null || account == null) return RedirectToAction("Login", "Auth");

            var hasChildInClass = await _context.ParentStudents
                .AnyAsync(ps => ps.ParentId == parentId && ps.Student.ClassId == classId);
            if (!hasChildInClass) return Forbid();

            var classroom = await _context.Classes.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == classId);
            if (classroom == null) return NotFound();

            var dateText = date.ToString("dd/MM/yyyy");
            var notification = await _context.Notifications
                .Where(n => n.RecipientId == account.Id
                    && n.Message.Contains(classroom.Name)
                    && n.Message.Contains(dateText))
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync();

            ViewBag.ClassName = classroom.Name;
            ViewBag.Date = date;
            ViewBag.TitleText = notification?.Title ?? $"L\u1edbp {classroom.Name} ngh\u1ec9 h\u1ecdc ng\u00e0y {dateText}";
            ViewBag.Reason = ToParentFacingReason(ExtractReason(notification?.Message));
            ViewBag.Message = $"L\u1edbp {classroom.Name} ngh\u1ec9 h\u1ecdc ng\u00e0y {dateText}. L\u00fd do: {ViewBag.Reason}.";

            return View("~/Views/Dashboard/Parent/Parent/ClassClosure.cshtml");
        }

        [HttpGet("HolidayDetail/{id:int}")]
        public async Task<IActionResult> HolidayDetail(int id)
        {
            ViewData["Title"] = "Chi ti\u1ebft ng\u00e0y l\u1ec5";
            var holiday = await _context.Holidays.IgnoreQueryFilters()
                .FirstOrDefaultAsync(h => h.Id == id);
            if (holiday == null) return NotFound();

            ViewBag.StatusText = GetHolidayStatusText(holiday.Date);
            return View("~/Views/Dashboard/Parent/Parent/HolidayDetail.cshtml", holiday);
        }

        [HttpGet("StudyReports")]
        public async Task<IActionResult> StudyReports(int? studentId, int? year)
        {
            ViewData["Title"] = "Báo cáo Học tập";
            var parentId = await GetCurrentParentId();
            if (parentId == null) return RedirectToAction("Login", "Auth");

            // Lấy danh sách con của phụ huynh này
            var children = await _context.ParentStudents
                .Include(ps => ps.Student)
                    .ThenInclude(s => s.Class)
                .Where(ps => ps.ParentId == parentId)
                .Select(ps => ps.Student)
                .ToListAsync();

            ViewBag.Children = children;

            if (children.Count == 0) return View("~/Views/Dashboard/Parent/Parent/StudyReports.cshtml", new List<StudyReport>());

            // Mặc định chọn đứa con đầu tiên nếu không chỉ định
            var targetStudentId = studentId ?? children.First().Id;
            var targetYear = year ?? DateTime.Now.Year;

            // Kiểm tra xem phụ huynh có quyền xem học sinh này không
            if (!children.Any(c => c.Id == targetStudentId)) return Forbid();

            var reports = await _context.StudyReports
                .Include(sr => sr.Ranking)
                .Include(sr => sr.Teacher)
                .Where(sr => sr.StudentId == targetStudentId && sr.Date.Year == targetYear)
                .OrderByDescending(sr => sr.Date)
                .ToListAsync();

            ViewBag.SelectedStudentId = targetStudentId;
            ViewBag.SelectedYear = targetYear;

            return View("~/Views/Dashboard/Parent/Parent/StudyReports.cshtml", reports);
        }

        [HttpGet("AttendanceReport")]
        public async Task<IActionResult> AttendanceReport(int? studentId, int? month, int? year)
        {
            ViewData["Title"] = "Báo cáo Điểm danh";
            var parentId = await GetCurrentParentId();
            if (parentId == null) return RedirectToAction("Login", "Auth");

            var children = await _context.ParentStudents
                .Include(ps => ps.Student)
                .Where(ps => ps.ParentId == parentId)
                .Select(ps => ps.Student)
                .ToListAsync();

            ViewBag.Children = children;

            if (children.Count == 0) return View("~/Views/Dashboard/Parent/Parent/AttendanceReport.cshtml", new List<Attendance>());

            var targetStudentId = studentId ?? children.First().Id;
            var targetMonth = month ?? DateTime.Now.Month;
            var targetYear = year ?? DateTime.Now.Year;

            if (!children.Any(c => c.Id == targetStudentId)) return Forbid();

            var attendances = await _context.Attendances
                .Where(a => a.StudentId == targetStudentId && a.Date.Month == targetMonth && a.Date.Year == targetYear)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            ViewBag.SelectedStudentId = targetStudentId;
            ViewBag.SelectedMonth = targetMonth;
            ViewBag.SelectedYear = targetYear;

            return View("~/Views/Dashboard/Parent/Parent/AttendanceReport.cshtml", attendances);
        }

        [HttpGet("Activities")]
        public async Task<IActionResult> Activities(int? studentId)
        {
            ViewData["Title"] = "Hoạt động & Sự kiện";
            var parentId = await GetCurrentParentId();
            if (parentId == null) return RedirectToAction("Login", "Auth");

            var children = await _context.ParentStudents
                .Include(ps => ps.Student).ThenInclude(s => s.Class)
                .Where(ps => ps.ParentId == parentId)
                .Select(ps => ps.Student)
                .ToListAsync();

            ViewBag.Children = children;

            if (children.Count == 0) return View("~/Views/Dashboard/Parent/Parent/Activities.cshtml", new List<StudentActivity>());

            var targetStudentId = studentId ?? children.First().Id;
            if (!children.Any(c => c.Id == targetStudentId)) return Forbid();

            var activities = await _context.StudentActivities
                .Include(sa => sa.Activity).ThenInclude(a => a.Location)
                .Include(sa => sa.Activity).ThenInclude(a => a.Organizer)
                .Where(sa => sa.StudentId == targetStudentId)
                .OrderByDescending(sa => sa.Activity.Date)
                .ToListAsync();

            ViewBag.SelectedStudentId = targetStudentId;
            return View("~/Views/Dashboard/Parent/Parent/Activities.cshtml", activities);
        }

        [HttpGet("Api/DashboardStats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var parentId = await GetCurrentParentId();
            if (parentId == null) return Unauthorized();

            var childrenIds = await _context.ParentStudents
                .Where(ps => ps.ParentId == parentId)
                .Select(ps => ps.StudentId)
                .ToListAsync();

            var childrenCount = childrenIds.Count;

            var unpaidTuitionsCount = await _context.Tuitions
                .Where(t => childrenIds.Contains(t.StudentId.Value) && t.PaymentStatus != "Paid")
                .CountAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayAttendances = await _context.Attendances
                .Where(a => childrenIds.Contains(a.StudentId) && a.Date == today)
                .Select(a => new { a.StudentId, a.Status })
                .ToListAsync();

            int presentCount = todayAttendances.Count(a => a.Status == "Present");
            int totalAttendancesRecorded = todayAttendances.Count;

            return Ok(new
            {
                success = true,
                data = new
                {
                    childrenCount = childrenCount,
                    unpaidTuitions = unpaidTuitionsCount,
                    presentCount = presentCount,
                    totalRecorded = totalAttendancesRecorded
                }
            });
        }

        private static string ExtractReason(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "Kh\u00f4ng c\u00f3 l\u00fd do c\u1ee5 th\u1ec3.";

            foreach (var marker in new[] { "L\u00fd do:", "LÃ½ do:" })
            {
                var index = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index < 0) continue;

                var reason = message[(index + marker.Length)..].Trim();
                var dotIndex = reason.IndexOf('.');
                if (dotIndex >= 0) reason = reason[..dotIndex].Trim();
                if (!string.IsNullOrWhiteSpace(reason)) return reason;
            }

            return message.Trim();
        }

        private static string ToParentFacingReason(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return "Gi\u00e1o vi\u00ean ph\u1ee5 tr\u00e1ch c\u00f3 vi\u1ec7c \u0111\u1ed9t xu\u1ea5t";

            var normalized = reason.Trim().ToLowerInvariant();
            if (normalized.Contains("kh\u00f4ng ph\u00e9p") || normalized.Contains("khong phep") || normalized.Contains("check-in"))
                return "Gi\u00e1o vi\u00ean ph\u1ee5 tr\u00e1ch c\u00f3 vi\u1ec7c \u0111\u1ed9t xu\u1ea5t";

            return reason.Trim();
        }

        private static string GetHolidayStatusText(DateOnly date)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (date == today) return "H\u00f4m nay";
            return date < today ? "\u0110\u00e3 qua" : "S\u1eafp t\u1edbi";
        }
    }
}
