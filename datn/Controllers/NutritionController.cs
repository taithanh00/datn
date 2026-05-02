using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace datn.Controllers
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
            return View();
        }

        [HttpGet("GetWeeklyMenu")]
        public async Task<IActionResult> GetWeeklyMenu(DateTime start)
        {
            var date = DateOnly.FromDateTime(start);
            var menu = await _nutritionService.GetWeeklyMenuAsync(date);
            return Json(menu);
        }

        [Authorize(Policy = "ManagerOnly")]
        [HttpPost("SaveMenu")]
        public async Task<IActionResult> SaveMenu([FromBody] Menu menu)
        {
            if (menu == null) return BadRequest(new { success = false });
            var success = await _nutritionService.SaveMenuAsync(menu);
            return Json(new { success });
        }

        [Authorize(Policy = "ManagerOnly")]
        [HttpPost("DeleteMenu")]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            var success = await _nutritionService.DeleteMenuAsync(id);
            return Json(new { success });
        }

        [HttpGet("GetDailyStatus")]
        public async Task<IActionResult> GetDailyStatus(DateTime date, int? classId)
        {
            var d = DateOnly.FromDateTime(date);
            var status = await _nutritionService.GetDailyMenuForClassAsync(classId ?? 0, d);
            return Json(status);
        }

        [HttpPost("SaveOverride")]
        public async Task<IActionResult> SaveOverride([FromBody] MenuOverride mo)
        {
            if (mo == null) return BadRequest();

            // Security: If teacher, check if student is in their class
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "Employee")
            {
                var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var teacher = await _context.Employees.FirstOrDefaultAsync(e => e.AccountId == accountId);
                if (teacher == null) return Forbid();

                var student = await _context.Students.FindAsync(mo.StudentId);
                if (student == null) return BadRequest("Student not found");

                var today = DateOnly.FromDateTime(DateTime.Now);
                var isAssigned = await _context.Assignments.AnyAsync(a => 
                    a.EmployeeId == teacher.Id && 
                    a.ClassId == student.ClassId && 
                    a.IsActive && 
                    (a.EndDate == null || a.EndDate >= today));

                if (!isAssigned) return Forbid("Bạn không quản lý lớp của học sinh này");
            }
            else if (role != "Manager")
            {
                return Forbid();
            }

            var success = await _nutritionService.SaveOverrideAsync(mo);
            return Json(new { success });
        }

        [HttpGet("GetMyClasses")]
        public async Task<IActionResult> GetMyClasses()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "Manager")
            {
                var classes = await _context.Classes.Where(c => c.IsActive).Select(c => new { id = c.Id, name = c.Name }).ToListAsync();
                return Json(classes);
            }

            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.AccountId == accountId);
            if (employee == null) return Json(new List<object>());

            var today = DateOnly.FromDateTime(DateTime.Now);
            var classesAssigned = await _context.Assignments
                .Include(a => a.Class)
                .Where(a => a.EmployeeId == employee.Id && a.IsActive && (a.EndDate == null || a.EndDate >= today))
                .Select(a => new { id = a.ClassId, name = a.Class.Name })
                .Distinct()
                .OrderBy(c => c.name)
                .ToListAsync();

            return Json(classesAssigned);
        }
    }
}
