using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace datn.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class TeacherSalaryController : BaseController
    {
        private readonly INotificationService _notificationService;

        public TeacherSalaryController(AppDbContext context, INotificationService notificationService) : base(context)
        {
            _notificationService = notificationService;
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Tính lương Giáo viên";
            return View();
        }

        [Authorize(Roles = "Employee")]
        [HttpGet("MySalary")]
        public async Task<IActionResult> MySalary()
        {
            ViewData["Title"] = "Lương của tôi";
            var username = User.Identity?.Name;
            var employee = await _context.Employees
                .Include(e => e.Account)
                .FirstOrDefaultAsync(e => e.Account.Username == username);

            if (employee == null) return NotFound();

            var salaries = await _context.Salaries
                .Include(s => s.PayrollPeriod)
                .Where(s => s.EmployeeId == employee.Id)
                .OrderByDescending(s => s.PayrollPeriod.Year)
                .ThenByDescending(s => s.PayrollPeriod.Month)
                .ToListAsync();

            return View(salaries);
        }

        [Authorize]
        [HttpGet("SalarySlip/{employeeId:int}/{periodId:int}")]
        public async Task<IActionResult> SalarySlip(int employeeId, int periodId)
        {
            var currentUsername = User.Identity?.Name;
            var currentRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Nếu không phải Manager, chỉ được xem lương của chính mình
            if (currentRole != "Manager")
            {
                var me = await _context.Employees.Include(e => e.Account)
                    .FirstOrDefaultAsync(e => e.Account.Username == currentUsername);
                if (me == null || me.Id != employeeId) return Forbid();
            }

            var salary = await _context.Salaries
                .Include(s => s.Employee)
                .Include(s => s.PayrollPeriod)
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.PayrollPeriodId == periodId);

            if (salary == null) return NotFound();

            // Lấy chi tiết các ngày công bị phạt để hiển thị trong phiếu lương
            var penalties = await _context.WorkAttendances
                .Where(w => w.EmployeeId == employeeId 
                            && w.Date.Month == salary.PayrollPeriod.Month 
                            && w.Date.Year == salary.PayrollPeriod.Year
                            && w.PenaltyAmount > 0)
                .OrderBy(w => w.Date)
                .ToListAsync();

            ViewBag.Penalties = penalties;
            return View(salary);
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("Api/Summary")]
        public async Task<IActionResult> Summary(int? month, int? year)
        {
            var nowVnt = GetVntNow();
            var targetMonth = month ?? nowVnt.Month;
            var targetYear = year ?? nowVnt.Year;

            var period = await EnsurePayrollPeriodAsync(targetMonth, targetYear);

            var data = await _context.Salaries
                .Where(s => s.PayrollPeriodId == period.Id)
                .Include(s => s.Employee).ThenInclude(e => e.Account)
                .OrderBy(s => s.Employee.FullName)
                .Select(s => new
                {
                    employeeId = s.EmployeeId,
                    employeeName = s.Employee.FullName,
                    baseSalary = s.Employee.BaseSalary ?? 0,
                    workingDays = s.WorkingDays ?? 0,
                    salaryAmount = s.SalaryAmount ?? 0
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                data,
                month = targetMonth,
                year = targetYear,
                periodId = period.Id,
                isLocked = period.IsLocked,
                lockedAtUtc = period.LockedAtUtc
            });
        }

        [HttpPost("Api/Recalculate")]
        public async Task<IActionResult> Recalculate([FromBody] PayrollRequestDto model)
        {
            if (model.Month is < 1 or > 12 || model.Year < 2000)
                return Json(new { success = false, message = "Tháng hoặc năm không hợp lệ." });

            var period = await EnsurePayrollPeriodAsync(model.Month, model.Year);
            if (period.IsLocked)
                return Json(new { success = false, message = $"Kỳ lương {model.Month}/{model.Year} đã chốt, không thể tính lại." });

            await CalculatePayrollForPeriodAsync(model.Month, model.Year);
            return Json(new { success = true, message = $"Đã tính lại lương cho {model.Month}/{model.Year}." });
        }

        [HttpPost("Api/Lock")]
        public async Task<IActionResult> Lock([FromBody] PayrollRequestDto model)
        {
            if (model.Month is < 1 or > 12 || model.Year < 2000)
                return Json(new { success = false, message = "Tháng hoặc năm không hợp lệ." });

            var period = await EnsurePayrollPeriodAsync(model.Month, model.Year);
            if (period.IsLocked)
                return Json(new { success = false, message = $"Kỳ lương {model.Month}/{model.Year} đã được chốt trước đó." });

            period.IsLocked = true;
            period.LockedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Thông báo cho toàn bộ Giáo viên
            await _notificationService.SendToRoleAsync("Employee", 
                "Bảng lương mới", 
                $"Bảng lương tháng {model.Month}/{model.Year} đã được chốt. Bạn có thể xem phiếu lương của mình ngay bây giờ.",
                "success", "/TeacherSalary/MySalary");

            return Json(new { success = true, message = $"Đã chốt kỳ lương {model.Month}/{model.Year}." });
        }

        private async Task CalculatePayrollForPeriodAsync(int month, int year)
        {
            var period = await EnsurePayrollPeriodAsync(month, year);
            if (period.IsLocked)
                return;

            var teachers = await _context.Employees
                .Include(e => e.Account).ThenInclude(a => a.Role)
                .Where(e => e.Account != null && e.Account.IsActive && e.Account.Role.Name == "Employee")
                .ToListAsync();

            foreach (var teacher in teachers)
            {
                var approvedRecords = await _context.WorkAttendances
                    .Where(w => w.EmployeeId == teacher.Id
                                && w.Status == "Approved"
                                && w.Date.Month == month
                                && w.Date.Year == year)
                    .ToListAsync();

                var workingDays = approvedRecords.Count;
                var totalPenalty = approvedRecords.Sum(w => w.PenaltyAmount);
                var baseSalary = teacher.BaseSalary ?? 0m;
                var dailyRate = baseSalary / 22m;
                var netSalary = Math.Max(0, (workingDays * dailyRate) - totalPenalty);

                var salary = await _context.Salaries
                    .FirstOrDefaultAsync(s => s.EmployeeId == teacher.Id && s.PayrollPeriodId == period.Id);

                if (salary == null)
                {
                    _context.Salaries.Add(new Salary
                    {
                        EmployeeId = teacher.Id,
                        PayrollPeriodId = period.Id,
                        WorkingDays = workingDays,
                        SalaryAmount = Math.Round(netSalary, 2, MidpointRounding.AwayFromZero)
                    });
                }
                else
                {
                    salary.WorkingDays = workingDays;
                    salary.SalaryAmount = Math.Round(netSalary, 2, MidpointRounding.AwayFromZero);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task<PayrollPeriod> EnsurePayrollPeriodAsync(int month, int year)
        {
            var period = await _context.PayrollPeriods
                .FirstOrDefaultAsync(p => p.Month == month && p.Year == year);

            if (period != null)
                return period;

            period = new PayrollPeriod
            {
                Month = month,
                Year = year
            };
            _context.PayrollPeriods.Add(period);
            await _context.SaveChangesAsync();
            return period;
        }

        private static DateTimeOffset GetVntNow()
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
            return TimeZoneInfo.ConvertTime(utcNow, tz);
        }

        public class PayrollRequestDto
        {
            public int Month { get; set; }
            public int Year { get; set; }
        }
    }
}
