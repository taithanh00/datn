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
        private readonly IEmailService _emailService;

        public AuthController(AppDbContext context, JwtService jwtService, IConfiguration config, IEmailService emailService)
        {
            _context = context;
            _jwtService = jwtService;
            _config = config;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Vui lòng nhập Email";
                return View();
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email.Trim());
            if (account == null)
            {
                ViewBag.Error = "Email này không tồn tại trong hệ thống. Vui lòng kiểm tra lại.";
                return View();
            }

            // Tạo token reset
            var token = Guid.NewGuid().ToString();
            account.PasswordResetToken = token;
            account.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            // Gửi email (Mock)
            var resetLink = Url.Action("ResetPassword", "Auth", new { token = token }, Request.Scheme);
            var subject = "[SenHồng] Khôi phục mật khẩu của bạn";
            var body = $@"
<div style='background-color: #f4f7f6; padding: 40px 0; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
    <table align='center' border='0' cellpadding='0' cellspacing='0' width='600' style='background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
        <tr>
            <td align='center' style='padding: 30px 0; background: linear-gradient(135deg, #fb923c 0%, #f97316 100%);'>
                <h1 style='color: #ffffff; margin: 0; font-size: 24px; letter-spacing: 1px;'>HỆ THỐNG MẦM NON SENHỒNG</h1>
            </td>
        </tr>
        <tr>
            <td style='padding: 40px 30px;'>
                <h2 style='color: #1e293b; margin-top: 0; font-size: 20px;'>Yêu cầu khôi phục mật khẩu</h2>
                <p style='color: #475569; line-height: 1.6; font-size: 16px;'>
                    Chào <strong>{account.Username}</strong>,
                </p>
                <p style='color: #475569; line-height: 1.6; font-size: 16px;'>
                    Chúng tôi nhận được yêu cầu khôi phục mật khẩu cho tài khoản của bạn tại <strong>Trường Mầm Non SenHồng</strong>. 
                    Nếu đây đúng là yêu cầu của bạn, hãy nhấn vào nút bên dưới để tiến hành đặt lại mật khẩu mới.
                </p>
                <div style='text-align: center; margin: 35px 0;'>
                    <a href='{resetLink}' style='background-color: #fb923c; color: #ffffff; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px; display: inline-block; box-shadow: 0 4px 6px rgba(251, 146, 60, 0.3);'>
                        ĐẶT LẠI MẬT KHẨU NGAY
                    </a>
                </div>
                <p style='color: #ef4444; font-size: 14px; font-style: italic; background-color: #fef2f2; padding: 12px; border-radius: 6px;'>
                    * Lưu ý: Đường dẫn này sẽ hết hạn trong vòng <strong>1 giờ</strong>.
                </p>
                <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 30px 0;'>
                <p style='color: #64748b; line-height: 1.6; font-size: 14px;'>
                    Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này. Tài khoản của bạn vẫn sẽ được giữ an toàn.
                </p>
            </td>
        </tr>
        <tr>
            <td style='padding: 20px 30px; background-color: #f8fafc; text-align: center;'>
                <p style='color: #94a3b8; font-size: 12px; margin: 0;'>
                    © {DateTime.Now.Year} Trường Mầm Non SenHồng. All rights reserved.<br>
                    Địa chỉ: Khu đô thị SenHồng, TP. Hồ Chí Minh
                </p>
            </td>
        </tr>
    </table>
</div>";

            await _emailService.SendEmailAsync(account.Email, subject, body);

            ViewBag.Success = "Hướng dẫn khôi phục mật khẩu đã được gửi vào Email của bạn.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login");

            var account = await _context.Accounts.FirstOrDefaultAsync(a => 
                a.PasswordResetToken == token && a.ResetTokenExpires > DateTime.UtcNow);

            if (account == null)
            {
                ViewBag.Error = "Liên kết khôi phục mật khẩu không hợp lệ hoặc đã hết hạn.";
                return View("ForgotPassword");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                ViewBag.Token = token;
                return View();
            }

            // Kiểm tra độ phức tạp (nên đồng bộ với logic ở AccountController/CreateParent)
            if (password.Length < 9 || !password.Any(char.IsUpper) || !password.Any(ch => "!@#$%^&*()_+=-[]{}|;:'\",.<>?/\\".Contains(ch)))
            {
                ViewBag.Error = "Mật khẩu không đạt yêu cầu bảo mật.";
                ViewBag.Token = token;
                return View();
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => 
                a.PasswordResetToken == token && a.ResetTokenExpires > DateTime.UtcNow);

            if (account == null)
            {
                ViewBag.Error = "Liên kết đã hết hạn hoặc không hợp lệ.";
                return View("ForgotPassword");
            }

            // Cập nhật mật khẩu
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password.Trim());
            account.PasswordResetToken = null; // Xóa token sau khi dùng
            account.ResetTokenExpires = null;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập bằng mật khẩu mới.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            username = username.Trim();
            password = password.Trim();

            var account = await _context.Accounts
                .Include(a => a.Role)
                .Include(a => a.Employee)
                .Include(a => a.Parent)
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
                Secure = Request.IsHttps,             // ⭐ Tự động bật bảo mật nếu dùng HTTPS
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
        [ValidateAntiForgeryToken]
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
            // Chuyển hướng về trang chủ (Landing Page) sau khi đăng xuất
            return Redirect("/");
        }

        // ACCESS DENIED
        [HttpGet]
        public IActionResult AccessDenied()
        {
            var username = User.Identity?.Name ?? "Người dùng";
            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value ?? "Chưa xác định";

            ViewBag.Username = username;
            ViewBag.Role = role;

            return View();
        }
    }
}   
