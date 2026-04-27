using datn.Data;
using datn.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace datn.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly AppDbContext _context;

        protected BaseController(AppDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                ViewBag.Username = User.Identity.Name;
                ViewBag.Role = User.FindFirst(ClaimTypes.Role)?.Value;
                ViewBag.FullName = User.FindFirst(ClaimTypes.Name)?.Value; // Hoặc nếu bạn muốn lấy FullName từ claim khác

                // Lấy Avatar trực tiếp từ Claims (đã được JwtService thêm vào)
                var avatarClaim = User.FindFirst("Avatar")?.Value;
                
                if (!string.IsNullOrEmpty(avatarClaim))
                {
                    ViewBag.UserAvatar = avatarClaim;
                }
                else
                {
                    // Fallback nếu không có claim (do token cũ hoặc lỗi)
                    var role = User.FindFirst(ClaimTypes.Role)?.Value;
                    ViewBag.UserAvatar = (role == "Manager" || role == "Parent")
                        ? "/images/lion_orange.png"
                        : "/images/lion_blue.png";
                }
            }
            else
            {
                ViewBag.UserAvatar = "/images/lion_blue.png";
            }

            base.OnActionExecuting(context);
        }
    }
}
