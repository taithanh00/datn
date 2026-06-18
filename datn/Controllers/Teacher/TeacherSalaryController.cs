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
    [Route("[controller]")]
    public class TeacherSalaryController : BaseController
    {
        private readonly IPayrollService _payrollService;

        public TeacherSalaryController(AppDbContext context, IPayrollService payrollService) : base(context)
        {
            _payrollService = payrollService;
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Quản lý tiền lương";
            return View("~/Views/Dashboard/Admin/TeacherSalary/Index.cshtml");
        }

        [Authorize(Roles = "Employee")]
        [HttpGet("MySalary")]
        [HttpGet("/Employee/Salary")]
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
                .Where(s => s.Status == SalaryStatus.Locked || s.Status == SalaryStatus.Paid)
                .OrderByDescending(s => s.PayrollPeriod.Year)
                .ThenByDescending(s => s.PayrollPeriod.Month)
                .ToListAsync();

            return View("~/Views/Dashboard/Teacher/TeacherSalary/MySalary.cshtml", salaries);
        }

        [Authorize]
        [HttpGet("SalarySlip/{employeeId:int}/{periodId:int}")]
        public async Task<IActionResult> SalarySlip(int employeeId, int periodId)
        {
            var currentUsername = User.Identity?.Name;
            var currentRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (currentRole != "Manager")
            {
                var me = await _context.Employees
                    .Include(e => e.Account)
                    .FirstOrDefaultAsync(e => e.Account.Username == currentUsername);
                if (me == null || me.Id != employeeId) return Forbid();
            }

            var salary = await _context.Salaries
                .Include(s => s.Employee)
                .Include(s => s.PayrollPeriod)
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.PayrollPeriodId == periodId);

            if (salary == null) return NotFound();

            if (currentRole != "Manager" && salary.Status is not (SalaryStatus.Locked or SalaryStatus.Paid))
            {
                return Forbid();
            }

            var month = salary.PayrollPeriod.Month ?? 0;
            var year = salary.PayrollPeriod.Year ?? 0;

            ViewBag.Penalties = await _context.WorkAttendances
                .Where(w => w.EmployeeId == employeeId
                            && w.Date.Month == month
                            && w.Date.Year == year
                            && w.PenaltyAmount > 0)
                .OrderBy(w => w.Date)
                .ToListAsync();

            ViewBag.CoverageBonuses = await _context.ClassCoverageBonuses
                .Include(b => b.Class)
                .Where(b => b.EmployeeId == employeeId
                            && b.Status == "Active"
                            && b.Date.Month == month
                            && b.Date.Year == year)
                .OrderBy(b => b.Date)
                .ToListAsync();

            return View("~/Views/Dashboard/Teacher/TeacherSalary/SalarySlip.cshtml", salary);
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("Api/Summary")]
        public async Task<IActionResult> Summary(int? month, int? year, string? status = null)
        {
            var nowVnt = GetVntNow();
            var targetMonth = month ?? nowVnt.Month;
            var targetYear = year ?? nowVnt.Year;
            var standardWorkingDays = _payrollService.CountWorkingDays(targetMonth, targetYear);
            var period = await _payrollService.EnsurePeriodAsync(targetMonth, targetYear);

            var query = _context.Salaries
                .Where(s => s.PayrollPeriodId == period.Id)
                .Include(s => s.Employee).ThenInclude(e => e.Account)
                .AsQueryable();

            if (Enum.TryParse<SalaryStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(s => s.Status == parsedStatus);
            }

            var rows = await query
                .OrderBy(s => s.Employee.FirstName).ThenBy(s => s.Employee.LastName)
                .Select(s => new
                {
                    employeeId = s.EmployeeId,
                    periodId = s.PayrollPeriodId,
                    employeeName = s.Employee.LastName + " " + s.Employee.FirstName,
                    baseSalary = s.BaseSalarySnapshot.HasValue && s.BaseSalarySnapshot.Value > 0
                        ? s.BaseSalarySnapshot.Value
                        : (s.Employee.BaseSalary ?? 0),
                    standardWorkingDays = s.StandardWorkingDays ?? standardWorkingDays,
                    workingDays = s.WorkingDays ?? 0,
                    penaltyAmount = s.PenaltyAmount,
                    coverageBonusAmount = s.CoverageBonusAmount,
                    salaryAmount = s.SalaryAmount ?? 0,
                    status = s.Status.ToString(),
                    calculatedAtUtc = s.CalculatedAtUtc,
                    lockedAtUtc = s.LockedAtUtc,
                    paidAtUtc = s.PaidAtUtc,
                    paymentMethod = s.PaymentMethod,
                    paymentNote = s.PaymentNote
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                data = rows,
                summary = new
                {
                    totalTeachers = rows.Count,
                    calculated = rows.Count(x => x.status == SalaryStatus.Calculated.ToString()),
                    locked = rows.Count(x => x.status == SalaryStatus.Locked.ToString()),
                    paid = rows.Count(x => x.status == SalaryStatus.Paid.ToString()),
                    totalAmount = rows.Sum(x => x.salaryAmount),
                    totalPenalty = rows.Sum(x => x.penaltyAmount),
                    totalBonus = rows.Sum(x => x.coverageBonusAmount)
                },
                month = targetMonth,
                year = targetYear,
                periodId = period.Id,
                isLocked = period.IsLocked,
                lockedAtUtc = period.LockedAtUtc
            });
        }

        [Authorize(Roles = "Manager")]
        [HttpPost("Api/Recalculate")]
        public async Task<IActionResult> Recalculate([FromBody] PayrollRequestDto model)
        {
            var error = ValidatePeriod(model.Month, model.Year);
            if (error != null) return Json(new { success = false, message = error });

            var period = await _payrollService.EnsurePeriodAsync(model.Month, model.Year);
            if (period.IsLocked)
                return Json(new { success = false, message = $"Kỳ lương {model.Month}/{model.Year} đã chốt, không thể tính lại." });

            await _payrollService.CalculatePeriodAsync(model.Month, model.Year);
            return Json(new { success = true, message = $"Đã tính lại lương cho {model.Month}/{model.Year}." });
        }

        [Authorize(Roles = "Manager")]
        [HttpPost("Api/RecalculateEmployee")]
        public async Task<IActionResult> RecalculateEmployee([FromBody] EmployeePayrollRequestDto model)
        {
            var error = ValidatePeriod(model.Month, model.Year);
            if (error != null) return Json(new { success = false, message = error });

            var salary = await _payrollService.CalculateEmployeeAsync(model.EmployeeId, model.Month, model.Year);
            if (salary == null)
                return Json(new { success = false, message = "Không thể tính lại lương. Kỳ lương đã chốt hoặc giáo viên không tồn tại." });

            return Json(new { success = true, message = "Đã tính lại lương giáo viên." });
        }

        [Authorize(Roles = "Manager")]
        [HttpPost("Api/LockSalary")]
        public async Task<IActionResult> LockSalary([FromBody] EmployeePayrollRequestDto model)
        {
            var error = ValidatePeriod(model.Month, model.Year);
            if (error != null) return Json(new { success = false, message = error });

            var success = await _payrollService.LockEmployeeSalaryAsync(model.EmployeeId, model.Month, model.Year);
            return Json(new
            {
                success,
                message = success ? "Đã chốt lương và gửi thông báo cho giáo viên." : "Không thể chốt dòng lương này."
            });
        }

        [Authorize(Roles = "Manager")]
        [HttpPost("Api/Lock")]
        public async Task<IActionResult> Lock([FromBody] PayrollRequestDto model)
        {
            var error = ValidatePeriod(model.Month, model.Year);
            if (error != null) return Json(new { success = false, message = error });

            var count = await _payrollService.LockPeriodAsync(model.Month, model.Year);
            return Json(new { success = true, message = $"Đã chốt {count} dòng lương kỳ {model.Month}/{model.Year}." });
        }

        [Authorize(Roles = "Manager")]
        [HttpPost("Api/MarkPaid")]
        public async Task<IActionResult> MarkPaid([FromBody] MarkPaidRequestDto model)
        {
            var error = ValidatePeriod(model.Month, model.Year);
            if (error != null) return Json(new { success = false, message = error });

            var success = await _payrollService.MarkPaidAsync(
                model.EmployeeId,
                model.Month,
                model.Year,
                model.PaymentMethod,
                model.Note);

            if (!success)
                return Json(new { success = false, message = "Vui lòng chốt lương giáo viên trước khi ghi nhận đã trả." });

            return Json(new
            {
                success,
                message = success ? "Đã ghi nhận thanh toán và gửi thông báo cho giáo viên." : "Không thể ghi nhận thanh toán."
            });
        }

        [Authorize(Roles = "Manager")]
        [HttpPost("Api/MarkPaidAll")]
        public async Task<IActionResult> MarkPaidAll([FromBody] PayrollRequestDto model)
        {
            var error = ValidatePeriod(model.Month, model.Year);
            if (error != null) return Json(new { success = false, message = error });

            var count = await _payrollService.MarkPeriodPaidAsync(model.Month, model.Year);
            if (count == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Không có dòng lương đã chốt nào để ghi nhận thanh toán."
                });
            }

            return Json(new
            {
                success = true,
                message = $"Đã ghi nhận thanh toán cho {count} dòng lương đã chốt."
            });
        }

        private static string? ValidatePeriod(int month, int year)
        {
            if (month is < 1 or > 12 || year < 2000) return "Tháng hoặc năm không hợp lệ.";
            return null;
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

        public class EmployeePayrollRequestDto : PayrollRequestDto
        {
            public int EmployeeId { get; set; }
        }

        public class MarkPaidRequestDto : EmployeePayrollRequestDto
        {
            public string? PaymentMethod { get; set; }
            public string? Note { get; set; }
        }
    }
}
