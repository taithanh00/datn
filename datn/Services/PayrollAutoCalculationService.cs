using datn.Data;
using datn.Models;
using Microsoft.EntityFrameworkCore;

namespace datn.Services
{
    public class PayrollAutoCalculationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PayrollAutoCalculationService> _logger;
        private DateOnly? _lastRunDateVnt;

        public PayrollAutoCalculationService(
            IServiceProvider serviceProvider,
            ILogger<PayrollAutoCalculationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Payroll Auto Calculation Service is starting.");

            // Chạy kiểm tra ngay khi khởi động
            await RunCalculationProcess(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var nowVnt = GetVntNow();
                    var todayVnt = DateOnly.FromDateTime(nowVnt.DateTime);

                    // Kiểm tra định kỳ hàng ngày vào lúc 01:00 sáng để cập nhật bảng lương (nếu chưa chốt)
                    if (nowVnt.Hour == 1 && _lastRunDateVnt != todayVnt)
                    {
                        await RunCalculationProcess(stoppingToken);
                        _lastRunDateVnt = todayVnt;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while executing payroll calculation process.");
                }

                // Kiểm tra mỗi 30 phút
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        private async Task RunCalculationProcess(CancellationToken stoppingToken)
        {
            var nowVnt = GetVntNow();
            
            // Quét và tính toán cho 2 tháng gần nhất (tháng hiện tại và tháng trước)
            // Tháng trước: Chắc chắn cần tính
            var lastMonth = nowVnt.AddMonths(-1);
            await CalculatePayrollAsync(lastMonth.Month, lastMonth.Year, stoppingToken);

            // Tháng hiện tại: Tính toán sơ bộ để nhân viên xem được lương tạm tính
            await CalculatePayrollAsync(nowVnt.Month, nowVnt.Year, stoppingToken);
        }

        private async Task CalculatePayrollAsync(int month, int year, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            _logger.LogInformation("Calculating payroll for {Month}/{Year}", month, year);

            var period = await db.PayrollPeriods
                .FirstOrDefaultAsync(p => p.Month == month && p.Year == year, cancellationToken);

            if (period == null)
            {
                period = new PayrollPeriod
                {
                    Month = month,
                    Year = year,
                    IsLocked = false
                };
                db.PayrollPeriods.Add(period);
                await db.SaveChangesAsync(cancellationToken);
            }

            // Nếu bảng lương đã khóa (đã chốt), không tính toán lại
            if (period.IsLocked)
            {
                _logger.LogInformation("Payroll period {Month}/{Year} is locked. Skipping.", month, year);
                return;
            }

            var totalWorkingDaysInMonth = CountWorkingDays(month, year);
            if (totalWorkingDaysInMonth == 0) return;

            var employees = await db.Employees
                .Include(e => e.Account)
                .Where(e => e.Account != null && e.Account.Role.Name == "Employee")
                .Where(e => e.Account.IsActive || db.WorkAttendances.Any(w => w.EmployeeId == e.Id && w.Date.Month == month && w.Date.Year == year))
                .ToListAsync(cancellationToken);

            foreach (var employee in employees)
            {
                var approvedRecords = await db.WorkAttendances
                    .Where(w => w.EmployeeId == employee.Id
                                && w.Status == "Approved"
                                && w.Date.Month == month
                                && w.Date.Year == year)
                    .ToListAsync(cancellationToken);

                var workingDaysCount = approvedRecords.Sum(w => (decimal?)w.WorkUnit) ?? 0m;
                var totalPenalty = approvedRecords.Sum(w => w.PenaltyAmount);

                // Nếu không đi làm ngày nào và không có tiền phạt, bỏ qua để không làm rác bảng lương
                if (workingDaysCount == 0 && totalPenalty == 0) continue;

                var baseSalary = employee.BaseSalary ?? 0m;
                
                // Lương mỗi ngày dựa trên số ngày công thực tế của tháng đó
                var dailyRate = baseSalary / totalWorkingDaysInMonth;
                var calculatedSalary = Math.Max(0, (workingDaysCount * dailyRate) - totalPenalty);

                var salary = await db.Salaries.FirstOrDefaultAsync(
                    s => s.EmployeeId == employee.Id && s.PayrollPeriodId == period.Id,
                    cancellationToken);

                if (salary == null)
                {
                    db.Salaries.Add(new Salary
                    {
                        EmployeeId = employee.Id,
                        PayrollPeriodId = period.Id,
                        WorkingDays = workingDaysCount,
                        SalaryAmount = Math.Round(calculatedSalary, 0, MidpointRounding.AwayFromZero)
                    });
                }
                else
                {
                    salary.WorkingDays = workingDaysCount;
                    salary.SalaryAmount = Math.Round(calculatedSalary, 0, MidpointRounding.AwayFromZero);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully updated payroll for {Month}/{Year}", month, year);
        }

        private static int CountWorkingDays(int month, int year)
        {
            int days = DateTime.DaysInMonth(year, month);
            int count = 0;
            for (int day = 1; day <= days; day++)
            {
                var date = new DateTime(year, month, day);
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    count++;
                }
            }
            return count;
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
