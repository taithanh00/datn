using datn.Data;
using datn.Hubs;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace datn.Controllers
{
    [Authorize(Roles = "Manager")]
    [Route("[controller]")]
    public class LeaveApprovalController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public LeaveApprovalController(AppDbContext context, INotificationService notificationService)
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
            ViewData["Title"] = "Duyệt nghỉ phép";
            return View();
        }

        [HttpGet("Api/PendingAttendance")]
        public async Task<IActionResult> PendingAttendance(int? month, int? year)
        {
            var nowVnt = GetVntNow();
            var targetMonth = month ?? nowVnt.Month;
            var targetYear = year ?? nowVnt.Year;

            var data = await _context.WorkAttendances
                .Where(w => w.Status == "Pending"
                            && w.Date.Month == targetMonth
                            && w.Date.Year == targetYear)
                .Include(w => w.Employee)
                .OrderByDescending(w => w.Date)
                .Select(w => new
                {
                    employeeId = w.EmployeeId,
                    employeeName = w.Employee.FullName,
                    date = w.Date.ToString("dd/MM/yyyy"),
                    rawDate = w.Date.ToString("yyyy-MM-dd"),
                    checkInAt = w.CheckInAtUtc,
                    checkOutAt = w.CheckOutAtUtc,
                    isLate = w.IsLate,
                    penaltyAmount = w.PenaltyAmount,
                    workedMinutes = w.WorkedMinutes,
                    workUnit = w.WorkUnit
                })
                .ToListAsync();

            return Json(new { success = true, data });
        }

        [HttpPost("Api/Attendance/Approve")]
        public async Task<IActionResult> ApproveAttendance([FromBody] AttendanceDecisionDto model)
        {
            var managerEmployeeId = await GetCurrentEmployeeIdAsync();
            if (managerEmployeeId == null)
                return Json(new { success = false, message = "Không tìm thấy hồ sơ nhân viên của Quản lý." });
            
            var date = DateOnly.Parse(model.Date);
            var record = await _context.WorkAttendances
                .Include(w => w.Employee)
                .FirstOrDefaultAsync(w => w.EmployeeId == model.EmployeeId && w.Date == date);

            if (record == null)
                return Json(new { success = false, message = "Không tìm thấy bản ghi chấm công." });

            record.Status = "Approved";
            record.ReviewedByEmployeeId = managerEmployeeId;
            record.ReviewedAtUtc = DateTime.UtcNow;
            record.ReviewNote = model.ReviewNote;
            await _context.SaveChangesAsync();

            // Thông báo cho Giáo viên
            await _notificationService.SendToUserAsync(record.Employee.AccountId, 
                "Chấm công đã được duyệt", 
                $"Ngày công {record.Date:dd/MM/yyyy} của bạn đã được quản lý phê duyệt.",
                "success", "/TimeAttendance");

            return Json(new { success = true, message = "Đã duyệt chấm công." });
        }

        [HttpPost("Api/Attendance/Reject")]
        public async Task<IActionResult> RejectAttendance([FromBody] AttendanceDecisionDto model)
        {
            var managerEmployeeId = await GetCurrentEmployeeIdAsync();
            if (managerEmployeeId == null)
                return Json(new { success = false, message = "Không tìm thấy hồ sơ nhân viên của Quản lý." });

            var date = DateOnly.Parse(model.Date);
            var record = await _context.WorkAttendances
                .Include(w => w.Employee)
                .FirstOrDefaultAsync(w => w.EmployeeId == model.EmployeeId && w.Date == date);

            if (record == null)
                return Json(new { success = false, message = "Không tìm thấy bản ghi chấm công." });

            record.Status = "Rejected";
            record.ReviewedByEmployeeId = managerEmployeeId;
            record.ReviewedAtUtc = DateTime.UtcNow;
            record.ReviewNote = model.ReviewNote;
            await _context.SaveChangesAsync();

            // Thông báo cho Giáo viên
            await _notificationService.SendToUserAsync(record.Employee.AccountId, 
                "Chấm công bị từ chối", 
                $"Ngày công {record.Date:dd/MM/yyyy} của bạn đã bị từ chối. Lý do: {model.ReviewNote}",
                "error", "/TimeAttendance");

            return Json(new { success = true, message = "Đã từ chối chấm công." });
        }

        [HttpGet("Api/PendingLeaveRequests")]
        public async Task<IActionResult> PendingLeaveRequests(int? month, int? year)
        {
            var nowVnt = GetVntNow();
            var targetMonth = month ?? nowVnt.Month;
            var targetYear = year ?? nowVnt.Year;

            var data = await _context.EmployeeLeaveRequests
                .Where(r => r.Status == "Pending"
                            && r.StartDate.Month == targetMonth
                            && r.StartDate.Year == targetYear)
                .Include(r => r.Employee)
                .OrderByDescending(r => r.CreatedAtUtc)
                .Select(r => new
                {
                    id = r.Id,
                    employeeId = r.EmployeeId,
                    employeeName = r.Employee.FullName,
                    startDate = r.StartDate.ToString("dd/MM/yyyy"),
                    endDate = r.EndDate.ToString("dd/MM/yyyy"),
                    reason = r.Reason,
                    createdAt = r.CreatedAtUtc
                })
                .ToListAsync();

            return Json(new { success = true, data });
        }

        [HttpPost("Api/Leave/Approve")]
        public async Task<IActionResult> ApproveLeave([FromBody] LeaveDecisionDto model)
        {
            var managerEmployeeId = await GetCurrentEmployeeIdAsync();
            if (managerEmployeeId == null)
                return Json(new { success = false, message = "Không tìm thấy hồ sơ nhân viên của Quản lý." });

            var record = await _context.EmployeeLeaveRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == model.RequestId);
            if (record == null)
                return Json(new { success = false, message = "Không tìm thấy đơn nghỉ phép." });

            record.Status = "Approved";
            record.ReviewedByEmployeeId = managerEmployeeId;
            record.ReviewedAtUtc = DateTime.UtcNow;
            record.ReviewNote = model.ReviewNote;
            await _context.SaveChangesAsync();

            // Thông báo cho Giáo viên
            await _notificationService.SendToUserAsync(record.Employee.AccountId, 
                "Đơn nghỉ phép được duyệt", 
                $"Đơn nghỉ phép từ {record.StartDate:dd/MM} đến {record.EndDate:dd/MM} đã được duyệt.",
                "success", "/LeaveRequest");

            return Json(new { success = true, message = "Đã duyệt đơn nghỉ phép." });
        }

        [HttpPost("Api/Leave/Reject")]
        public async Task<IActionResult> RejectLeave([FromBody] LeaveDecisionDto model)
        {
            var managerEmployeeId = await GetCurrentEmployeeIdAsync();
            if (managerEmployeeId == null)
                return Json(new { success = false, message = "Không tìm thấy hồ sơ nhân viên của Quản lý." });

            var record = await _context.EmployeeLeaveRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == model.RequestId);
            if (record == null)
                return Json(new { success = false, message = "Không tìm thấy đơn nghỉ phép." });

            record.Status = "Rejected";
            record.ReviewedByEmployeeId = managerEmployeeId;
            record.ReviewedAtUtc = DateTime.UtcNow;
            record.ReviewNote = model.ReviewNote;
            await _context.SaveChangesAsync();

            // Thông báo cho Giáo viên
            await _notificationService.SendToUserAsync(record.Employee.AccountId, 
                "Đơn nghỉ phép bị từ chối", 
                $"Đơn nghỉ phép từ {record.StartDate:dd/MM} đến {record.EndDate:dd/MM} đã bị từ chối. Lý do: {model.ReviewNote}",
                "error", "/LeaveRequest");

            return Json(new { success = true, message = "Đã từ chối đơn nghỉ phép." });
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

        public class AttendanceDecisionDto
        {
            public int EmployeeId { get; set; }
            public string Date { get; set; } = string.Empty;
            public string? ReviewNote { get; set; }
        }

        public class LeaveDecisionDto
        {
            public int RequestId { get; set; }
            public string? ReviewNote { get; set; }
        }

        private static DateTimeOffset GetVntNow()
        {
            var utcNow = DateTimeOffset.UtcNow;
            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            return TimeZoneInfo.ConvertTime(utcNow, tz);
        }
    }
}
