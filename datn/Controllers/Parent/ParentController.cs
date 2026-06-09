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
    }
}
