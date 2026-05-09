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
                ViewBag.FullName = User.FindFirst("FullName")?.Value ?? User.Identity.Name;

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

                // Nếu là Employee, tải TeacherType để phân biệt GVCN / GVBM trong View
                if (User.FindFirst(ClaimTypes.Role)?.Value == "Employee")
                {
                    var accountIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(accountIdStr, out var accountId))
                    {
                        var employee = _context.Employees
                            .FirstOrDefault(e => e.AccountId == accountId);

                        if (employee != null)
                        {
                            ViewBag.TeacherType = employee.TeacherType.ToString(); // "Lead" hoặc "Subject"
                            ViewBag.IsLead = employee.TeacherType == TeacherType.Lead;
                        }
                        else
                        {
                            ViewBag.TeacherType = "Subject";
                            ViewBag.IsLead = false;
                        }
                    }
                }
            }
            else
            {
                ViewBag.UserAvatar = "/images/lion_blue.png";
                ViewBag.IsLead = false;
                ViewBag.TeacherType = "Subject";
            }

            base.OnActionExecuting(context);
        }
    }
}
