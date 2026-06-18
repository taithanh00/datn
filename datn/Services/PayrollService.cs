using datn.Data;
using datn.Models;
using Microsoft.EntityFrameworkCore;

namespace datn.Services
{
    public interface IPayrollService
    {
        Task<PayrollPeriod> EnsurePeriodAsync(int month, int year);
        Task CalculatePeriodAsync(int month, int year, CancellationToken cancellationToken = default);
        Task<Salary?> CalculateEmployeeAsync(int employeeId, int month, int year, CancellationToken cancellationToken = default);
        Task<bool> LockEmployeeSalaryAsync(int employeeId, int month, int year, CancellationToken cancellationToken = default);
        Task<int> LockPeriodAsync(int month, int year, CancellationToken cancellationToken = default);
        Task<bool> MarkPaidAsync(int employeeId, int month, int year, string? paymentMethod, string? note, CancellationToken cancellationToken = default);
        Task<int> MarkPeriodPaidAsync(int month, int year, CancellationToken cancellationToken = default);
        int CountWorkingDays(int month, int year);
    }

    public class PayrollService : IPayrollService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public PayrollService(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<PayrollPeriod> EnsurePeriodAsync(int month, int year)
        {
            var period = await _context.PayrollPeriods
                .FirstOrDefaultAsync(p => p.Month == month && p.Year == year);

            if (period != null) return period;

            period = new PayrollPeriod
            {
                Month = month,
                Year = year,
                IsLocked = false
            };
            _context.PayrollPeriods.Add(period);
            await _context.SaveChangesAsync();
            return period;
        }

        public async Task CalculatePeriodAsync(int month, int year, CancellationToken cancellationToken = default)
        {
            var period = await EnsurePeriodAsync(month, year);
            if (period.IsLocked) return;

            var teachers = await _context.Employees
                .IgnoreQueryFilters()
                .Include(e => e.Account).ThenInclude(a => a.Role)
                .Where(e => e.Account != null && e.Account.Role.Name == "Employee")
                .Where(e => e.Account.IsActive || _context.WorkAttendances.Any(w =>
                    w.EmployeeId == e.Id && w.Date.Month == month && w.Date.Year == year))
                .ToListAsync(cancellationToken);

            foreach (var teacher in teachers)
            {
                await CalculateEmployeeCoreAsync(teacher, period, month, year, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Salary?> CalculateEmployeeAsync(int employeeId, int month, int year, CancellationToken cancellationToken = default)
        {
            var period = await EnsurePeriodAsync(month, year);
            if (period.IsLocked) return null;

            var teacher = await _context.Employees
                .IgnoreQueryFilters()
                .Include(e => e.Account).ThenInclude(a => a.Role)
                .FirstOrDefaultAsync(e => e.Id == employeeId && e.Account.Role.Name == "Employee", cancellationToken);

            if (teacher == null) return null;

            var salary = await CalculateEmployeeCoreAsync(teacher, period, month, year, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return salary;
        }

        public async Task<bool> LockEmployeeSalaryAsync(int employeeId, int month, int year, CancellationToken cancellationToken = default)
        {
            var period = await EnsurePeriodAsync(month, year);
            var salary = await _context.Salaries
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.PayrollPeriodId == period.Id, cancellationToken);

            if (salary == null || salary.Status == SalaryStatus.Paid || salary.Status == SalaryStatus.Cancelled)
            {
                return false;
            }

            if (salary.Status != SalaryStatus.Locked)
            {
                salary.Status = SalaryStatus.Locked;
                salary.LockedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            await NotifySalaryLockedAsync(salary.Employee.AccountId, month, year);
            return true;
        }

        public async Task<int> LockPeriodAsync(int month, int year, CancellationToken cancellationToken = default)
        {
            var period = await EnsurePeriodAsync(month, year);
            var salaries = await _context.Salaries
                .Include(s => s.Employee)
                .Where(s => s.PayrollPeriodId == period.Id
                    && s.Status != SalaryStatus.Paid
                    && s.Status != SalaryStatus.Cancelled)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var salary in salaries)
            {
                salary.Status = SalaryStatus.Locked;
                salary.LockedAtUtc ??= now;
            }

            period.IsLocked = true;
            period.LockedAtUtc ??= now;
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var salary in salaries)
            {
                await NotifySalaryLockedAsync(salary.Employee.AccountId, month, year);
            }

            return salaries.Count;
        }

        public async Task<bool> MarkPaidAsync(int employeeId, int month, int year, string? paymentMethod, string? note, CancellationToken cancellationToken = default)
        {
            var period = await EnsurePeriodAsync(month, year);
            var salary = await _context.Salaries
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.PayrollPeriodId == period.Id, cancellationToken);

            if (salary == null || salary.Status != SalaryStatus.Locked)
            {
                return false;
            }

            salary.Status = SalaryStatus.Paid;
            salary.PaidAtUtc = DateTime.UtcNow;
            salary.PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Khác" : paymentMethod.Trim();
            salary.PaymentNote = note?.Trim();

            await _context.SaveChangesAsync(cancellationToken);
            await _notificationService.SendToUserAsync(
                salary.Employee.AccountId,
                $"Lương tháng {month:D2}/{year} đã được thanh toán",
                $"Lương tháng {month:D2}/{year} của bạn đã được ghi nhận thanh toán qua {salary.PaymentMethod}.",
                "success",
                "/TeacherSalary/MySalary");

            return true;
        }

        public async Task<int> MarkPeriodPaidAsync(int month, int year, CancellationToken cancellationToken = default)
        {
            var period = await EnsurePeriodAsync(month, year);
            var salaries = await _context.Salaries
                .Include(s => s.Employee)
                .Where(s => s.PayrollPeriodId == period.Id && s.Status == SalaryStatus.Locked)
                .ToListAsync(cancellationToken);

            if (salaries.Count == 0)
            {
                return 0;
            }

            var now = DateTime.UtcNow;
            foreach (var salary in salaries)
            {
                salary.Status = SalaryStatus.Paid;
                salary.PaidAtUtc = now;
                salary.PaymentMethod = "Chuyển khoản";
                salary.PaymentNote = null;
            }

            await _context.SaveChangesAsync(cancellationToken);

            foreach (var salary in salaries)
            {
                await _notificationService.SendToUserAsync(
                    salary.Employee.AccountId,
                    $"Lương tháng {month:D2}/{year} đã được thanh toán",
                    $"Lương tháng {month:D2}/{year} của bạn đã được ghi nhận thanh toán qua {salary.PaymentMethod}.",
                    "success",
                    "/TeacherSalary/MySalary");
            }

            return salaries.Count;
        }

        public int CountWorkingDays(int month, int year)
        {
            var days = DateTime.DaysInMonth(year, month);
            var count = 0;
            for (var day = 1; day <= days; day++)
            {
                var date = new DateTime(year, month, day);
                if (date.DayOfWeek != DayOfWeek.Sunday) count++;
            }
            return count;
        }

        private async Task<Salary> CalculateEmployeeCoreAsync(Employee teacher, PayrollPeriod period, int month, int year, CancellationToken cancellationToken)
        {
            var salary = await _context.Salaries
                .FirstOrDefaultAsync(s => s.EmployeeId == teacher.Id && s.PayrollPeriodId == period.Id, cancellationToken);

            if (salary is { Status: SalaryStatus.Locked or SalaryStatus.Paid })
            {
                return salary;
            }

            var approvedRecords = await _context.WorkAttendances
                .Where(w => w.EmployeeId == teacher.Id
                            && w.Status == "Approved"
                            && w.Date.Month == month
                            && w.Date.Year == year)
                .ToListAsync(cancellationToken);

            foreach (var attendance in approvedRecords)
            {
                WorkAttendanceCalculator.EnsurePayrollValues(attendance);
            }

            var workingDays = approvedRecords.Sum(w => (decimal?)w.WorkUnit) ?? 0m;
            var totalPenalty = approvedRecords.Sum(w => w.PenaltyAmount);
            var totalBonus = await _context.ClassCoverageBonuses
                .Where(b => b.EmployeeId == teacher.Id
                            && b.Status == "Active"
                            && b.Date.Month == month
                            && b.Date.Year == year)
                .SumAsync(b => b.Amount, cancellationToken);

            var standardWorkingDays = CountWorkingDays(month, year);
            var baseSalary = teacher.BaseSalary ?? 0m;
            var dailyRate = standardWorkingDays == 0 ? 0m : baseSalary / standardWorkingDays;
            var finalAmount = Math.Max(0, (workingDays * dailyRate) - totalPenalty + totalBonus);

            if (salary == null)
            {
                salary = new Salary
                {
                    EmployeeId = teacher.Id,
                    PayrollPeriodId = period.Id
                };
                _context.Salaries.Add(salary);
            }

            salary.WorkingDays = workingDays;
            salary.SalaryAmount = Math.Round(finalAmount, 0, MidpointRounding.AwayFromZero);
            salary.Status = SalaryStatus.Calculated;
            salary.BaseSalarySnapshot = baseSalary;
            salary.StandardWorkingDays = standardWorkingDays;
            salary.PenaltyAmount = totalPenalty;
            salary.CoverageBonusAmount = totalBonus;
            salary.CalculatedAtUtc = DateTime.UtcNow;

            return salary;
        }

        private Task NotifySalaryLockedAsync(int accountId, int month, int year)
        {
            return _notificationService.SendToUserAsync(
                accountId,
                $"Lương tháng {month:D2}/{year} đã được chốt",
                $"Lương tháng {month:D2}/{year} của bạn đã được chốt. Vui lòng xem phiếu lương trong mục Lương của tôi.",
                "success",
                "/TeacherSalary/MySalary");
        }
    }
}
