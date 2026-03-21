using datn.Data;

namespace datn.Services
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;

        public TokenCleanupService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Xóa các token đã hết hạn hoặc đã bị thu hồi quá 7 ngày
                var cutoff = DateTime.UtcNow.AddDays(-7);
                var expiredTokens = dbContext.RefreshTokens
                    .Where(r => r.ExpiresAt < DateTime.UtcNow ||
                               (r.IsRevoked && r.CreatedAt < cutoff));

                dbContext.RefreshTokens.RemoveRange(expiredTokens);
                await dbContext.SaveChangesAsync();

                // Chạy mỗi 24 giờ
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
