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

namespace datn.Controllers.Teacher
{
    [Authorize(Roles = "Employee")]
    [Route("[controller]")]
    public class TimeAttendanceController : BaseController
    {
        private const decimal LatePenaltyAmount = 20000m;
        private static readonly TimeSpan GraceEnd = new(6, 40, 0);

        private readonly IHubContext<RealtimeHub> _hubContext;
        private readonly ITimeAttendanceWindowService _attendanceWindowService;

        public TimeAttendanceController(
            AppDbContext context,
            IHubContext<RealtimeHub> hubContext,
            ITimeAttendanceWindowService attendanceWindowService) : base(context)
        {
            _hubContext = hubContext;
            _attendanceWindowService = attendanceWindowService;
        }

        [HttpGet("")]
        [HttpGet("/Employee/TimeAttendance")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Chấm công";
            return View("~/Views/Dashboard/Teacher/TimeAttendance/Index.cshtml");
        }

        [HttpGet("Api/Today")]
        public async Task<IActionResult> GetTodayAttendance()
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            if (employeeId == null)
                return Json(new { success = false, message = "Không tìm thấy thông tin giáo viên" });

            var nowVnt = _attendanceWindowService.GetVntNow();
            var today = DateOnly.FromDateTime(nowVnt.DateTime);
            var attendance = await _context.WorkAttendances
                .FirstOrDefaultAsync(w => w.EmployeeId == employeeId.Value && w.Date == today);

            var attendanceWindow = _attendanceWindowService.GetWindowState(nowVnt);
            return Json(new
            {
                success = true,
                data = new
                {
                    serverTimeVnt = nowVnt.ToString("dd/MM/yyyy HH:mm:ss"),
                    isAllowedNow = attendanceWindow.IsAllowed,
                    attendanceWindowMessage = attendanceWindow.Message,
                    canCheckIn = attendanceWindow.IsAllowed
                        && attendance?.CheckInAtUtc == null
                        && attendance?.Status != WorkAttendanceStatuses.UnauthorizedAbsent,
                    canCheckOut = attendanceWindow.IsAllowed && attendance?.CheckInAtUtc != null && attendance?.CheckOutAtUtc == null,
                    status = attendance?.Status ?? "Chưa chấm công",
                    checkInAt = attendance?.CheckInAtUtc != null
                        ? _attendanceWindowService.ToVnt(attendance.CheckInAtUtc.Value).ToString("HH:mm:ss")
                        : null,
                    checkOutAt = attendance?.CheckOutAtUtc != null
                        ? _attendanceWindowService.ToVnt(attendance.CheckOutAtUtc.Value).ToString("HH:mm:ss")
                        : null,
                    isLate = attendance?.IsLate ?? false,
                    penaltyAmount = attendance?.PenaltyAmount ?? 0
                }
            });
        }

        [HttpPost("Api/CheckIn")]
        public async Task<IActionResult> CheckIn()
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            if (employeeId == null)
                return Json(new { success = false, message = "Không tìm thấy thông tin giáo viên" });

            var nowVnt = _attendanceWindowService.GetVntNow();
            var attendanceWindow = _attendanceWindowService.GetWindowState(nowVnt);
            if (!attendanceWindow.IsAllowed)
                return Json(new { success = false, message = attendanceWindow.Message });

            var today = DateOnly.FromDateTime(nowVnt.DateTime);
            var existing = await _context.WorkAttendances
                .FirstOrDefaultAsync(w => w.EmployeeId == employeeId.Value && w.Date == today);

            if (existing?.Status == WorkAttendanceStatuses.UnauthorizedAbsent)
                return Json(new { success = false, message = "Bạn đã được ghi nhận nghỉ không phép hôm nay. Vui lòng liên hệ quản lý." });

            if (existing?.CheckInAtUtc != null)
                return Json(new { success = false, message = "Bạn đã check-in hôm nay rồi." });

            var isLate = nowVnt.TimeOfDay > GraceEnd;
            var record = existing ?? new WorkAttendance
            {
                EmployeeId = employeeId.Value,
                Date = today
            };

            record.CheckInAtUtc = nowVnt.UtcDateTime;
            record.IsLate = isLate;
            record.PenaltyAmount = isLate ? LatePenaltyAmount : 0m;
            record.Status = WorkAttendanceStatuses.Pending;

            if (existing == null)
                _context.WorkAttendances.Add(record);
            else
                _context.WorkAttendances.Update(record);

            await _context.SaveChangesAsync();
            await NotifyManagersAsync("attendance.created", employeeId.Value, today.ToString("yyyy-MM-dd"));
            return Json(new
            {
                success = true,
                message = isLate
                    ? "Check-in thành công. Bạn đi trễ, tạm tính phạt 20.000đ (chờ Manager duyệt)."
                    : "Check-in thành công. Bạn đi đúng giờ.",
                data = new
                {
                    checkInAt = nowVnt.ToString("HH:mm:ss"),
                    isLate,
                    penaltyAmount = record.PenaltyAmount,
                    status = record.Status
                }
            });
        }

        [HttpPost("Api/CheckOut")]
        public async Task<IActionResult> CheckOut()
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            if (employeeId == null)
                return Json(new { success = false, message = "Không tìm thấy thông tin giáo viên" });

            var nowVnt = _attendanceWindowService.GetVntNow();
            var attendanceWindow = _attendanceWindowService.GetWindowState(nowVnt);
            if (!attendanceWindow.IsAllowed)
                return Json(new { success = false, message = attendanceWindow.Message });

            var today = DateOnly.FromDateTime(nowVnt.DateTime);
            var record = await _context.WorkAttendances
                .FirstOrDefaultAsync(w => w.EmployeeId == employeeId.Value && w.Date == today);

            if (record == null || record.CheckInAtUtc == null)
                return Json(new { success = false, message = "Bạn chưa check-in hôm nay." });
            if (record.CheckOutAtUtc != null)
                return Json(new { success = false, message = "Bạn đã check-out hôm nay rồi." });

            record.CheckOutAtUtc = nowVnt.UtcDateTime;
            var checkInVnt = _attendanceWindowService.ToVnt(record.CheckInAtUtc.Value);
            var workedMinutes = (int)Math.Max(0, (nowVnt - checkInVnt).TotalMinutes);
            record.WorkedMinutes = workedMinutes;
            var calculatedWorkUnit = Math.Round((decimal)workedMinutes / 480m, 2, MidpointRounding.AwayFromZero);
            record.WorkUnit = Math.Min(1.0m, calculatedWorkUnit);
            record.Status = WorkAttendanceStatuses.Pending;

            _context.WorkAttendances.Update(record);
            await _context.SaveChangesAsync();
            await NotifyManagersAsync("attendance.updated", employeeId.Value, today.ToString("yyyy-MM-dd"));

            return Json(new
            {
                success = true,
                message = "Check-out thành công. Bản ghi đang chờ Manager duyệt.",
                data = new
                {
                    checkOutAt = nowVnt.ToString("HH:mm:ss"),
                    workedMinutes,
                    workUnit = record.WorkUnit,
                    status = record.Status
                }
            });
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

        private Task NotifyManagersAsync(string eventType, int employeeId, string workDate)
        {
            return _hubContext.Clients.Group("Managers").SendAsync("attendanceChanged", new
            {
                eventType,
                employeeId,
                workDate,
                at = DateTimeOffset.UtcNow
            });
        }
    }
}
