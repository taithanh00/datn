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

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }


        // POST: /Auth/Login
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Tìm account kèm Role
            var account = await _context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Username == username && a.IsActive);

            if (account == null || !BCrypt.Net.BCrypt.Verify(password, account.PasswordHash))
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng";
                return View();
            }

            // Tạo Access Token
            var accessToken = _jwtService.GenerateAccessToken(account);

            // Tạo Refresh Token và lưu vào DB
            var refreshToken = new RefreshToken
            {
                AccountId = account.Id,
                Token = _jwtService.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(
                    int.Parse(_config["JwtSettings:RefreshTokenExpiryDays"]))
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            // Lưu token vào HttpOnly Cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // để true khi deploy HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(
                    int.Parse(_config["JwtSettings:AccessTokenExpiryMinutes"]))
            };

            Response.Cookies.Append("access_token", accessToken, cookieOptions);


            cookieOptions.Expires = refreshToken.ExpiresAt;

            Response.Cookies.Append("refresh_token", refreshToken.Token, cookieOptions);
            return RedirectToAction("Index", "Home");
        }

        // POST: /Auth/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refresh_token"];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                // Thu hồi token trong DB
                var token = await _context.RefreshTokens
                    .FirstOrDefaultAsync(r => r.Token == refreshToken);

                if (token != null)
                {
                    token.IsRevoked = true;
                    await _context.SaveChangesAsync();
                }
            }

            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("refresh_token");
            return RedirectToAction("Login");
        }
    }
}   