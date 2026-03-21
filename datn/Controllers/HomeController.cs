using datn.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
namespace datn.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Username = User.Identity?.Name;
            ViewBag.Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Dùng fully qualified name để tránh nhầm với datn.Models.Activity
            return View(new ErrorViewModel
            {
                RequestId = System.Diagnostics.Activity.Current?.Id
                            ?? HttpContext.TraceIdentifier
            });
        }
    }
}
