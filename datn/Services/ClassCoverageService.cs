using datn.Data;
using datn.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;

namespace datn.Services
{
    public interface IClassCoverageService
    {
        Task<CoverageProcessResult> ProcessLeaveApprovalAsync(EmployeeLeaveRequest leaveRequest);
        Task<CoverageProcessResult> ProcessClassDateAsync(int classId, DateOnly date, string? reason = null);
        Task<CoverageProcessResult> ProcessEmployeeAttendanceApprovedAsync(int employeeId, DateOnly date);
        Task<bool> CanClassOperateOnDateAsync(int classId, DateOnly date);

        [Obsolete("Use ProcessLeaveApprovalAsync")]
        Task<int> HandleApprovedLeaveAsync(EmployeeLeaveRequest leaveRequest);
    }

    public class ClassCoverageService : IClassCoverageService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ClassCoverageOptions _options;
        private readonly ILogger<ClassCoverageService> _logger;

        public ClassCoverageService(
            AppDbContext context,
            INotificationService notificationService,
            IEmailService emailService,
            IOptions<ClassCoverageOptions> options,
            ILogger<ClassCoverageService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<int> HandleApprovedLeaveAsync(EmployeeLeaveRequest leaveRequest)
        {
            var result = await ProcessLeaveApprovalAsync(leaveRequest);
            return result.ParentsNotified;
        }

        public async Task<CoverageProcessResult> ProcessLeaveApprovalAsync(EmployeeLeaveRequest leaveRequest)
        {
            var aggregate = new CoverageProcessResult();
            if (leaveRequest.Status != "Approved")
                return aggregate;

            for (var date = leaveRequest.StartDate; date <= leaveRequest.EndDate; date = date.AddDays(1))
            {
                var affectedClassIds = await _context.Assignments
                    .Where(a => a.EmployeeId == leaveRequest.EmployeeId
                                && a.IsActive
                                && a.StartDate <= date
                                && (a.EndDate == null || a.EndDate >= date))
                    .Select(a => a.ClassId)
                    .Distinct()
                    .ToListAsync();

                foreach (var classId in affectedClassIds)
                {
                    var dayResult = await ProcessClassDateAsync(classId, date, leaveRequest.Reason);
                    aggregate.ParentsNotified += dayResult.ParentsNotified;
                    aggregate.BonusesGranted += dayResult.BonusesGranted;
                }
            }

            return aggregate;
        }

        public async Task<CoverageProcessResult> ProcessClassDateAsync(int classId, DateOnly date, string? reason = null)
        {
            var result = new CoverageProcessResult();

            if (!await CanClassOperateOnDateAsync(classId, date))
            {
                if (await NotifyClassCancelledAsync(classId, date, reason))
                    result.ParentsNotified = 1;
            }
            else
            {
                result.BonusesGranted = await TryGrantSoloCoverageBonusesAsync(classId, date);
            }

            return result;
        }

        public async Task<CoverageProcessResult> ProcessEmployeeAttendanceApprovedAsync(int employeeId, DateOnly date)
        {
            var result = new CoverageProcessResult();
            if (!_options.RequirePresentCheckIn)
                return result;

            if (!await HasApprovedCheckInAsync(employeeId, date))
                return result;

            var classIds = await GetActiveAssignmentClassIdsAsync(employeeId, date);
            foreach (var classId in classIds)
            {
                result.BonusesGranted += await TryGrantSoloCoverageBonusesAsync(classId, date);
            }

            return result;
        }

        public async Task<bool> CanClassOperateOnDateAsync(int classId, DateOnly date)
        {
            var activeTeacherIds = await GetActiveTeacherIdsAsync(classId, date);

            if (activeTeacherIds.Count == 0)
                return true;

            var absentTeacherIds = await GetAbsentTeacherIdsAsync(activeTeacherIds, date);
            var presentTeacherIds = await GetPresentTeacherIdsAsync(activeTeacherIds, date);

            if (activeTeacherIds.Any(id => !absentTeacherIds.Contains(id) || presentTeacherIds.Contains(id)))
                return true;

            var dayOfWeek = GetSchoolDayOfWeek(date);
            if (!dayOfWeek.HasValue)
                return true;

            return await HasConfirmedSubstituteAsync(classId, date, dayOfWeek.Value);
        }

        private async Task<int> TryGrantSoloCoverageBonusesAsync(int classId, DateOnly date)
        {
            var activeTeacherIds = await GetActiveTeacherIdsAsync(classId, date);
            if (activeTeacherIds.Count != 2)
                return 0;

            var absentTeacherIds = await GetAbsentTeacherIdsAsync(activeTeacherIds, date);
            if (absentTeacherIds.Count != 1)
                return 0;

            var dayOfWeek = GetSchoolDayOfWeek(date);
            if (dayOfWeek.HasValue && await HasConfirmedSubstituteAsync(classId, date, dayOfWeek.Value))
                return 0;

            var absentId = absentTeacherIds[0];
            var coveringId = activeTeacherIds.First(id => id != absentId);

            if (_options.RequirePresentCheckIn && !await HasApprovedCheckInAsync(coveringId, date))
                return 0;

            var leaveRequestId = await _context.EmployeeLeaveRequests
                .Where(r => r.EmployeeId == absentId
                            && r.Status == "Approved"
                            && r.StartDate <= date
                            && r.EndDate >= date)
                .OrderByDescending(r => r.ReviewedAtUtc)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync();

            return await GrantBonusIfNotExistsAsync(coveringId, classId, date, absentId, leaveRequestId);
        }

        private async Task<int> GrantBonusIfNotExistsAsync(
            int employeeId,
            int classId,
            DateOnly date,
            int absentEmployeeId,
            int? leaveRequestId)
        {
            var exists = await _context.ClassCoverageBonuses.AnyAsync(b =>
                b.EmployeeId == employeeId
                && b.ClassId == classId
                && b.Date == date
                && b.AbsentEmployeeId == absentEmployeeId
                && b.Status == "Active");

            if (exists)
                return 0;

            var className = await _context.Classes
                .Where(c => c.Id == classId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync() ?? $"Lớp #{classId}";

            var absentName = await _context.Employees
                .Where(e => e.Id == absentEmployeeId)
                .Select(e => e.LastName + " " + e.FirstName)
                .FirstOrDefaultAsync() ?? "đồng nghiệp";

            var amount = _options.SoloCoverageBonusAmount;
            if (amount <= 0)
                return 0;

            _context.ClassCoverageBonuses.Add(new ClassCoverageBonus
            {
                EmployeeId = employeeId,
                ClassId = classId,
                Date = date,
                AbsentEmployeeId = absentEmployeeId,
                LeaveRequestId = leaveRequestId,
                Amount = amount,
                Status = "Active",
                CreatedAtUtc = DateTime.UtcNow,
                Note = $"Thưởng phụ trách {className} khi {absentName.Trim()} nghỉ ngày {date:dd/MM/yyyy}"
            });
            await _context.SaveChangesAsync();

            await NotifyTeacherBonusAsync(employeeId, className, date, amount);

            _logger.LogInformation(
                "Granted solo coverage bonus {Amount} to employee {EmployeeId} for class {ClassId} on {Date}",
                amount, employeeId, classId, date);

            return 1;
        }

        private async Task NotifyTeacherBonusAsync(int employeeId, string className, DateOnly date, decimal amount)
        {
            var accountId = await _context.Employees
                .Where(e => e.Id == employeeId)
                .Select(e => e.AccountId)
                .FirstOrDefaultAsync();

            if (accountId == 0)
                return;

            var title = "Thưởng phụ trách lớp";
            var message =
                $"Bạn được thưởng {amount:N0}đ vì phụ trách lớp {className} ngày {date:dd/MM/yyyy} khi đồng nghiệp nghỉ và bạn đã check-in làm việc.";

            try
            {
                await _notificationService.SendToUserAsync(accountId, title, message, "success", "/TeacherSalary/MySalary");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify teacher {EmployeeId} about coverage bonus", employeeId);
            }
        }

        private async Task<List<int>> GetActiveTeacherIdsAsync(int classId, DateOnly date)
        {
            return await _context.Assignments
                .Where(a => a.ClassId == classId
                            && a.IsActive
                            && a.StartDate <= date
                            && (a.EndDate == null || a.EndDate >= date))
                .Select(a => a.EmployeeId)
                .Distinct()
                .ToListAsync();
        }

        private async Task<List<int>> GetActiveAssignmentClassIdsAsync(int employeeId, DateOnly date)
        {
            return await _context.Assignments
                .Where(a => a.EmployeeId == employeeId
                            && a.IsActive
                            && a.StartDate <= date
                            && (a.EndDate == null || a.EndDate >= date))
                .Select(a => a.ClassId)
                .Distinct()
                .ToListAsync();
        }

        private async Task<List<int>> GetAbsentTeacherIdsAsync(List<int> activeTeacherIds, DateOnly date)
        {
            var approvedLeaveTeacherIds = await _context.EmployeeLeaveRequests
                .Where(r => r.Status == "Approved"
                            && activeTeacherIds.Contains(r.EmployeeId)
                            && r.StartDate <= date
                            && r.EndDate >= date)
                .Select(r => r.EmployeeId)
                .Distinct()
                .ToListAsync();

            var unauthorizedAbsentTeacherIds = await _context.WorkAttendances
                .Where(w => w.Date == date
                            && activeTeacherIds.Contains(w.EmployeeId)
                            && w.Status == WorkAttendanceStatuses.UnauthorizedAbsent)
                .Select(w => w.EmployeeId)
                .Distinct()
                .ToListAsync();

            return approvedLeaveTeacherIds
                .Union(unauthorizedAbsentTeacherIds)
                .ToList();
        }

        private async Task<List<int>> GetPresentTeacherIdsAsync(List<int> activeTeacherIds, DateOnly date)
        {
            return await _context.WorkAttendances
                .Where(w => w.Date == date
                            && activeTeacherIds.Contains(w.EmployeeId)
                            && w.Status == WorkAttendanceStatuses.Approved
                            && w.CheckInAtUtc != null)
                .Select(w => w.EmployeeId)
                .Distinct()
                .ToListAsync();
        }

        private async Task<bool> HasApprovedCheckInAsync(int employeeId, DateOnly date)
        {
            return await _context.WorkAttendances.AnyAsync(w =>
                w.EmployeeId == employeeId
                && w.Date == date
                && w.Status == WorkAttendanceStatuses.Approved
                && w.CheckInAtUtc != null);
        }

        private async Task<bool> HasConfirmedSubstituteAsync(int classId, DateOnly date, int dayOfWeek)
        {
            return await _context.Substitutions
                .AnyAsync(s => s.Date == date
                               && s.Status == "Confirmed"
                               && s.ClassSchedule.ClassId == classId
                               && s.ClassSchedule.IsActive
                               && s.ClassSchedule.DayOfWeek == dayOfWeek
                               && s.ClassSchedule.EffectiveFrom <= date
                               && (s.ClassSchedule.EffectiveTo == null || s.ClassSchedule.EffectiveTo >= date));
        }

        private async Task<bool> NotifyClassCancelledAsync(int classId, DateOnly date, string? reason)
        {
            var classInfo = await _context.Classes
                .AsNoTracking()
                .Where(c => c.Id == classId)
                .Select(c => new { c.Id, c.Name })
                .FirstOrDefaultAsync();

            if (classInfo == null)
                return false;

            var className = classInfo.Name ?? $"Lớp #{classInfo.Id}";

            var parentRecipients = await _context.ParentStudents
                .Where(ps => ps.Student.ClassId == classId
                             && ps.Student.Status == StudentStatus.Active
                             && ps.Parent.IsActive
                             && ps.Parent.Account.IsActive)
                .Select(ps => new
                {
                    ps.Parent.AccountId,
                    ps.Parent.Account.Email
                })
                .GroupBy(x => x.AccountId)
                .Select(g => g.First())
                .ToListAsync();

            if (parentRecipients.Count == 0)
                return false;

            var title = $"Lớp {className} nghỉ học ngày {date:dd/MM/yyyy}";
            var message = BuildNotificationMessage(className, date, reason);
            var emailSubject = title;
            var emailBody = BuildEmailBody(className, date, reason);
            var url = "/Parent/Children";

            foreach (var recipient in parentRecipients)
            {
                if (await HasDuplicateNotificationAsync(recipient.AccountId, title, url, className, date))
                    continue;

                try
                {
                    await _notificationService.SendToUserAsync(recipient.AccountId, title, message, "warning", url);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send realtime notification to parent account {AccountId}", recipient.AccountId);
                }

                if (!string.IsNullOrWhiteSpace(recipient.Email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(recipient.Email, emailSubject, emailBody);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send email to {Email} for class cancellation", recipient.Email);
                    }
                }
            }

            return true;
        }

        private async Task<bool> HasDuplicateNotificationAsync(int accountId, string title, string url, string className, DateOnly date)
        {
            var dateText = date.ToString("dd/MM/yyyy");
            return await _context.Notifications.AnyAsync(n =>
                n.RecipientId == accountId &&
                n.Title == title &&
                n.Url == url &&
                n.Message.Contains(className) &&
                n.Message.Contains(dateText));
        }

        private static string BuildNotificationMessage(string className, DateOnly date, string? reason)
        {
            var safeReason = string.IsNullOrWhiteSpace(reason) ? "không có giáo viên phụ trách nào có mặt" : reason.Trim();
            return $"Lớp {className} nghỉ học ngày {date:dd/MM/yyyy}. Lý do: {safeReason}.";
        }

        private static string BuildEmailBody(string className, DateOnly date, string? reason)
        {
            var safeClassName = WebUtility.HtmlEncode(className);
            var safeReason = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(reason) ? "không có giáo viên phụ trách nào có mặt" : reason.Trim());
            var safeDate = WebUtility.HtmlEncode(date.ToString("dd/MM/yyyy"));

            return $@"
<div style=""font-family:Arial,sans-serif;line-height:1.6;color:#1f2937"">
  <h2 style=""margin:0 0 12px;color:#b45309"">Thông báo lớp nghỉ học</h2>
  <p>Xin chào phụ huynh,</p>
  <p>Lớp <strong>{safeClassName}</strong> sẽ nghỉ học vào ngày <strong>{safeDate}</strong>.</p>
  <p><strong>Lý do:</strong> {safeReason}</p>
  <p>Vui lòng theo dõi thông báo tiếp theo từ nhà trường.</p>
</div>";
        }

        private static int? GetSchoolDayOfWeek(DateOnly date)
        {
            return date.DayOfWeek switch
            {
                DayOfWeek.Monday => 1,
                DayOfWeek.Tuesday => 2,
                DayOfWeek.Wednesday => 3,
                DayOfWeek.Thursday => 4,
                DayOfWeek.Friday => 5,
                DayOfWeek.Saturday => 6,
                _ => null
            };
        }
    }
}
