using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.EntityFrameworkCore;
namespace datn.Middleware
{
    /// <summary>
    /// RefreshTokenMiddleware: Middleware xử lý làm mới JWT Access Token tự động
    /// 
    /// Mục đích:
    /// - Khi user gửi request mà Access Token đã hết hạn hoặc không tồn tại
    /// - Middleware tự động kiểm tra Refresh Token
    /// - Nếu Refresh Token còn hợp lệ → cấp Access Token mới
    /// - Nếu Refresh Token không hợp lệ → chuyển hướng đến trang Login
    /// 
    /// Lợi ích:
    /// - Cải thiện UX: User không bị ngắt kết nối bất ngờ
    /// - Bảo mật: Access Token ngắn hạn, thường xuyên được refresh
    /// - Phát hiện bất thường: Nếu token bị revoke được dùng lại → cảnh báo/chặn tất cả token của account
    /// </summary>
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

        /// <summary>
        /// InvokeAsync: Xử lý request trước khi đến controller
        /// Tự động refresh token nếu cần thiết
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Bỏ qua xử lý refresh token cho các request tới /Auth (để tránh vòng lặp)
            // Ví dụ: Login, Logout không cần kiểm tra/refresh token
            var path = context.Request.Path.Value ?? "";
            if (!path.StartsWith("/Auth"))
            {
                var accessToken = context.Request.Cookies["access_token"];
                var refreshToken = context.Request.Cookies["refresh_token"];

                // Nếu không có access token nhưng có refresh token → thử refresh lấy token mới
                // Điều này xảy ra khi: Access token hết hạn, user tiếp tục dùng ứng dụng
                if (string.IsNullOrEmpty(accessToken) &&
                    !string.IsNullOrEmpty(refreshToken))
                {
                    await TryRefreshToken(context, refreshToken);
                }
            }

            await _next(context);
        }

        /// <summary>
        /// TryRefreshToken: Thử cấp Access Token mới dùng Refresh Token
        /// 
        /// Các bước:
        /// 1. Tìm Refresh Token trong DB
        /// 2. Kiểm tra token có bị revoke không
        /// 3. Kiểm tra token có hết hạn không
        /// 4. Nếu hợp lệ → cấp Access Token mới + tạo Refresh Token mới + cập nhật cookie
        /// 5. Nếu không hợp lệ → xóa cookie + chuyển hướng đến Login
        /// </summary>
        private async Task TryRefreshToken(HttpContext context, string refreshToken)
        {
            var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
            var jwtService = context.RequestServices.GetRequiredService<JwtService>();
            var config = context.RequestServices.GetRequiredService<IConfiguration>();

            // Tìm Refresh Token trong DB kèm theo Account và Role
            var storedToken = await dbContext.RefreshTokens
                .Include(r => r.Account).ThenInclude(a => a.Role)
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            // ===== KIỂM TRA 1: Token có tồn tại trong DB không =====
            if (storedToken == null)
            {
                _logger.LogWarning("Refresh token không tồn tại trong DB");
                ClearCookies(context);
                return;
            }

            // ===== KIỂM TRA 2: Token đã bị thu hồi (revoked) chưa =====
            // Nếu token đã bị revoke mà vẫn được dùng → nghi ngờ bị đánh cắp (tấn công)
            // Hành động: Thu hồi TẤT CẢ token của account này để bảo vệ
            if (storedToken.IsRevoked)
            {
                _logger.LogWarning(
                    "CẢNH BÁO: Refresh token bị thu hồi được dùng lại. AccountId: {Id}",
                    storedToken.AccountId);

                // Thu hồi toàn bộ refresh token của account này (chặn tất cả phiên làm việc)
                var allTokens = await dbContext.RefreshTokens
                    .Where(r => r.AccountId == storedToken.AccountId && !r.IsRevoked)
                    .ToListAsync();
                allTokens.ForEach(t => t.IsRevoked = true);
                await dbContext.SaveChangesAsync();

                ClearCookies(context);
                context.Response.Redirect("/Auth/Login?reason=security");
                return;
            }

            // ===== KIỂM TRA 3: Token đã hết hạn chưa =====
            if (storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogInformation(
                    "Refresh token hết hạn. AccountId: {Id}", storedToken.AccountId);
                ClearCookies(context);
                context.Response.Redirect("/Auth/Login?reason=expired");
                return;
            }

            // ===== TẤT CẢ KIỂM TRA ĐỀU PASSED → Cấp token mới =====
            _logger.LogInformation(
                "Cấp lại token cho AccountId: {Id}", storedToken.AccountId);

            // Tạo Access Token mới
            var newAccessToken = jwtService.GenerateAccessToken(storedToken.Account);

            // Thu hồi Refresh Token cũ (không thể dùng nữa)
            storedToken.IsRevoked = true;

            // Tạo Refresh Token mới
            var newRefreshToken = new RefreshToken
            {
                AccountId = storedToken.AccountId,
                Token = jwtService.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(
                    int.Parse(config["JwtSettings:RefreshTokenExpiryDays"]))
            };
            dbContext.RefreshTokens.Add(newRefreshToken);
            await dbContext.SaveChangesAsync();

            // Cấu hình cookie cho Access Token mới
            var accessOpts = new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(
                    int.Parse(config["JwtSettings:AccessTokenExpiryMinutes"]))
            };

            // Cấu hình cookie cho Refresh Token mới
            var refreshOpts = new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = newRefreshToken.ExpiresAt
            };

            // Cập nhật Authorization header để request tiếp tục được xử lý với token mới
            context.Request.Headers["Authorization"] = "Bearer " + newAccessToken;

            // Cập nhật cookie trên response
            context.Response.Cookies.Append("access_token", newAccessToken, accessOpts);
            context.Response.Cookies.Append("refresh_token", newRefreshToken.Token, refreshOpts);
        }

        /// <summary>
        /// ClearCookies: Xóa cookie chứa token
        /// Gọi khi token không hợp lệ để đảm bảo người dùng phải đăng nhập lại
        /// </summary>
        private void ClearCookies(HttpContext context)
        {
            context.Response.Cookies.Delete("access_token");
            context.Response.Cookies.Delete("refresh_token");
        }
    }
}
