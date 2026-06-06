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
        public ManagerController(AppDbContext context) : base(context)
        {
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Bảng điều khiển Quản lý";
            return View("~/Views/Dashboard/Admin/Manager/Index.cshtml");
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
            var contractDeadline = today.AddDays(30);
            var activeContracts = await _context.TeacherContracts.CountAsync(c => c.Status == TeacherContractStatus.Active);
            var expiringContracts = await _context.TeacherContracts.CountAsync(c => c.Status == TeacherContractStatus.Active
                && c.ExpiryDate.HasValue
                && c.ExpiryDate.Value >= today
                && c.ExpiryDate.Value <= contractDeadline);
            var teachersWithoutContracts = await _context.Employees
                .Include(e => e.Account).ThenInclude(a => a.Role)
                .CountAsync(e => e.Account.Role.Name == "Employee" && !e.TeacherContracts.Any());

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
                    activeContracts,
                    expiringContracts,
                    teachersWithoutContracts,
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
            return View("~/Views/Dashboard/Admin/Manager/Students.cshtml");
        }

        [HttpGet("StudentDetail/{id:int}")]
        public async Task<IActionResult> StudentDetail(int id, [FromServices] IStudentService _studentService)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null) return NotFound();
            
            ViewData["Title"] = $"Hồ sơ học sinh - {student.FullName}";
            return View("~/Views/Dashboard/Admin/Manager/StudentDetail.cshtml", student);
        }

        [HttpGet("Teachers")]
        public IActionResult Teachers()
        {
            ViewData["Title"] = "Danh sách Giáo viên";
            return View("~/Views/Dashboard/Admin/Manager/Teachers.cshtml");
        }

        [HttpGet("TeacherContracts")]
        public IActionResult TeacherContracts()
        {
            ViewData["Title"] = "Quản lý hợp đồng giáo viên";
            return View("~/Views/Dashboard/Admin/Manager/TeacherContracts.cshtml");
        }

        [HttpGet("TeacherDetail/{id:int}")]
        public async Task<IActionResult> TeacherDetail(int id)
        {
            var teacher = await _context.Employees
                .Include(e => e.Account)
                .Include(e => e.Assignments).ThenInclude(a => a.Class)
                .Include(e => e.TeacherContracts)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (teacher == null) return NotFound();

            ViewData["Title"] = "Chi tiết Giáo viên";
            return View("~/Views/Dashboard/Admin/Manager/TeacherDetail.cshtml", teacher);
        }

        [HttpGet("Assignments")]
        public IActionResult Assignments()
        {
            ViewData["Title"] = "Phân công Giảng dạy";
            return View("~/Views/Dashboard/Admin/Manager/Assignments.cshtml");
        }

        [HttpGet("Classes")]
        public IActionResult Classes()
        {
            ViewData["Title"] = "Lớp học";
            return View("~/Views/Dashboard/Admin/Manager/Classes.cshtml");
        }

        [HttpGet("Subjects")]
        public IActionResult Subjects()
        {
            ViewData["Title"] = "Danh mục môn học";
            return View("~/Views/Dashboard/Admin/Manager/Subjects.cshtml");
        }

        [HttpGet("Schedules")]
        public IActionResult Schedules()
        {
            ViewData["Title"] = "Thời khóa biểu";
            return View("~/Views/Dashboard/Admin/Manager/Schedules.cshtml");
        }

        [HttpGet("Parents")]
        public IActionResult Parents()
        {
            ViewData["Title"] = "Phụ huynh";
            return View("~/Views/Dashboard/Admin/Manager/Parents.cshtml");
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
            return View("~/Views/Dashboard/Admin/Manager/ParentDetail.cshtml", parent);
        }

    }
}
