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
        // LOGIN
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Tìm account trong DB kèm theo Role của nó
            var account = await _context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Username == username && a.IsActive);

            // Kiểm tra account có tồn tại và mật khẩu có đúng không -> nếu không, trả về lỗi đăng nhập cho người dùng
            if (account == null || !BCrypt.Net.BCrypt.Verify(password, account.PasswordHash))
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng";
                return View();
            }
            // nếu có, tạo JWT Access Token và Refresh Token, lưu vào cookie, chuyển hướng về trang chủ
            // Tạo access token:
            var accessToken = _jwtService.GenerateAccessToken(account);

            // Tạo refresh token: 
            var refreshToken = new RefreshToken
            {
                AccountId = account.Id,
                Token = _jwtService.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(
                    int.Parse(_config["JwtSettings:RefreshTokenExpiryDays"])),
                CreatedAt = DateTime.UtcNow
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            // Cấu hình cookie cho Access Token:
            // - HttpOnly = true: JavaScript không thể truy cập (chống XSS)
            // - Secure = false: để phát triển, nên đặt true khi deploy HTTPS
            // - SameSite = Strict: chỉ gửi cookie với same-site requests (chống CSRF)
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(
                    int.Parse(_config["JwtSettings:AccessTokenExpiryMinutes"]))
            };

            // Lưu Access Token vào cookie
            Response.Cookies.Append("access_token", accessToken, cookieOptions);

            // Cập nhật thời hạn của cookie để match với Refresh Token (lâu hơn)
            cookieOptions.Expires = refreshToken.ExpiresAt;

            // Lưu Refresh Token vào cookie
            Response.Cookies.Append("refresh_token", refreshToken.Token, cookieOptions);
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