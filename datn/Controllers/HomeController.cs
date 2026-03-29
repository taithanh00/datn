using datn.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace datn.Controllers
{
    /// <summary>
    /// HomeController: Xử lý các request liên quan tới dashboard
    /// 
    /// Đặc điểm:
    /// - Tất cả action đều được bảo vệ bằng [Authorize] - yêu cầu user phải đăng nhập
    /// - Index action sẽ render dashboard cho 3 roles: Manager, Employee, Parent
    /// - Dashboard sẽ hiển thị nội dung khác nhau tùy theo role
    /// </summary>
    [Authorize]  // Bảo vệ cả controller - chỉ user đã xác thực mới vào được
    public class HomeController : Controller
    {
        /// <summary>
        /// GET: /Home/Index
        /// Hiển thị trang Dashboard chính
        /// 
        /// Luồng:
        /// 1. Lấy thông tin user từ JWT claims
        /// 2. Truyền Username và Role vào ViewBag để Razor view sử dụng
        /// 3. Return view Home/Index.cshtml
        /// 
        /// ViewBag truyền:
        /// - Username: Tên đăng nhập của user
        /// - Role: Vai trò của user (Manager, Employee, Parent)
        /// </summary>
        public IActionResult Index()
        {
            // Lấy tên user từ Identity Claims
            ViewBag.Username = User.Identity?.Name;

            // Lấy Role từ JWT Claims
            // ClaimTypes.Role là claim type chuẩn trong .NET
            ViewBag.Role = User.FindFirst(ClaimTypes.Role)?.Value;

            // Render view Home/Index.cshtml
            // View sẽ dùng layout _DashboardLayout.cshtml
            return View();
        }

        /// <summary>
        /// GET: /Home/Privacy
        /// Hiển thị trang Privacy
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// GET: /Home/Error
        /// Hiển thị trang lỗi
        /// 
        /// Ghi chú:
        /// - ResponseCache: Không cache error page
        /// - Dùng fully qualified name Activity để tránh nhầm với datn.Models.Activity
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = System.Diagnostics.Activity.Current?.Id
                            ?? HttpContext.TraceIdentifier
            });
        }
    }
}
