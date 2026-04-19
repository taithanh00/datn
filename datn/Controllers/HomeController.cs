using datn.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using datn.Data;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace datn.Controllers
{
    /// <summary>
    /// HomeController: Xử lý các request liên quan tới dashboard
    /// </summary>
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var username = User.Identity?.Name ?? "User";
            ViewBag.Username = username;
            ViewBag.Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            var employee = _context.Employees.Include(e => e.Account).FirstOrDefault(e => e.Account.Username == username);
            ViewBag.UserAvatar = employee?.AvatarPath ?? "/images/lion_blue.png";

            base.OnActionExecuting(context);
        }

        public IActionResult Index()
        {
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
