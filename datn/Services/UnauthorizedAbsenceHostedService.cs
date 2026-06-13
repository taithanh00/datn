namespace datn.Services
{
    public class UnauthorizedAbsenceHostedService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UnauthorizedAbsenceHostedService> _logger;

        public UnauthorizedAbsenceHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<UnauthorizedAbsenceHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IUnauthorizedAbsenceService>();
                    await service.ProcessTodayAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unauthorized absence background processing failed");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }
    }
}
