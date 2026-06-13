using datn.Data;
using datn.Models;
using Microsoft.EntityFrameworkCore;

namespace datn.Services
{
    public interface IUnauthorizedAbsenceService
    {
        Task<UnauthorizedAbsenceProcessResult> ProcessTodayAsync(CancellationToken cancellationToken = default);
        Task<UnauthorizedAbsenceProcessResult> ProcessDateAsync(DateOnly date, TimeSpan currentVntTime, CancellationToken cancellationToken = default);
    }

    public sealed record UnauthorizedAbsenceProcessResult(int RecordsCreated, int ClassesProcessed);

    public class UnauthorizedAbsenceService : IUnauthorizedAbsenceService
    {
        private static readonly TimeSpan UnauthorizedAbsenceCutoff = new(6, 40, 0);
        private const string UnauthorizedAbsentNote = "Nghỉ không phép: không check-in sau 06:40";

        private readonly AppDbContext _context;
        private readonly ITimeAttendanceWindowService _attendanceWindowService;
        private readonly IClassCoverageService _classCoverageService;
        private readonly ILogger<UnauthorizedAbsenceService> _logger;

        public UnauthorizedAbsenceService(
            AppDbContext context,
            ITimeAttendanceWindowService attendanceWindowService,
            IClassCoverageService classCoverageService,
            ILogger<UnauthorizedAbsenceService> logger)
        {
            _context = context;
            _attendanceWindowService = attendanceWindowService;
            _classCoverageService = classCoverageService;
            _logger = logger;
        }

        public Task<UnauthorizedAbsenceProcessResult> ProcessTodayAsync(CancellationToken cancellationToken = default)
        {
            var nowVnt = _attendanceWindowService.GetVntNow();
            return ProcessDateAsync(DateOnly.FromDateTime(nowVnt.DateTime), nowVnt.TimeOfDay, cancellationToken);
        }

        public async Task<UnauthorizedAbsenceProcessResult> ProcessDateAsync(
            DateOnly date,
            TimeSpan currentVntTime,
            CancellationToken cancellationToken = default)
        {
            if (date.DayOfWeek == DayOfWeek.Sunday || currentVntTime < UnauthorizedAbsenceCutoff)
                return new UnauthorizedAbsenceProcessResult(0, 0);

            var isHoliday = await _context.Holidays.AnyAsync(h => h.IsActive && h.Date == date, cancellationToken);
            if (isHoliday)
                return new UnauthorizedAbsenceProcessResult(0, 0);

            var assignedTeachers = await _context.Assignments
                .Where(a => a.IsActive
                            && a.Employee.IsActive
                            && a.Employee.Account.IsActive
                            && a.StartDate <= date
                            && (a.EndDate == null || a.EndDate >= date))
                .Select(a => new { a.EmployeeId, a.ClassId })
                .Distinct()
                .ToListAsync(cancellationToken);

            if (assignedTeachers.Count == 0)
                return new UnauthorizedAbsenceProcessResult(0, 0);

            var assignedTeacherIds = assignedTeachers
                .Select(a => a.EmployeeId)
                .Distinct()
                .ToList();

            var approvedLeaveTeacherIds = await _context.EmployeeLeaveRequests
                .Where(r => r.Status == "Approved"
                            && assignedTeacherIds.Contains(r.EmployeeId)
                            && r.StartDate <= date
                            && r.EndDate >= date)
                .Select(r => r.EmployeeId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var existingAttendanceTeacherIds = await _context.WorkAttendances
                .Where(w => w.Date == date && assignedTeacherIds.Contains(w.EmployeeId))
                .Select(w => w.EmployeeId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var teacherIdsToMark = assignedTeacherIds
                .Except(approvedLeaveTeacherIds)
                .Except(existingAttendanceTeacherIds)
                .ToList();

            if (teacherIdsToMark.Count == 0)
                return new UnauthorizedAbsenceProcessResult(0, 0);

            foreach (var teacherId in teacherIdsToMark)
            {
                _context.WorkAttendances.Add(new WorkAttendance
                {
                    EmployeeId = teacherId,
                    Date = date,
                    Status = WorkAttendanceStatuses.UnauthorizedAbsent,
                    WorkUnit = 0m,
                    PenaltyAmount = 0m,
                    Note = UnauthorizedAbsentNote,
                    ReviewNote = "Hệ thống tự động ghi nhận nghỉ không phép"
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            var affectedClassIds = assignedTeachers
                .Where(a => teacherIdsToMark.Contains(a.EmployeeId))
                .Select(a => a.ClassId)
                .Distinct()
                .ToList();

            var classesProcessed = 0;
            foreach (var classId in affectedClassIds)
            {
                try
                {
                    await _classCoverageService.ProcessClassDateAsync(classId, date, "Nghỉ không phép");
                    classesProcessed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Class coverage processing failed for unauthorized absence in class {ClassId} on {Date}",
                        classId,
                        date);
                }
            }

            _logger.LogInformation(
                "Processed unauthorized absences for {Date}: recordsCreated={RecordsCreated}, classesProcessed={ClassesProcessed}",
                date,
                teacherIdsToMark.Count,
                classesProcessed);

            return new UnauthorizedAbsenceProcessResult(teacherIdsToMark.Count, classesProcessed);
        }
    }
}
