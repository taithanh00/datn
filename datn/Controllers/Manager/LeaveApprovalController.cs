using datn.Data;
using datn.Hubs;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace datn.Controllers.Manager
{
    [Authorize(Roles = "Manager")]
    [Route("[controller]")]
    public class LeaveApprovalController : BaseController
    {
        private readonly INotificationService _notificationService;
        private readonly IClassCoverageService _classCoverageService;
        private readonly ILogger<LeaveApprovalController> _logger;

        public LeaveApprovalController(
            AppDbContext context,
            INotificationService notificationService,
            IClassCoverageService classCoverageService,
            ILogger<LeaveApprovalController> logger) : base(context)
        {
            _notificationService = notificationService;
            _classCoverageService = classCoverageService;
            _logger = logger;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Duyệt nghỉ phép";
            return View("~/Views/Dashboard/Admin/LeaveApproval/Index.cshtml");
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
                    employeeName = w.Employee.LastName + " " + w.Employee.FirstName,
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

            if (record.CheckInAtUtc != null)
            {
                try
                {
                    await _classCoverageService.ProcessEmployeeAttendanceApprovedAsync(record.EmployeeId, record.Date);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Class coverage bonus check failed after attendance approval");
                }
            }

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
                    employeeName = r.Employee.LastName + " " + r.Employee.FirstName,
                    startDate = r.StartDate.ToString("dd/MM/yyyy"),
                    endDate = r.EndDate.ToString("dd/MM/yyyy"),
                    isPaid = r.IsPaid,
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

            // Nếu là nghỉ có lương, tự động tạo ngày công
            if (record.IsPaid)
            {
                var startDate = record.StartDate;
                var endDate = record.EndDate;

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    // Chỉ tính công cho các ngày làm việc (T2-T7).
                    if (date.DayOfWeek != DayOfWeek.Sunday)
                    {
                        var existing = await _context.WorkAttendances
                            .FirstOrDefaultAsync(w => w.EmployeeId == record.EmployeeId && w.Date == date);

                        if (existing == null)
                        {
                            _context.WorkAttendances.Add(new WorkAttendance
                            {
                                EmployeeId = record.EmployeeId,
                                Date = date,
                                Status = "Approved", // Tự động duyệt vì là nghỉ phép đã được Manager đồng ý
                                WorkUnit = 1.0m,
                                PenaltyAmount = 0m,
                                ReviewedByEmployeeId = managerEmployeeId,
                                ReviewedAtUtc = DateTime.UtcNow,
                                Note = $"Nghỉ phép có lương: {record.Reason}",
                                ReviewNote = "Hệ thống tự động tạo từ đơn nghỉ phép"
                            });
                        }
                        else if (existing.Status != "Approved")
                        {
                            // Nếu đã có bản ghi chấm công (ví dụ: quên CheckOut hoặc Pending), ghi đè bằng Approved nghỉ phép
                            existing.Status = "Approved";
                            existing.WorkUnit = 1.0m;
                            existing.PenaltyAmount = 0m;
                            existing.ReviewedByEmployeeId = managerEmployeeId;
                            existing.ReviewedAtUtc = DateTime.UtcNow;
                            existing.ReviewNote = "Hệ thống tự động duyệt công từ đơn nghỉ phép có lương";
                            existing.Note = (existing.Note ?? "") + $" | Nghỉ phép có lương: {record.Reason}";
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            try
            {
                var coverage = await _classCoverageService.ProcessLeaveApprovalAsync(record);
                _logger.LogInformation(
                    "Leave approval coverage: request {RequestId}, parentsNotified={Parents}, bonusesGranted={Bonuses}",
                    record.Id, coverage.ParentsNotified, coverage.BonusesGranted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Class coverage processing failed for leave request {RequestId}", record.Id);
            }

            // Thông báo cho Giáo viên
            await _notificationService.SendToUserAsync(record.Employee.AccountId, 
                "Đơn nghỉ phép được duyệt", 
                $"Đơn nghỉ phép {(record.IsPaid ? "CÓ LƯƠNG" : "KHÔNG LƯƠNG")} từ {record.StartDate:dd/MM} đến {record.EndDate:dd/MM} đã được duyệt.",
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

        [HttpGet("Api/Leave/{requestId:int}/AffectedSchedules")]
        public IActionResult GetAffectedSchedules(int requestId)
        {
            return StatusCode(StatusCodes.Status410Gone, new { success = false, message = "Tính năng dạy thay đã được loại bỏ." });
        }

        [HttpGet("Api/AvailableTeachers")]
        public IActionResult GetAvailableTeachers()
        {
            return StatusCode(StatusCodes.Status410Gone, new { success = false, message = "Tính năng dạy thay đã được loại bỏ." });
        }

        [HttpPost("Api/AssignSubstitute")]
        public IActionResult AssignSubstitute()
        {
            return StatusCode(StatusCodes.Status410Gone, new { success = false, message = "Tính năng dạy thay đã được loại bỏ." });
        }


        private async Task<int?> GetCurrentEmployeeIdAsync()
        {
            var claim = User.FindFirst("EmployeeId");
            if (claim != null && int.TryParse(claim.Value, out int employeeId))
            {
                return employeeId;
            }

            // Fallback
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

