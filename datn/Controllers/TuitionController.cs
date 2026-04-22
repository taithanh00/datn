using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace datn.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class TuitionController : BaseController
    {
        private readonly INotificationService _notificationService;

        public TuitionController(AppDbContext context, INotificationService notificationService) : base(context)
        {
            _notificationService = notificationService;
        }

        // ============ MANAGER VIEWS ============

        [Authorize(Roles = "Manager")]
        [HttpGet("")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Quản lý Kế hoạch Học phí";
            return View();
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("Monitoring")]
        public IActionResult Monitoring()
        {
            ViewData["Title"] = "Theo dõi nộp học phí";
            return View();
        }

        // ============ PARENT VIEWS ============

        [Authorize(Roles = "Parent")]
        [HttpGet("MyTuition")]
        public async Task<IActionResult> MyTuition()
        {
            ViewData["Title"] = "Học phí của con";
            var username = User.Identity?.Name;
            var parent = await _context.Parents.Include(p => p.Account)
                .Include(p => p.ParentStudents).ThenInclude(ps => ps.Student)
                .FirstOrDefaultAsync(p => p.Account.Username == username);

            if (parent == null) return NotFound();

            var studentIds = parent.ParentStudents.Select(ps => ps.StudentId).ToList();
            var tuitions = await _context.Tuitions
                .Include(t => t.Student)
                .Include(t => t.TuitionPlan)
                .Where(t => studentIds.Contains(t.StudentId ?? 0))
                .OrderByDescending(t => t.Year).ThenByDescending(t => t.Month)
                .ToListAsync();

            return View(tuitions);
        }

        // ============ API ENDPOINTS ============

        [Authorize(Roles = "Manager")]
        [HttpGet("Api/Plans")]
        public async Task<IActionResult> GetPlans()
        {
            var plans = await _context.TuitionPlans.OrderBy(p => p.AgeFrom).ToListAsync();
            return Json(new { success = true, data = plans });
        }

        [Authorize(Roles = "Manager")]
        [HttpPost("Api/Plans/Create")]
        public async Task<IActionResult> CreatePlan([FromBody] TuitionPlan model)
        {
            _context.TuitionPlans.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã tạo kế hoạch học phí mới." });
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("Api/Monitoring")]
        public async Task<IActionResult> GetTuitionMonitoring(int month, int year, bool? isPaid)
        {
            var query = _context.Tuitions
                .Include(t => t.Student).ThenInclude(s => s.Class)
                .Include(t => t.TuitionPlan)
                .Where(t => t.Month == month && t.Year == year);

            if (isPaid.HasValue)
                query = query.Where(t => t.IsPaid == isPaid.Value);

            var data = await query.OrderBy(t => t.Student.LastName).ThenBy(t => t.Student.FirstName).ToListAsync();
            
            var result = data.Select(t => new {
                id = t.Id,
                studentName = (t.Student.FirstName + " " + t.Student.LastName).Trim(),
                className = t.Student.Class?.Name ?? "Chưa phân lớp",
                amount = t.TuitionPlan?.Amount ?? 0,
                extraFee = t.ExtraFee ?? 0,
                total = (t.TuitionPlan?.Amount ?? 0) + (t.ExtraFee ?? 0),
                isPaid = t.IsPaid
            });

            return Json(new { success = true, data = result });
        }

        [Authorize(Roles = "Manager")]
        [HttpPost("Api/GenerateMonthlyTuition")]
        public async Task<IActionResult> GenerateMonthlyTuition(int month, int year)
        {
            var students = await _context.Students.ToListAsync();
            var plans = await _context.TuitionPlans.ToListAsync();
            int count = 0;

            foreach (var student in students)
            {
                // Kiểm tra xem đã có hóa đơn cho tháng này chưa
                var existing = await _context.Tuitions.AnyAsync(t => t.StudentId == student.Id && t.Month == month && t.Year == year);
                if (existing) continue;

                // Tìm kế hoạch phù hợp với độ tuổi (nếu có AgeFrom, AgeTo)
                // Giả định logic đơn giản lấy kế hoạch đầu tiên nếu không khớp tuổi
                var plan = plans.FirstOrDefault(); 

                var tuition = new Tuition
                {
                    StudentId = student.Id,
                    Month = month,
                    Year = year,
                    TuitionPlanId = plan?.Id,
                    IsPaid = false,
                    ExtraFee = 0
                };

                _context.Tuitions.Add(tuition);
                count++;

                // Thông báo cho phụ huynh (nếu có thông tin)
                var parentStudent = await _context.ParentStudents.Include(ps => ps.Parent)
                    .FirstOrDefaultAsync(ps => ps.StudentId == student.Id);
                if (parentStudent != null)
                {
                    await _notificationService.SendToUserAsync(parentStudent.Parent.AccountId, 
                        "Thông báo học phí mới", 
                        $"Học phí tháng {month}/{year} của bé {student.FirstName} {student.LastName} đã có. Vui lòng kiểm tra và hoàn thành nộp phí.",
                        "info", "/Tuition/MyTuition");
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Đã sinh {count} hóa đơn học phí cho tháng {month}/{year}." });
        }

        [Authorize(Roles = "Manager")]
        [HttpPost("Api/ConfirmPaid/{id}")]
        public async Task<IActionResult> ConfirmPaid(int id)
        {
            var tuition = await _context.Tuitions.Include(t => t.Student).FirstOrDefaultAsync(t => t.Id == id);
            if (tuition == null) return NotFound();

            tuition.IsPaid = true;
            await _context.SaveChangesAsync();

            // Thông báo cho phụ huynh
            var parentStudent = await _context.ParentStudents.Include(ps => ps.Parent)
                .FirstOrDefaultAsync(ps => ps.StudentId == tuition.StudentId);
            if (parentStudent != null)
            {
                await _notificationService.SendToUserAsync(parentStudent.Parent.AccountId, 
                    "Xác nhận đã đóng học phí", 
                    $"Hệ thống đã nhận được học phí tháng {tuition.Month}/{tuition.Year} cho bé {tuition.Student.FirstName} {tuition.Student.LastName}. Cảm ơn quý phụ huynh.",
                    "success", "/Tuition/MyTuition");
            }

            return Json(new { success = true, message = "Đã xác nhận thanh toán." });
        }
    }
}
