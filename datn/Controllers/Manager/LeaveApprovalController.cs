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
using System.Globalization;
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

            var records = await _context.WorkAttendances
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
                    w.CheckInAtUtc,
                    w.CheckOutAtUtc,
                    isLate = w.IsLate,
                    penaltyAmount = w.PenaltyAmount,
                    workedMinutes = w.WorkedMinutes,
                    workUnit = w.WorkUnit
                })
                .ToListAsync();

            var data = records.Select(w => new
            {
                w.employeeId,
                w.employeeName,
                w.date,
                w.rawDate,
                checkInAt = FormatVntTime(w.CheckInAtUtc),
                checkOutAt = FormatVntTime(w.CheckOutAtUtc),
                w.isLate,
                w.penaltyAmount,
                w.workedMinutes,
                w.workUnit
            });

            return Json(new { success = true, data });
        }

        [HttpGet("Api/Attendance/History")]
        public async Task<IActionResult> AttendanceHistory(int? month, int? year, string? status, string? keyword)
        {
            var nowVnt = GetVntNow();
            var targetMonth = month ?? nowVnt.Month;
            var targetYear = year ?? nowVnt.Year;
            var normalizedStatus = NormalizeHistoryStatus(status);
            var searchTerm = keyword?.Trim();

            var query = _context.WorkAttendances
                .AsNoTracking()
                .Include(w => w.Employee)
                .Where(w => (w.Status == "Approved" || w.Status == "Rejected")
                            && w.Date.Month == targetMonth
                            && w.Date.Year == targetYear);

            if (normalizedStatus != null)
                query = query.Where(w => w.Status == normalizedStatus);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(w => (w.Employee.LastName + " " + w.Employee.FirstName).Contains(searchTerm));

            var records = await query
                .OrderByDescending(w => w.ReviewedAtUtc ?? DateTime.MinValue)
                .ThenByDescending(w => w.Date)
                .Select(w => new
                {
                    w.EmployeeId,
                    employeeName = w.Employee.LastName + " " + w.Employee.FirstName,
                    w.Date,
                    w.CheckInAtUtc,
                    w.CheckOutAtUtc,
                    w.Status,
                    w.IsLate,
                    w.PenaltyAmount,
                    w.WorkedMinutes,
                    w.WorkUnit,
                    w.Note,
                    w.ReviewNote,
                    w.ReviewedByEmployeeId,
                    w.ReviewedAtUtc
                })
                .ToListAsync();

            var reviewerIds = records
                .Where(r => r.ReviewedByEmployeeId.HasValue)
                .Select(r => r.ReviewedByEmployeeId!.Value)
                .Distinct()
                .ToList();

            var reviewers = await _context.Employees
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(e => reviewerIds.Contains(e.Id))
                .Select(e => new
                {
                    e.Id,
                    name = e.Account.Role.Name == "Manager" ? "Quản lý" : e.LastName + " " + e.FirstName
                })
                .ToDictionaryAsync(e => e.Id, e => e.name);

            var data = records.Select(r => new
            {
                employeeId = r.EmployeeId,
                employeeName = r.employeeName,
                date = r.Date.ToString("dd/MM/yyyy"),
                rawDate = r.Date.ToString("yyyy-MM-dd"),
                checkInAt = FormatVntTime(r.CheckInAtUtc),
                checkOutAt = FormatVntTime(r.CheckOutAtUtc),
                status = r.Status,
                isLate = r.IsLate,
                penaltyAmount = r.PenaltyAmount,
                workedMinutes = r.WorkedMinutes,
                workUnit = r.WorkUnit,
                note = r.Note,
                reviewNote = r.ReviewNote,
                reviewerName = r.ReviewedByEmployeeId.HasValue && reviewers.TryGetValue(r.ReviewedByEmployeeId.Value, out var reviewerName)
                    ? reviewerName
                    : "Hệ thống",
                reviewedAt = FormatVntDateTime(r.ReviewedAtUtc)
            });

            return Json(new { success = true, data });
        }

        [HttpPost("Api/Attendance/Approve")]
        public async Task<IActionResult> ApproveAttendance([FromBody] AttendanceDecisionDto? model)
        {
            if (!TryGetAttendanceDecision(model, out var date, out var validationMessage))
                return Json(new { success = false, message = validationMessage });
            var decision = model!;

            var managerEmployeeId = await GetCurrentEmployeeIdAsync();
            if (managerEmployeeId == null)
                return Json(new { success = false, message = "Không tìm thấy hồ sơ nhân viên của Quản lý." });
            
            var record = await _context.WorkAttendances
                .Include(w => w.Employee)
                .FirstOrDefaultAsync(w => w.EmployeeId == decision.EmployeeId && w.Date == date);

            if (record == null)
                return Json(new { success = false, message = "Không tìm thấy bản ghi chấm công." });

            WorkAttendanceCalculator.EnsurePayrollValues(record);
            record.Status = "Approved";
            record.ReviewedByEmployeeId = managerEmployeeId;
            record.ReviewedAtUtc = DateTime.UtcNow;
            record.ReviewNote = decision.ReviewNote;
            await _context.SaveChangesAsync();

            // Thông báo cho Giáo viên
            await _notificationService.SendToUserAsync(record.Employee!.AccountId,
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
        public async Task<IActionResult> RejectAttendance([FromBody] AttendanceDecisionDto? model)
        {
            if (!TryGetAttendanceDecision(model, out var date, out var validationMessage))
                return Json(new { success = false, message = validationMessage });
            var decision = model!;

            var managerEmployeeId = await GetCurrentEmployeeIdAsync();
            if (managerEmployeeId == null)
                return Json(new { success = false, message = "Không tìm thấy hồ sơ nhân viên của Quản lý." });

            var record = await _context.WorkAttendances
                .Include(w => w.Employee)
                .FirstOrDefaultAsync(w => w.EmployeeId == decision.EmployeeId && w.Date == date);

            if (record == null)
                return Json(new { success = false, message = "Không tìm thấy bản ghi chấm công." });

            record.Status = "Rejected";
            record.ReviewedByEmployeeId = managerEmployeeId;
            record.ReviewedAtUtc = DateTime.UtcNow;
            record.ReviewNote = decision.ReviewNote;
            await _context.SaveChangesAsync();

            // Thông báo cho Giáo viên
            await _notificationService.SendToUserAsync(record.Employee!.AccountId,
                "Chấm công bị từ chối", 
                $"Ngày công {record.Date:dd/MM/yyyy} của bạn đã bị từ chối. Lý do: {decision.ReviewNote}",
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

        [HttpGet("Api/Leave/History")]
        public async Task<IActionResult> LeaveHistory(int? month, int? year, string? status, string? keyword)
        {
            var nowVnt = GetVntNow();
            var targetMonth = month ?? nowVnt.Month;
            var targetYear = year ?? nowVnt.Year;
            var normalizedStatus = NormalizeHistoryStatus(status);
            var searchTerm = keyword?.Trim();

            var query = _context.EmployeeLeaveRequests
                .AsNoTracking()
                .Include(r => r.Employee)
                .Where(r => (r.Status == "Approved" || r.Status == "Rejected")
                            && r.StartDate.Month == targetMonth
                            && r.StartDate.Year == targetYear);

            if (normalizedStatus != null)
                query = query.Where(r => r.Status == normalizedStatus);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(r => (r.Employee.LastName + " " + r.Employee.FirstName).Contains(searchTerm));

            var records = await query
                .OrderByDescending(r => r.ReviewedAtUtc ?? DateTime.MinValue)
                .ThenByDescending(r => r.StartDate)
                .ThenByDescending(r => r.CreatedAtUtc)
                .Select(r => new
                {
                    r.Id,
                    r.EmployeeId,
                    employeeName = r.Employee.LastName + " " + r.Employee.FirstName,
                    r.StartDate,
                    r.EndDate,
                    r.IsPaid,
                    r.Reason,
                    r.Status,
                    r.ReviewNote,
                    r.ReviewedByEmployeeId,
                    r.ReviewedAtUtc,
                    r.CreatedAtUtc
                })
                .ToListAsync();

            var reviewerIds = records
                .Where(r => r.ReviewedByEmployeeId.HasValue)
                .Select(r => r.ReviewedByEmployeeId!.Value)
                .Distinct()
                .ToList();

            var reviewers = await _context.Employees
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(e => reviewerIds.Contains(e.Id))
                .Select(e => new
                {
                    e.Id,
                    name = e.Account.Role.Name == "Manager" ? "Quản lý" : e.LastName + " " + e.FirstName
                })
                .ToDictionaryAsync(e => e.Id, e => e.name);

            var data = records.Select(r => new
            {
                id = r.Id,
                employeeId = r.EmployeeId,
                employeeName = r.employeeName,
                startDate = r.StartDate.ToString("dd/MM/yyyy"),
                endDate = r.EndDate.ToString("dd/MM/yyyy"),
                isPaid = r.IsPaid,
                reason = r.Reason,
                status = r.Status,
                reviewNote = r.ReviewNote,
                reviewerName = r.ReviewedByEmployeeId.HasValue && reviewers.TryGetValue(r.ReviewedByEmployeeId.Value, out var reviewerName)
                    ? reviewerName
                    : "Hệ thống",
                reviewedAt = FormatVntDateTime(r.ReviewedAtUtc),
                createdAt = r.CreatedAtUtc
            });

            return Json(new { success = true, data });
        }

        [HttpPost("Api/Leave/Approve")]
        public async Task<IActionResult> ApproveLeave([FromBody] LeaveDecisionDto? model)
        {
            if (model == null || model.RequestId <= 0)
                return Json(new { success = false, message = "Dữ liệu đơn nghỉ phép không hợp lệ." });

            var managerEmployeeId = await GetCurrentEmployeeIdAsync();
            if (managerEmployeeId == null)
                return Json(new { success = false, message = "Không tìm thấy hồ sơ nhân viên của Quản lý." });

            var record = await _context.EmployeeLeaveRequests
                .AsNoTracking()
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == model.RequestId);
            if (record == null)
                return Json(new { success = false, message = "Không tìm thấy đơn nghỉ phép." });

            var reviewedAtUtc = DateTime.UtcNow;
            var updatedRows = await _context.EmployeeLeaveRequests
                .Where(r => r.Id == model.RequestId && r.Status == "Pending")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.Status, "Approved")
                    .SetProperty(r => r.ReviewedByEmployeeId, managerEmployeeId)
                    .SetProperty(r => r.ReviewedAtUtc, reviewedAtUtc)
                    .SetProperty(r => r.ReviewNote, model.ReviewNote));

            if (updatedRows == 0)
                return Json(new { success = false, message = "Đơn nghỉ phép này đã được xử lý trước đó." });

            record.Status = "Approved";
            record.ReviewedByEmployeeId = managerEmployeeId;
            record.ReviewedAtUtc = reviewedAtUtc;
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
                                ReviewedAtUtc = reviewedAtUtc,
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
                            existing.ReviewedAtUtc = reviewedAtUtc;
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
        public async Task<IActionResult> RejectLeave([FromBody] LeaveDecisionDto? model)
        {
            if (model == null || model.RequestId <= 0)
                return Json(new { success = false, message = "Dữ liệu đơn nghỉ phép không hợp lệ." });

            var managerEmployeeId = await GetCurrentEmployeeIdAsync();
            if (managerEmployeeId == null)
                return Json(new { success = false, message = "Không tìm thấy hồ sơ nhân viên của Quản lý." });

            var record = await _context.EmployeeLeaveRequests
                .AsNoTracking()
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == model.RequestId);
            if (record == null)
                return Json(new { success = false, message = "Không tìm thấy đơn nghỉ phép." });

            var reviewedAtUtc = DateTime.UtcNow;
            var updatedRows = await _context.EmployeeLeaveRequests
                .Where(r => r.Id == model.RequestId && r.Status == "Pending")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.Status, "Rejected")
                    .SetProperty(r => r.ReviewedByEmployeeId, managerEmployeeId)
                    .SetProperty(r => r.ReviewedAtUtc, reviewedAtUtc)
                    .SetProperty(r => r.ReviewNote, model.ReviewNote));

            if (updatedRows == 0)
                return Json(new { success = false, message = "Đơn nghỉ phép này đã được xử lý trước đó." });

            record.Status = "Rejected";
            record.ReviewedByEmployeeId = managerEmployeeId;
            record.ReviewedAtUtc = reviewedAtUtc;
            record.ReviewNote = model.ReviewNote;

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
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.AccountId == accountId);

            return employee?.Id;
        }

        private static bool TryGetAttendanceDecision(AttendanceDecisionDto? model, out DateOnly date, out string message)
        {
            date = default;
            message = string.Empty;

            if (model == null || model.EmployeeId <= 0)
            {
                message = "Dữ liệu chấm công không hợp lệ.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(model.Date))
            {
                message = "Ngày chấm công không hợp lệ.";
                return false;
            }

            if (!DateOnly.TryParseExact(
                    model.Date,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out date))
            {
                message = "Ngày chấm công không đúng định dạng yyyy-MM-dd.";
                return false;
            }

            return true;
        }

        private static string? NormalizeHistoryStatus(string? status)
        {
            if (string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase))
                return "Approved";

            if (string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase))
                return "Rejected";

            return null;
        }

        private static string? FormatVntTime(DateTime? utc)
        {
            return utc.HasValue
                ? WorkAttendanceCalculator.ToVnt(utc.Value).ToString("HH:mm")
                : null;
        }

        private static string? FormatVntDateTime(DateTime? utc)
        {
            return utc.HasValue
                ? WorkAttendanceCalculator.ToVnt(utc.Value).ToString("dd/MM/yyyy HH:mm")
                : null;
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

