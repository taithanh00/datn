using datn.Data;
using datn.Hubs;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace datn.Controllers
{
    [Authorize(Roles = "Employee")]
    [Route("[controller]")]
    public class LeaveRequestController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public LeaveRequestController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
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

        [HttpGet("")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Tạo đơn nghỉ phép";
            return View();
        }

        [HttpGet("Api/MyRequests")]
        public async Task<IActionResult> MyRequests()
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            if (employeeId == null)
                return Json(new { success = false, message = "Không tìm thấy thông tin giáo viên." });

            var data = await _context.EmployeeLeaveRequests
                .Where(r => r.EmployeeId == employeeId.Value)
                .OrderByDescending(r => r.CreatedAtUtc)
                .Take(20)
                .Select(r => new
                {
                    id = r.Id,
                    startDate = r.StartDate.ToString("dd/MM/yyyy"),
                    endDate = r.EndDate.ToString("dd/MM/yyyy"),
                    reason = r.Reason,
                    status = r.Status,
                    reviewNote = r.ReviewNote
                })
                .ToListAsync();

            return Json(new { success = true, data });
        }

        [HttpPost("Api/Create")]
        public async Task<IActionResult> Create([FromBody] CreateLeaveRequestDto model)
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            if (employeeId == null)
                return Json(new { success = false, message = "Không tìm thấy thông tin giáo viên." });

            var emp = await _context.Employees.FindAsync(employeeId.Value);

            if (string.IsNullOrWhiteSpace(model.StartDate) || string.IsNullOrWhiteSpace(model.EndDate))
                return Json(new { success = false, message = "Vui lòng chọn ngày bắt đầu và ngày kết thúc." });

            if (!DateOnly.TryParse(model.StartDate, out var startDate) ||
                !DateOnly.TryParse(model.EndDate, out var endDate))
                return Json(new { success = false, message = "Định dạng ngày không hợp lệ." });

            if (endDate < startDate)
                return Json(new { success = false, message = "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu." });

            var request = new EmployeeLeaveRequest
            {
                EmployeeId = employeeId.Value,
                StartDate = startDate,
                EndDate = endDate,
                Reason = model.Reason?.Trim() ?? string.Empty,
                Status = "Pending",
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.EmployeeLeaveRequests.Add(request);
            await _context.SaveChangesAsync();

            // Gửi thông báo tới các Manager
            await _notificationService.SendToRoleAsync("Manager", 
                "Đơn xin nghỉ mới", 
                $"Giáo viên {emp?.FullName} vừa gửi đơn xin nghỉ từ {startDate:dd/MM} đến {endDate:dd/MM}.",
                "warning", "/LeaveApproval");

            return Json(new { success = true, message = "Tạo đơn nghỉ phép thành công. Đơn đang chờ duyệt." });
        }

        private async Task<int?> GetCurrentEmployeeIdAsync()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out var accountId))
                return null;

            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.AccountId == accountId);

            return employee?.Id;
        }

        public class CreateLeaveRequestDto
        {
            public string? StartDate { get; set; }
            public string? EndDate { get; set; }
            public string? Reason { get; set; }
        }
    }
}
