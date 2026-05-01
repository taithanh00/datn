using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
            var path = context.Request.Path.Value ?? "";
            
            // Skip for static files and Auth endpoints
            if (path.StartsWith("/Auth", StringComparison.OrdinalIgnoreCase) || 
                path.Contains(".") || 
                path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)) 
            {
                await _next(context);
                return;
            }

            var refreshToken = context.Request.Cookies["refresh_token"];
            var isAuthenticated = context.User.Identity?.IsAuthenticated == true;

            // CASE 1: Not authenticated but has refresh token -> Try to refresh
            if (!isAuthenticated && !string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogInformation("User not authenticated, attempting to refresh token via cookie.");
                var success = await TryRefreshToken(context, refreshToken);
                if (success)
                {
                    // Refresh successful -> Redirect to the same URL to pick up the new access token
                    _logger.LogInformation("Token refresh successful, redirecting to {Path}", path);
                    context.Response.Redirect(context.Request.Path + context.Request.QueryString);
                    return;
                }
            }

            // await _next(context) will be called below

            await _next(context);
        }

        private async Task<bool> TryRefreshToken(HttpContext context, string refreshToken)
        {
            try 
            {
                var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
                var jwtService = context.RequestServices.GetRequiredService<JwtService>();
                var config = context.RequestServices.GetRequiredService<IConfiguration>();

                var storedToken = await dbContext.RefreshTokens
                    .Include(r => r.Account).ThenInclude(a => a.Role)
                    .FirstOrDefaultAsync(r => r.Token == refreshToken);

                if (storedToken == null || !storedToken.Account.IsActive || storedToken.IsRevoked || storedToken.ExpiresAt <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Invalid or expired refresh token.");
                    ClearCookies(context);
                    return false;
                }

                var newAccessToken = jwtService.GenerateAccessToken(storedToken.Account);
                
                storedToken.IsRevoked = true;
                storedToken.RevokedAtUtc = DateTime.UtcNow;

                var newRefreshToken = new RefreshToken
                {
                    AccountId = storedToken.AccountId,
                    Token = jwtService.GenerateRefreshToken(),
                    ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(config["JwtSettings:RefreshTokenExpiryDays"] ?? "1")),
                    CreatedAt = DateTime.UtcNow
                };
                
                dbContext.RefreshTokens.Add(newRefreshToken);
                await dbContext.SaveChangesAsync();

                var accessOpts = new CookieOptions { 
                    HttpOnly = true, 
                    Secure = true, // Force secure for cookies
                    SameSite = SameSiteMode.Strict, 
                    Expires = DateTime.UtcNow.AddMinutes(int.Parse(config["JwtSettings:AccessTokenExpiryMinutes"] ?? "30")) 
                };
                var refreshOpts = new CookieOptions { 
                    HttpOnly = true, 
                    Secure = true, 
                    SameSite = SameSiteMode.Strict, 
                    Expires = newRefreshToken.ExpiresAt 
                };

                context.Response.Cookies.Append("access_token", newAccessToken, accessOpts);
                context.Response.Cookies.Append("refresh_token", newRefreshToken.Token, refreshOpts);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return false;
            }
        }

        private void ClearCookies(HttpContext context)
        {
            context.Response.Cookies.Delete("access_token");
            context.Response.Cookies.Delete("refresh_token");
        }
    }
}
