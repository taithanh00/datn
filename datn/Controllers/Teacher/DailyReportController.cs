using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace datn.Controllers.Teacher
{
    [Authorize]
    public class DailyReportController : BaseController
    {
        private readonly IDailyReportService _reportService;
        private readonly IHealthService _healthService;
        private readonly INutritionService _nutritionService;

        public DailyReportController(
            AppDbContext context,
            IDailyReportService reportService,
            IHealthService healthService,
            INutritionService nutritionService) : base(context)
        {
            _reportService = reportService;
            _healthService = healthService;
            _nutritionService = nutritionService;
        }

        // ────── EMPLOYEE VIEWS ──────────────────────────────────────

        [Authorize(Policy = "EmployeeOnly")]
        [HttpGet("/Employee/ClassReports")]
        public IActionResult ClassReports()
        {
            ViewData["Title"] = "Nhật ký lớp học";
            return View("~/Views/Dashboard/Teacher/DailyReport/ClassReports.cshtml");
        }

        [Authorize(Policy = "EmployeeOnly")]
        [HttpGet]
        public async Task<IActionResult> GetMyClasses()
        {
            var employeeId = await GetCurrentEmployeeId();
            if (employeeId == 0) return Json(new List<object>());

            var today = DateOnly.FromDateTime(DateTime.Now);

            var classes = await _context.Assignments
                .Include(a => a.Class)
                .Where(a => a.EmployeeId == employeeId && a.IsActive && 
                           (a.EndDate == null || a.EndDate >= today))
                .Select(a => new { id = a.ClassId, name = a.Class.Name })
                .Distinct()
                .OrderBy(c => c.name)
                .ToListAsync();

            return Json(classes);
        }

        [Authorize(Policy = "EmployeeOnly")]
        [HttpGet]
        public async Task<IActionResult> GetClassDailyData(int classId, string date)
        {
            if (!await CanAccessClass(classId)) return Forbid();

            if (!DateOnly.TryParse(date, out var d)) d = DateOnly.FromDateTime(DateTime.Now);

            var students = await _context.Students
                .Where(s => s.ClassId == classId)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Select(s => new { s.Id, s.FirstName, s.LastName, s.Allergies, s.AvatarPath })
                .ToListAsync();

            var reports = await _context.DailyReports
                .Where(r => r.Student.ClassId == classId && r.Date == d)
                .ToListAsync();

            var healthRecords = await _context.HealthRecords
                .Where(h => h.Date == d && students.Select(s => s.Id).Contains(h.StudentId))
                .ToListAsync();

            return Json(new { students, reports, healthRecords });
        }

        [Authorize(Policy = "EmployeeOnly")]
        [HttpPost]
        public async Task<IActionResult> SaveStudentReport([FromBody]   SaveReportDto dto)
        {
            if (dto == null) return BadRequest();

            // GVBM chỉ được xem, không được sửa nhật ký ăn ngủ
            if (!await CanEditClass(dto.ClassId))
                return Json(new { success = false, message = "Chỉ giáo viên phụ trách mới có quyền ghi nhật ký lớp học." });

            if (!DateOnly.TryParse(dto.Date, out var d)) d = DateOnly.FromDateTime(DateTime.Now);

            var report = await _context.DailyReports
                .FirstOrDefaultAsync(r => r.StudentId == dto.StudentId && r.Date == d);

            if (report == null)
            {
                report = new DailyReport { StudentId = dto.StudentId, Date = d };
                _context.DailyReports.Add(report);
            }

            report.EatingStatus = (EatingStatus)dto.EatingStatus;
            report.EatingNote = dto.EatingNote;
            report.SleepingStatus = (SleepingStatus)dto.SleepingStatus;
            report.SleepingNote = dto.SleepingNote;
            report.MoodNote = dto.MoodNote;
            report.ActivityNote = dto.ActivityNote;

            await _context.SaveChangesAsync();

            if (dto.Temperature.HasValue || dto.Weight.HasValue || dto.Height.HasValue)
            {
                var health = await _context.HealthRecords
                    .FirstOrDefaultAsync(h => h.StudentId == dto.StudentId && h.Date == d);
                
                if (health == null)
                {
                    health = new HealthRecord { StudentId = dto.StudentId, Date = d };
                    _context.HealthRecords.Add(health);
                }
                health.Temperature = dto.Temperature.HasValue ? (decimal)dto.Temperature.Value : null;
                health.Weight = dto.Weight.HasValue ? (decimal)dto.Weight.Value : null;
                health.Height = dto.Height.HasValue ? (decimal)dto.Height.Value : null;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // ────── PRIVATE HELPERS ──────────────────────────────────────

        private async Task<int> GetCurrentEmployeeId()
        {
            var accountIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountIdStr)) return 0;
            var accountId = int.Parse(accountIdStr);
            var emp = await _context.Employees.FirstOrDefaultAsync(e => e.AccountId == accountId);
            return emp?.Id ?? 0;
        }

        // Giáo viên phụ trách có thể xem nhật ký lớp.
        private async Task<bool> CanViewClass(int classId)
        {
            var employeeId = await GetCurrentEmployeeId();
            if (employeeId == 0) return false;
            var today = DateOnly.FromDateTime(DateTime.Now);
            return await _context.Assignments.AnyAsync(a =>
                a.EmployeeId == employeeId &&
                a.ClassId == classId &&
                a.IsActive &&
                a.StartDate <= today &&
                (a.EndDate == null || a.EndDate >= today));
        }

        // Giáo viên phụ trách có thể chỉnh sửa nhật ký lớp.
        private async Task<bool> CanEditClass(int classId)
        {
            var employeeId = await GetCurrentEmployeeId();
            if (employeeId == 0) return false;

            var today = GetTodayVnt();
            return await _context.Assignments.AnyAsync(a =>
                a.EmployeeId == employeeId &&
                a.ClassId == classId &&
                a.IsActive &&
                a.StartDate <= today &&
                (a.EndDate == null || a.EndDate >= today));
        }

        // Giữ lại cho tương thích ngược
        private Task<bool> CanAccessClass(int classId) => CanViewClass(classId);

        // ────── PARENT VIEWS ────────────────────────────────────────

        [Authorize(Policy = "ParentOnly")]
        [HttpGet("/Parent/DailyReport/MyChild")]
        public async Task<IActionResult> MyChild()
        {
            var accountIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountIdStr)) return View("~/Views/Dashboard/Parent/DailyReport/MyChild.cshtml");
            var accountId = int.Parse(accountIdStr);
            
            var parent = await _context.Parents.FirstOrDefaultAsync(p => p.AccountId == accountId);
            if (parent != null)
            {
                var children = await _context.ParentStudents
                    .Include(ps => ps.Student)
                    .Where(ps => ps.ParentId == parent.Id)
                    .Select(ps => new { ps.Student.Id, ps.Student.FirstName, ps.Student.LastName })
                    .ToListAsync();
                ViewBag.Children = children;
                ViewBag.DefaultStudentId = children.FirstOrDefault()?.Id;
            }

            ViewData["Title"] = "Nhật ký của bé";
            return View("~/Views/Dashboard/Parent/DailyReport/MyChild.cshtml");
        }

        [Authorize(Policy = "ParentOnly")]
        [HttpGet]
        public async Task<IActionResult> GetChildDailyLog(int studentId, string date)
        {
            // Security: Check if parent is linked to this student
            var accountIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountIdStr)) return Forbid();
            var accountId = int.Parse(accountIdStr);
            var parent = await _context.Parents.FirstOrDefaultAsync(p => p.AccountId == accountId);
            if (parent == null || !await _context.ParentStudents.AnyAsync(ps => ps.ParentId == parent.Id && ps.StudentId == studentId))
            {
                return Forbid();
            }

            if (!DateOnly.TryParse(date, out var d)) d = DateOnly.FromDateTime(DateTime.Now);

            var report = await _reportService.GetReportAsync(studentId, d);
            var health = await _context.HealthRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(hr => hr.StudentId == studentId && hr.Date == d);
            var history = await _healthService.GetHistoryAsync(studentId);

            return Json(new { report, health, history });
        }
    }

    public class SaveReportDto
    {
        public int ReportId { get; set; }
        public int StudentId { get; set; }
        public int ClassId { get; set; }
        public string Date { get; set; } = string.Empty;
        public int EatingStatus { get; set; }
        public string? EatingNote { get; set; }
        public int SleepingStatus { get; set; }
        public string? SleepingNote { get; set; }
        public string? MoodNote { get; set; }
        public string? ActivityNote { get; set; }
        public double? Temperature { get; set; }
        public double? Weight { get; set; }
        public double? Height { get; set; }
    }
}

