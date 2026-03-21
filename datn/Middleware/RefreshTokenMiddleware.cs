using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.EntityFrameworkCore;
namespace datn.Middleware
{
    public class RefreshTokenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RefreshTokenMiddleware> _logger;

        public RefreshTokenMiddleware(
            RequestDelegate next,
            ILogger<RefreshTokenMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Chỉ xử lý khi có refresh_token cookie
            // và không phải đang ở trang Auth
            var path = context.Request.Path.Value ?? "";
            if (!path.StartsWith("/Auth"))
            {
                var accessToken = context.Request.Cookies["access_token"];
                var refreshToken = context.Request.Cookies["refresh_token"];

                // Nếu không có access token nhưng có refresh token → thử refresh
                if (string.IsNullOrEmpty(accessToken) &&
                    !string.IsNullOrEmpty(refreshToken))
                {
                    await TryRefreshToken(context, refreshToken);
                }
            }

            await _next(context);
        }

        private async Task TryRefreshToken(HttpContext context, string refreshToken)
        {
            var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
            var jwtService = context.RequestServices.GetRequiredService<JwtService>();
            var config = context.RequestServices.GetRequiredService<IConfiguration>();

            var storedToken = await dbContext.RefreshTokens
                .Include(r => r.Account).ThenInclude(a => a.Role)
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            // Token không tồn tại
            if (storedToken == null)
            {
                _logger.LogWarning("Refresh token không tồn tại trong DB");
                ClearCookies(context);
                return;
            }

            // Token đã bị thu hồi — nghi bị đánh cắp
            if (storedToken.IsRevoked)
            {
                _logger.LogWarning(
                    "CẢNH BÁO: Refresh token bị thu hồi được dùng lại. AccountId: {Id}",
                    storedToken.AccountId);

                // Thu hồi toàn bộ token của account
                var allTokens = await dbContext.RefreshTokens
                    .Where(r => r.AccountId == storedToken.AccountId && !r.IsRevoked)
                    .ToListAsync();
                allTokens.ForEach(t => t.IsRevoked = true);
                await dbContext.SaveChangesAsync();

                ClearCookies(context);
                context.Response.Redirect("/Auth/Login?reason=security");
                return;
            }

            // Token hết hạn
            if (storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogInformation(
                    "Refresh token hết hạn. AccountId: {Id}", storedToken.AccountId);
                ClearCookies(context);
                context.Response.Redirect("/Auth/Login?reason=expired");
                return;
            }

            // Token hợp lệ — cấp mới
            _logger.LogInformation(
                "Cấp lại token cho AccountId: {Id}", storedToken.AccountId);

            var newAccessToken = jwtService.GenerateAccessToken(storedToken.Account);

            storedToken.IsRevoked = true;
            var newRefreshToken = new RefreshToken
            {
                AccountId = storedToken.AccountId,
                Token = jwtService.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(
                    int.Parse(config["JwtSettings:RefreshTokenExpiryDays"]))
            };
            dbContext.RefreshTokens.Add(newRefreshToken);
            await dbContext.SaveChangesAsync();

            var accessOpts = new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(
                    int.Parse(config["JwtSettings:AccessTokenExpiryMinutes"]))
            };
            var refreshOpts = new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = newRefreshToken.ExpiresAt
            };

            context.Request.Headers["Authorization"] = "Bearer " + newAccessToken;
            context.Response.Cookies.Append("access_token", newAccessToken, accessOpts);
            context.Response.Cookies.Append("refresh_token", newRefreshToken.Token, refreshOpts);
        }

        private void ClearCookies(HttpContext context)
        {
            context.Response.Cookies.Delete("access_token");
            context.Response.Cookies.Delete("refresh_token");
        }
    }
}
