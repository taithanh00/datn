using datn.Data;
using datn.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace datn.Controllers
{
    [Authorize(Roles = "Parent")]
    [Route("[controller]")]
    public class ParentController : Controller
    {
        private readonly AppDbContext _context;

        public ParentController(AppDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var username = User.Identity?.Name ?? "User";
            ViewBag.Username = username;
            ViewBag.Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            var parent = _context.Parents.Include(p => p.Account)
                .FirstOrDefault(p => p.Account.Username == username);
            ViewBag.UserAvatar = "/images/lion_orange.png"; // Mặc định cho Parent

            base.OnActionExecuting(context);
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

            return View(students);
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
                .Where(ps => ps.ParentId == parentId)
                .Select(ps => ps.Student)
                .ToListAsync();

            ViewBag.Children = children;

            if (children.Count == 0) return View(new List<StudyReport>());

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

            return View(reports);
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

            if (children.Count == 0) return View(new List<Attendance>());

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

            return View(attendances);
        }
    }
}
