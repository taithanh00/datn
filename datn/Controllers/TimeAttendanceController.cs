using datn.Data;
using datn.Hubs;
using datn.Models;
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
    public class TimeAttendanceController : BaseController
    {
        private const string PendingStatus = "Pending";
        private const decimal LatePenaltyAmount = 20000m;
        private static readonly TimeSpan WorkStart = new(8, 0, 0);
        private static readonly TimeSpan WorkEnd = new(17, 0, 0);
        private static readonly TimeSpan GraceEnd = new(8, 10, 0);

        private readonly IHubContext<RealtimeHub> _hubContext;

        public TimeAttendanceController(AppDbContext context, IHubContext<RealtimeHub> hubContext) : base(context)
        {
            _hubContext = hubContext;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Chấm công";
            return View();
        }

        [HttpGet("Api/Today")]
        public async Task<IActionResult> GetTodayAttendance()
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            if (employeeId == null)
                return Json(new { success = false, message = "Không tìm thấy thông tin giáo viên" });

            var nowVnt = GetVntNow();
            var today = DateOnly.FromDateTime(nowVnt.DateTime);
            var attendance = await _context.WorkAttendances
                .FirstOrDefaultAsync(w => w.EmployeeId == employeeId.Value && w.Date == today);

            var isAllowedNow = IsWithinWorkingWindow(nowVnt);
            return Json(new
            {
                success = true,
                data = new
                {
                    serverTimeVnt = nowVnt.ToString("dd/MM/yyyy HH:mm:ss"),
                    isAllowedNow,
                    canCheckIn = isAllowedNow && attendance?.CheckInAtUtc == null,
                    canCheckOut = isAllowedNow && attendance?.CheckInAtUtc != null && attendance?.CheckOutAtUtc == null,
                    status = attendance?.Status ?? "Chưa chấm công",
                    checkInAt = attendance?.CheckInAtUtc != null
                        ? ToVnt(attendance.CheckInAtUtc.Value).ToString("HH:mm:ss")
                        : null,
                    checkOutAt = attendance?.CheckOutAtUtc != null
                        ? ToVnt(attendance.CheckOutAtUtc.Value).ToString("HH:mm:ss")
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

            var nowVnt = GetVntNow();
            if (!IsWithinWorkingWindow(nowVnt))
                return Json(new { success = false, message = "Chỉ được chấm công từ Thứ 2 đến Thứ 6, 08:00 - 17:00 (VNT)." });

            var today = DateOnly.FromDateTime(nowVnt.DateTime);
            var existing = await _context.WorkAttendances
                .FirstOrDefaultAsync(w => w.EmployeeId == employeeId.Value && w.Date == today);

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
            record.Status = PendingStatus;

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

            var nowVnt = GetVntNow();
            if (!IsWithinWorkingWindow(nowVnt))
                return Json(new { success = false, message = "Chỉ được chấm công từ Thứ 2 đến Thứ 6, 08:00 - 17:00 (VNT)." });

            var today = DateOnly.FromDateTime(nowVnt.DateTime);
            var record = await _context.WorkAttendances
                .FirstOrDefaultAsync(w => w.EmployeeId == employeeId.Value && w.Date == today);

            if (record == null || record.CheckInAtUtc == null)
                return Json(new { success = false, message = "Bạn chưa check-in hôm nay." });
            if (record.CheckOutAtUtc != null)
                return Json(new { success = false, message = "Bạn đã check-out hôm nay rồi." });

            record.CheckOutAtUtc = nowVnt.UtcDateTime;
            var checkInVnt = ToVnt(record.CheckInAtUtc.Value);
            var workedMinutes = (int)Math.Max(0, (nowVnt - checkInVnt).TotalMinutes);
            record.WorkedMinutes = workedMinutes;
            record.WorkUnit = Math.Round((decimal)workedMinutes / 480m, 2, MidpointRounding.AwayFromZero);
            record.Status = PendingStatus;

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

        private static bool IsWithinWorkingWindow(DateTimeOffset vntNow)
        {
            var day = vntNow.DayOfWeek;
            var isWorkingDay = day is >= DayOfWeek.Monday and <= DayOfWeek.Friday;
            var time = vntNow.TimeOfDay;
            return isWorkingDay && time >= WorkStart && time <= WorkEnd;
        }

        private static DateTimeOffset GetVntNow()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var tz = ResolveVntTimeZone();
            return TimeZoneInfo.ConvertTime(utcNow, tz);
        }

        private static DateTimeOffset ToVnt(DateTime utc)
        {
            var tz = ResolveVntTimeZone();
            var normalizedUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTime(new DateTimeOffset(normalizedUtc), tz);
        }

        private static TimeZoneInfo ResolveVntTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
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
