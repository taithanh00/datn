using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using datn.Data;
using datn.Models;
using datn.Services;

namespace datn.Controllers
{
    [AllowAnonymous]
    public class LandingPageController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public LandingPageController(AppDbContext context, IEmailService emailService, IConfiguration config)
        {
            _context = context;
            _emailService = emailService;
            _config = config;
        }

        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public async Task<IActionResult> Teachers()
        {
            var teachers = await _context.Employees
                .Where(e => e.ShowOnLanding && e.IsActive)
                .ToListAsync();
            return View(teachers);
        }

        public IActionResult Facilities()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitConsultation(string FullName, string PhoneNumber, string Email, string Note)
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(PhoneNumber))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ họ tên và số điện thoại." });
            }

            try
            {
                var senderEmail = _config["EmailSettings:SenderEmail"];
                var subject = "[SenHồng] Yêu cầu tư vấn mới từ Landing Page";
                
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
                <h2 style='color: #1e293b; margin-top: 0; font-size: 20px;'>Yêu cầu tư vấn mới</h2>
                <p style='color: #475569; line-height: 1.6; font-size: 16px;'>
                    Bạn nhận được một yêu cầu tư vấn mới từ trang Landing Page. Dưới đây là thông tin chi tiết:
                </p>
                <div style='background-color: #f8fafc; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                    <p style='margin: 8px 0; color: #475569;'><strong>Họ tên:</strong> {FullName}</p>
                    <p style='margin: 8px 0; color: #475569;'><strong>Số điện thoại:</strong> {PhoneNumber}</p>
                    <p style='margin: 8px 0; color: #475569;'><strong>Email:</strong> {Email ?? "Không cung cấp"}</p>
                    <p style='margin: 8px 0; color: #475569;'><strong>Nội dung:</strong> {Note ?? "Không có nội dung"}</p>
                </div>
                <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 30px 0;'>
                <p style='color: #64748b; line-height: 1.6; font-size: 14px; text-align: center;'>
                    Đây là email tự động từ hệ thống. Vui lòng liên hệ lại khách hàng sớm nhất có thể.
                </p>
            </td>
        </tr>
        <tr>
            <td style='padding: 20px 30px; background-color: #f8fafc; text-align: center;'>
                <p style='color: #94a3b8; font-size: 12px; margin: 0;'>
                    © {DateTime.Now.Year} Trường Mầm Non SenHồng. All rights reserved.<br>
                    Địa chỉ: 12 Lang Liêu, Nha Trang, Khánh Hòa
                </p>
            </td>
        </tr>
    </table>
</div>";

                await _emailService.SendEmailAsync(senderEmail, subject, body);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống khi gửi email: " + ex.Message });
            }
        }
    }
}
