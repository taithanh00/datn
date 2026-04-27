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
    [Authorize(Roles = "Manager")]
    [Route("[controller]")]
    public class LeaveApprovalController : BaseController
    {
        private readonly INotificationService _notificationService;

        public LeaveApprovalController(AppDbContext context, INotificationService notificationService) : base(context)
        {
            _notificationService = notificationService;
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
                    // Chỉ tính công cho các ngày trong tuần (T2-T6)
                    if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
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
                            existing.Note = (existing.Note ?? "") + $" | Nghỉ phép có lương: {record.Reason}";
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

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
        public async Task<IActionResult> GetAffectedSchedules(int requestId)
        {
            var leave = await _context.EmployeeLeaveRequests.FindAsync(requestId);
            if (leave == null) return Json(new { success = false, message = "Không tìm thấy đơn nghỉ." });

            var startDate = leave.StartDate;
            var endDate = leave.EndDate;

            var dates = new List<DateOnly>();
            for (var d = startDate; d <= endDate; d = d.AddDays(1)) dates.Add(d);

            var affected = new List<object>();

            foreach (var date in dates)
            {
                var dayOfWeek = date.DayOfWeek switch
                {
                    DayOfWeek.Monday => 1,
                    DayOfWeek.Tuesday => 2,
                    DayOfWeek.Wednesday => 3,
                    DayOfWeek.Thursday => 4,
                    DayOfWeek.Friday => 5,
                    _ => 0
                };

                if (dayOfWeek == 0) continue;

                var schedules = await _context.ClassSchedules
                    .Where(cs => cs.EmployeeId == leave.EmployeeId 
                                 && cs.DayOfWeek == dayOfWeek 
                                 && cs.IsActive
                                 && cs.EffectiveFrom <= date
                                 && (cs.EffectiveTo == null || cs.EffectiveTo >= date))
                    .Include(cs => cs.Class)
                    .Include(cs => cs.Subject)
                    .ToListAsync();

                foreach (var s in schedules)
                {
                    var substitution = await _context.Substitutions
                        .Include(sub => sub.SubstituteEmployee)
                        .FirstOrDefaultAsync(sub => sub.ClassScheduleId == s.Id && sub.Date == date && sub.Status == "Confirmed");

                    affected.Add(new
                    {
                        date = date.ToString("dd/MM/yyyy"),
                        rawDate = date.ToString("yyyy-MM-dd"),
                        scheduleId = s.Id,
                        className = s.Class.Name,
                        subjectName = s.Subject.Name,
                        time = $"{s.StartTime:HH:mm} - {s.EndTime:HH:mm}",
                        substituteName = substitution?.SubstituteEmployee.FullName,
                        substituteId = substitution?.SubstituteEmployeeId
                    });
                }
            }

            return Json(new { success = true, data = affected });
        }

        [HttpGet("Api/AvailableTeachers")]
        public async Task<IActionResult> GetAvailableTeachers()
        {
            var teachers = await _context.Employees
                .Include(e => e.Account)
                .Where(e => e.Account.IsActive && e.Account.Role.Name == "Employee")
                .Select(e => new { id = e.Id, fullName = e.FullName })
                .ToListAsync();

            return Json(new { success = true, data = teachers });
        }

        [HttpPost("Api/AssignSubstitute")]
        public async Task<IActionResult> AssignSubstitute([FromBody] SubstituteAssignmentDto model)
        {
            var managerEmployeeId = await GetCurrentEmployeeIdAsync();
            var date = DateOnly.Parse(model.Date);
            
            var existing = await _context.Substitutions
                .FirstOrDefaultAsync(s => s.ClassScheduleId == model.ClassScheduleId && s.Date == date);
            
            if (existing != null)
            {
                _context.Substitutions.Remove(existing);
            }

            var schedule = await _context.ClassSchedules.FindAsync(model.ClassScheduleId);
            if (schedule == null) return Json(new { success = false, message = "Không tìm thấy tiết học." });

            var sub = new Substitution
            {
                ClassScheduleId = model.ClassScheduleId,
                Date = date,
                OriginalEmployeeId = schedule.EmployeeId,
                SubstituteEmployeeId = model.SubstituteEmployeeId,
                Note = model.Note,
                Status = "Confirmed"
            };

            _context.Substitutions.Add(sub);

            // CỘNG CÔNG CHO NGƯỜI DẠY THAY
            var attendance = await _context.WorkAttendances
                .FirstOrDefaultAsync(w => w.EmployeeId == model.SubstituteEmployeeId && w.Date == date);

            if (attendance == null)
            {
                _context.WorkAttendances.Add(new WorkAttendance
                {
                    EmployeeId = model.SubstituteEmployeeId,
                    Date = date,
                    Status = "Approved",
                    WorkUnit = 0.2m,
                    Note = "Dạy thay tiết học",
                    ReviewNote = "Hệ thống tự động cộng công dạy thay",
                    ReviewedByEmployeeId = managerEmployeeId,
                    ReviewedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                attendance.WorkUnit = (attendance.WorkUnit ?? 1.0m) + 0.2m;
                attendance.Note = (attendance.Note ?? "") + " | Dạy thay tiết học";
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã phân công dạy thay và cộng công thành công." });
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

        public class SubstituteAssignmentDto
        {
            public int ClassScheduleId { get; set; }
            public string Date { get; set; } = string.Empty;
            public int SubstituteEmployeeId { get; set; }
            public string? Note { get; set; }
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
