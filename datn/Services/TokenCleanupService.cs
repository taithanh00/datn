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

        /// <summary>
        /// ExecuteAsync: Phương thức chính chạy liên tục trong suốt vòng đời ứng dụng
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Vòng lặp chạy cho đến khi ứng dụng dừng
            while (!stoppingToken.IsCancellationRequested)
            {
                // Tạo một scope mới để lấy instance của DbContext
                // (Scope này sẽ được dispose khi thoát khối using)
                using var scope = _services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Xác định ngày cutoff: 7 ngày trước hiện tại (UTC)
                var cutoff = DateTime.UtcNow.AddDays(-7);

                // Tìm các token cần xóa:
                // 1. Token đã hết hạn (ExpiresAt < now)
                // 2. Token đã bị revoke và tồn tại quá 7 ngày
                // Lý do giữ token revoked 7 ngày: Để phát hiện các hành động nghi vấn
                var expiredTokens = dbContext.RefreshTokens
                    .Where(r => r.ExpiresAt < DateTime.UtcNow ||
                               (r.IsRevoked && r.CreatedAt < cutoff));

                // Xóa toàn bộ các token tìm được
                dbContext.RefreshTokens.RemoveRange(expiredTokens);
                await dbContext.SaveChangesAsync();

                // Chờ 24 giờ trước lần dọn dẹp tiếp theo
                // stoppingToken: Token để dừng dịch vụ khi ứng dụng tắt
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
