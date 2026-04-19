using datn.Data;
using datn.Models;
using Microsoft.EntityFrameworkCore;

namespace datn.Services
{
    public class PayrollAutoCalculationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private DateOnly? _lastRunDateVnt;

        public PayrollAutoCalculationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var nowVnt = GetVntNow();
                    var todayVnt = DateOnly.FromDateTime(nowVnt.DateTime);

                    if (nowVnt.Day == 5 && _lastRunDateVnt != todayVnt)
                    {
                        await CalculatePayrollForPreviousMonthAsync(stoppingToken);
                        _lastRunDateVnt = todayVnt;
                    }
                }
                catch
                {
                    // Keep background loop alive even if calculation fails.
                }

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        private async Task CalculatePayrollForPreviousMonthAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var nowVnt = GetVntNow();
            var target = nowVnt.AddMonths(-1);
            var targetMonth = target.Month;
            var targetYear = target.Year;

            var period = await db.PayrollPeriods
                .FirstOrDefaultAsync(p => p.Month == targetMonth && p.Year == targetYear, cancellationToken);

            if (period == null)
            {
                period = new PayrollPeriod
                {
                    Month = targetMonth,
                    Year = targetYear
                };
                db.PayrollPeriods.Add(period);
                await db.SaveChangesAsync(cancellationToken);
            }

            if (period.IsLocked)
            {
                return;
            }

            var employees = await db.Employees
                .Include(e => e.Account)
                .Where(e => e.Account != null && e.Account.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var employee in employees)
            {
                var approvedRecords = await db.WorkAttendances
                    .Where(w => w.EmployeeId == employee.Id
                                && w.Status == "Approved"
                                && w.Date.Month == targetMonth
                                && w.Date.Year == targetYear)
                    .ToListAsync(cancellationToken);

                var workingDays = approvedRecords.Count;
                var totalPenalty = approvedRecords.Sum(w => w.PenaltyAmount);
                var baseSalary = employee.BaseSalary ?? 0m;
                var dailyRate = baseSalary / 22m;
                var calculatedSalary = Math.Max(0, (workingDays * dailyRate) - totalPenalty);

                var salary = await db.Salaries.FirstOrDefaultAsync(
                    s => s.EmployeeId == employee.Id && s.PayrollPeriodId == period.Id,
                    cancellationToken);

                if (salary == null)
                {
                    db.Salaries.Add(new Salary
                    {
                        EmployeeId = employee.Id,
                        PayrollPeriodId = period.Id,
                        WorkingDays = workingDays,
                        SalaryAmount = Math.Round(calculatedSalary, 2, MidpointRounding.AwayFromZero)
                    });
                }
                else
                {
                    salary.WorkingDays = workingDays;
                    salary.SalaryAmount = Math.Round(calculatedSalary, 2, MidpointRounding.AwayFromZero);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
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
    }
}
