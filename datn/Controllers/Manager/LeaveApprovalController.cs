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
                    DayOfWeek.Saturday => 6,
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
                .Select(e => new { id = e.Id, fullName = e.LastName + " " + e.FirstName })
                .ToListAsync();

            return Json(new { success = true, data = teachers });
        }

        [HttpPost("Api/AssignSubstitute")]
        public async Task<IActionResult> AssignSubstitute([FromBody] SubstituteAssignmentDto model)
        {
            var managerEmployeeId = await GetCurrentEmployeeIdAsync();
            var date = DateOnly.Parse(model.Date);

            var schedule = await _context.ClassSchedules
                .Include(cs => cs.Class)
                .Include(cs => cs.Subject)
                .FirstOrDefaultAsync(cs => cs.Id == model.ClassScheduleId);
            if (schedule == null) return Json(new { success = false, message = "Không tìm thấy tiết học." });

            // ── CONFLICT CHECK: Kiểm tra giáo viên có đang bận không ──
            var isFree = await IsTeacherFreeAsync(model.SubstituteEmployeeId, date, schedule.StartTime, schedule.EndTime, excludeScheduleId: model.ClassScheduleId);
            if (!isFree)
            {
                return Json(new { success = false, message = $"Giáo viên này đã có lịch dạy hoặc đang dạy thay tại lớp khác vào khung giờ {schedule.StartTime:HH:mm} - {schedule.EndTime:HH:mm} ngày {date:dd/MM/yyyy}. Vui lòng chọn giáo viên khác." });
            }

            var existing = await _context.Substitutions
                .FirstOrDefaultAsync(s => s.ClassScheduleId == model.ClassScheduleId && s.Date == date);

            if (existing != null)
            {
                _context.Substitutions.Remove(existing);
            }

            var sub = new Substitution
            {
                ClassScheduleId = model.ClassScheduleId,
                Date = date,
                OriginalEmployeeId = model.OriginalEmployeeId,
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

            try
            {
                await _classCoverageService.ProcessClassDateAsync(schedule.ClassId, date);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Class coverage re-evaluation failed after substitute assignment");
            }

            // ── SIGNALR NOTIFICATION: Gửi thông báo cho GV được phân công ──
            var substituteEmployee = await _context.Employees
                .Include(e => e.Account)
                .FirstOrDefaultAsync(e => e.Id == model.SubstituteEmployeeId);

            if (substituteEmployee?.Account != null)
            {
                await _notificationService.SendToUserAsync(
                    substituteEmployee.Account.Id,
                    "Lịch dạy thay mới",
                    $"Bạn được phân công dạy thay môn {schedule.Subject.Name} tại lớp {schedule.Class.Name} vào lúc {schedule.StartTime:HH:mm} - {schedule.EndTime:HH:mm} ngày {date:dd/MM/yyyy}. Cảm ơn bạn!",
                    "info",
                    "/Employee/WorkSchedule"
                );
            }

            return Json(new { success = true, message = "Đã phân công dạy thay, cộng công và gửi thông báo thành công." });
        }

        /// <summary>
        /// Kiểm tra giáo viên có rảnh trong khung giờ cho trước không.
        /// Trả về true nếu rảnh, false nếu đang bận.
        /// </summary>
        private async Task<bool> IsTeacherFreeAsync(int employeeId, DateOnly date, TimeOnly startTime, TimeOnly endTime, int? excludeScheduleId = null)
        {
            var dayOfWeek = date.DayOfWeek switch
            {
                DayOfWeek.Monday => 1,
                DayOfWeek.Tuesday => 2,
                DayOfWeek.Wednesday => 3,
                DayOfWeek.Thursday => 4,
                DayOfWeek.Friday => 5,
                DayOfWeek.Saturday => 6,
                _ => 0
            };

            if (dayOfWeek == 0) return true; // Chủ nhật luôn rảnh

            // Kiểm tra trong ClassSchedule (lịch dạy cố định)
            var hasRegularConflict = await _context.ClassSchedules
                .AnyAsync(cs =>
                    cs.EmployeeId == employeeId &&
                    cs.DayOfWeek == dayOfWeek &&
                    cs.IsActive &&
                    cs.EffectiveFrom <= date &&
                    (cs.EffectiveTo == null || cs.EffectiveTo >= date) &&
                    (excludeScheduleId == null || cs.Id != excludeScheduleId) &&
                    cs.StartTime < endTime && cs.EndTime > startTime); // Kiểm tra giao nhau về thời gian

            if (hasRegularConflict) return false;

            // Kiểm tra trong Substitution (đã nhận dạy thay ở lớp khác)
            var hasSubConflict = await _context.Substitutions
                .Include(s => s.ClassSchedule)
                .AnyAsync(s =>
                    s.SubstituteEmployeeId == employeeId &&
                    s.Date == date &&
                    s.Status == "Confirmed" &&
                    (excludeScheduleId == null || s.ClassScheduleId != excludeScheduleId) &&
                    s.ClassSchedule.StartTime < endTime && s.ClassSchedule.EndTime > startTime);

            return !hasSubConflict;
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
            public int OriginalEmployeeId { get; set; }
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

