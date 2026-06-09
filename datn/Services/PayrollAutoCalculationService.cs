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
            try
            {
                _logger.LogInformation("Payroll Auto Calculation Service is starting.");
                await RunCalculationProcess(stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var nowVnt = GetVntNow();
                        var todayVnt = DateOnly.FromDateTime(nowVnt.DateTime);

                        if (nowVnt.Hour == 1 && _lastRunDateVnt != todayVnt)
                        {
                            await RunCalculationProcess(stoppingToken);
                            _lastRunDateVnt = todayVnt;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Error occurred while executing payroll calculation process.");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown.
            }
        }

        private async Task RunCalculationProcess(CancellationToken stoppingToken)
        {
            var nowVnt = GetVntNow();
            var lastMonth = nowVnt.AddMonths(-1);

            await CalculatePayrollAsync(lastMonth.Month, lastMonth.Year, stoppingToken);
            await CalculatePayrollAsync(nowVnt.Month, nowVnt.Year, stoppingToken);
        }

        private async Task CalculatePayrollAsync(int month, int year, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();

            _logger.LogInformation("Calculating payroll for {Month}/{Year}", month, year);
            await payrollService.CalculatePeriodAsync(month, year, cancellationToken);
            _logger.LogInformation("Successfully updated payroll for {Month}/{Year}", month, year);
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
