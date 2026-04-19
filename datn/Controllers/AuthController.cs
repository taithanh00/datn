using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
namespace datn.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, JwtService jwtService, IConfiguration config)
        {
            _context = context;
            _jwtService = jwtService;
            _config = config;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe = false)
        {
            var account = await _context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Username == username && a.IsActive);

            // Kiểm tra account có tồn tại và mật khẩu có đúng không
            if (account == null || !BCrypt.Net.BCrypt.Verify(password, account.PasswordHash))
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng";
                return View();
            }

            // ===== BƯỚC 2️⃣: TẠO ACCESS TOKEN (luôn ngắn hạn 15-30 phút) =====
            var accessToken = _jwtService.GenerateAccessToken(account);

            int refreshTokenExpiryDays = rememberMe 
                ? int.Parse(_config["JwtSettings:RememberedRefreshTokenExpiryDays"] ?? "30")  // "Ghi nhớ" → 30 ngày
                : int.Parse(_config["JwtSettings:DefaultRefreshTokenExpiryDays"] ?? "1");     // Không ghi nhớ → 1 ngày

            var refreshToken = new RefreshToken
            {
                AccountId = account.Id,
                Token = _jwtService.GenerateRefreshToken(),  // Sinh chuỗi random 64 bytes
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),  // ⭐ Thời hạn khác nhau
                CreatedAt = DateTime.UtcNow
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,            // ⭐ An toàn: JavaScript không thể truy cập
                Secure = false,             // ⚠️ TODO: Đặt true khi deploy HTTPS (production)
                SameSite = SameSiteMode.Strict,  // ⭐ An toàn: chỉ gửi cho same-site requests
                Expires = DateTime.UtcNow.AddMinutes(
                    int.Parse(_config["JwtSettings:AccessTokenExpiryMinutes"]))  // Access Token thường 15-30 phút
            };

            // Lưu Access Token vào cookie (ngắn hạn - tự động refresh khi hết hạn)
            Response.Cookies.Append("access_token", accessToken, cookieOptions);

            // Cập nhật thời hạn cookie để match với Refresh Token (dài hơn)
            cookieOptions.Expires = refreshToken.ExpiresAt;

            // Lưu Refresh Token vào cookie (thời hạn dài hơn - 1 ngày hoặc 30 ngày tùy rememberMe)
            Response.Cookies.Append("refresh_token", refreshToken.Token, cookieOptions);

            // ===== BƯỚC 5️⃣: CHUYỂN HƯỚNG VỀ TRANG CHỦ =====
            return RedirectToAction("Index", "Home");
        }

        // LOGOUT
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refresh_token"];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                // Tìm Refresh Token trong DB
                var token = await _context.RefreshTokens
                    .FirstOrDefaultAsync(r => r.Token == refreshToken);

                if (token != null)
                {
                    // Đánh dấu token là bị thu hồi (không thể dùng được nữa)
                    token.IsRevoked = true;
                    await _context.SaveChangesAsync();
                }
            }

            // Xóa các cookie chứa token
            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("refresh_token");
            // Chuyển hướng về trang Login sau khi đăng xuất
            return RedirectToAction("Login");
        }
    }
}   
