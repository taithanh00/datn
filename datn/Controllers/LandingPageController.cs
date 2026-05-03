using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using datn.Data;
using datn.Models;

namespace datn.Controllers
{
    [AllowAnonymous]
    public class LandingPageController : Controller
    {
        private readonly AppDbContext _context;

        public LandingPageController(AppDbContext context)
        {
            _context = context;
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
    }
}
