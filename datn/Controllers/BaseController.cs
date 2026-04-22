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
                var username = User.Identity.Name;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                ViewBag.Username = username;
                ViewBag.Role = role;

                // Lấy thông tin tài khoản kèm Employee/Parent để lấy Avatar
                var account = _context.Accounts
                    .Include(a => a.Role)
                    .Include(a => a.Employee)
                    .Include(a => a.Parent)
                    .FirstOrDefault(a => a.Username == username);

                if (account != null)
                {
                    string defaultAvatar = (role == "Manager" || role == "Parent")
                        ? "/images/lion_orange.png"
                        : "/images/lion_blue.png";

                    if (role == "Employee")
                    {
                        ViewBag.UserAvatar = account.Employee?.AvatarPath ?? defaultAvatar;
                    }
                    else if (role == "Parent")
                    {
                        ViewBag.UserAvatar = account.Parent?.AvatarPath ?? defaultAvatar;
                    }
                    else if (role == "Manager")
                    {
                        ViewBag.UserAvatar = account.Employee?.AvatarPath ?? defaultAvatar;
                    }
                    else
                    {
                        ViewBag.UserAvatar = defaultAvatar;
                    }
                }
                else
                {
                    ViewBag.UserAvatar = "/images/lion_blue.png";
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
