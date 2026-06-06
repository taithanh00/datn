using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace datn.Controllers.Manager
{
    [Authorize]
    [Route("Manager/[controller]")]
    public class NutritionController : BaseController
    {
        private readonly INutritionService _nutritionService;

        public NutritionController(AppDbContext context, INutritionService nutritionService) : base(context)
        {
            _nutritionService = nutritionService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Quản lý Dinh dưỡng & Thực đơn";
            return View("~/Views/Dashboard/Admin/Nutrition/Index.cshtml");
        }

        [HttpGet("GetWeeklyMenu")]
        public async Task<IActionResult> GetWeeklyMenu()
        {
            var menu = await _nutritionService.GetWeeklyMenuAsync();
            return Json(menu);
        }

        [Authorize(Policy = "ManagerOnly")]
        [HttpPost("SaveMenu")]
        public async Task<IActionResult> SaveMenu([FromBody] Menu menu)
        {
            if (menu == null || menu.DayOfWeek < 1 || menu.DayOfWeek > 6)
            {
                return BadRequest(new { success = false });
            }

            var success = await _nutritionService.SaveMenuAsync(menu);
            return Json(new { success });
        }

        [Authorize(Policy = "ManagerOnly")]
        [HttpPost("DeleteMenu")]
        public async Task<IActionResult> DeleteMenu([FromQuery] int id)
        {
            var success = await _nutritionService.DeleteMenuAsync(id);
            return Json(new { success });
        }
    }
}
