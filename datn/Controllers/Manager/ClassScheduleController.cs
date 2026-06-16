using datn.Data;
using datn.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace datn.Controllers.Manager
{
    [Authorize(Roles = "Manager")]
    [Route("Manager")]
    public class ClassScheduleController : BaseController
    {
        private static readonly TimeOnly SchoolStart = new(6, 45);
        private static readonly TimeOnly LunchStart = new(11, 0);
        private static readonly TimeOnly LunchEnd = new(14, 0);
        private static readonly TimeOnly SchoolEnd = new(17, 0);
        private const string PlaySubjectName = "Hoạt động vui chơi";
        private const string PlaySubjectCreateMessage = "Vui lòng tạo môn học \"Hoạt động vui chơi\" trong tab Danh mục môn học trước khi xếp lịch khung 10:00 - 11:00.";
        private static readonly TimeOnly PlayStart = new(10, 0);
        private static readonly TimeOnly PlayEnd = new(11, 0);
        private static readonly ScheduleSlot[] AllowedScheduleSlots =
        [
            new("Học chính buổi sáng", new TimeOnly(7, 30), new TimeOnly(10, 0)),
            new("Vui chơi", new TimeOnly(10, 0), new TimeOnly(11, 0)),
            new("Học chính buổi chiều", new TimeOnly(14, 0), new TimeOnly(16, 30))
        ];
        private static readonly ScheduleSlot[] LockedScheduleSlots =
        [
            new("Đón trẻ & Ăn sáng", new TimeOnly(6, 45), new TimeOnly(7, 30)),
            new("Ăn & Ngủ trưa", LunchStart, LunchEnd),
            new("Trả trẻ", new TimeOnly(16, 30), SchoolEnd)
        ];

        public ClassScheduleController(AppDbContext context) : base(context) { }

        [HttpGet("Api/ClassSchedules")]
        public async Task<IActionResult> GetClassSchedules(int classId, string? weekStart = null)
        {
            var selectedWeekStart = ResolveWeekStart(weekStart);
            var selectedWeekEnd = selectedWeekStart.AddDays(5);

            var schedules = await _context.ClassSchedules
                .Where(cs => cs.ClassId == classId
                    && cs.EffectiveFrom <= selectedWeekEnd
                    && (cs.EffectiveTo == null || cs.EffectiveTo >= selectedWeekStart))
                .Include(cs => cs.Subject)
                .Include(cs => cs.Location)
                .OrderBy(cs => cs.DayOfWeek)
                .ThenBy(cs => cs.StartTime)
                .ToListAsync();

            var assignments = await _context.Assignments
                .Where(a => a.ClassId == classId
                    && a.IsActive
                    && a.StartDate <= selectedWeekEnd
                    && (a.EndDate == null || a.EndDate >= selectedWeekStart))
                .Include(a => a.Employee)
                .ToListAsync();

            return Json(new
            {
                success = true,
                weekStart = selectedWeekStart.ToString("yyyy-MM-dd"),
                weekEnd = selectedWeekEnd.ToString("yyyy-MM-dd"),
                data = schedules.Select(cs => new
                {
                    id = cs.Id,
                    classId = cs.ClassId,
                    subjectId = cs.SubjectId,
                    subjectName = cs.Subject.Name,
                    employeeId = cs.EmployeeId,
                    teacherName = string.Join(", ", assignments
                        .Where(a => DateRangesOverlap(a.StartDate, a.EndDate, cs.EffectiveFrom, cs.EffectiveTo))
                        .Select(a => a.Employee.FullName)
                        .Distinct()),
                    dayOfWeek = cs.DayOfWeek,
                    dayLabel = GetVietnameseDayLabel(cs.DayOfWeek),
                    startTime = cs.StartTime.ToString("HH:mm"),
                    endTime = cs.EndTime.ToString("HH:mm"),
                    locationId = cs.LocationId,
                    locationName = cs.Location?.Name,
                    effectiveFrom = cs.EffectiveFrom.ToString("yyyy-MM-dd"),
                    effectiveTo = cs.EffectiveTo?.ToString("yyyy-MM-dd"),
                    note = cs.Note,
                    isActive = cs.IsActive
                })
            });
        }

        [HttpGet("Api/ClassSchedule/{id:int}")]
        public async Task<IActionResult> GetClassSchedule(int id)
        {
            var schedule = await _context.ClassSchedules.FindAsync(id);
            if (schedule == null)
                return Json(new { success = false, message = "Không tìm thấy thời khóa biểu." });

            return Json(new
            {
                success = true,
                data = new
                {
                    id = schedule.Id,
                    classId = schedule.ClassId,
                    subjectId = schedule.SubjectId,
                    employeeId = schedule.EmployeeId,
                    dayOfWeek = schedule.DayOfWeek,
                    startTime = schedule.StartTime.ToString("HH:mm"),
                    endTime = schedule.EndTime.ToString("HH:mm"),
                    locationId = schedule.LocationId,
                    effectiveFrom = schedule.EffectiveFrom.ToString("yyyy-MM-dd"),
                    effectiveTo = schedule.EffectiveTo?.ToString("yyyy-MM-dd"),
                    note = schedule.Note,
                    isActive = schedule.IsActive
                }
            });
        }

        [HttpPost("Api/ClassSchedule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClassSchedule([FromBody] SaveClassScheduleViewModel model)
        {
            var validation = await ValidateScheduleRequestAsync(model, null);
            if (!validation.Success)
                return Json(new { success = false, message = validation.Message });

            var schedule = new ClassSchedule
            {
                ClassId = model.ClassId,
                SubjectId = model.SubjectId,
                EmployeeId = null,
                DayOfWeek = model.DayOfWeek,
                StartTime = TimeOnly.Parse(model.StartTime),
                EndTime = TimeOnly.Parse(model.EndTime),
                LocationId = model.LocationId,
                EffectiveFrom = validation.EffectiveFrom!.Value,
                EffectiveTo = validation.EffectiveTo,
                Note = model.Note?.Trim(),
                IsActive = model.IsActive
            };

            _context.ClassSchedules.Add(schedule);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Tạo thời khóa biểu thành công." });
        }

        [HttpPut("Api/ClassSchedule/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClassSchedule(int id, [FromBody] SaveClassScheduleViewModel model)
        {
            var schedule = await _context.ClassSchedules.FindAsync(id);
            if (schedule == null)
                return Json(new { success = false, message = "Không tìm thấy thời khóa biểu." });

            var validation = await ValidateScheduleRequestAsync(model, id);
            if (!validation.Success)
                return Json(new { success = false, message = validation.Message });

            schedule.ClassId = model.ClassId;
            schedule.SubjectId = model.SubjectId;
            schedule.EmployeeId = null;
            schedule.DayOfWeek = model.DayOfWeek;
            schedule.StartTime = TimeOnly.Parse(model.StartTime);
            schedule.EndTime = TimeOnly.Parse(model.EndTime);
            schedule.LocationId = model.LocationId;
            schedule.EffectiveFrom = validation.EffectiveFrom!.Value;
            schedule.EffectiveTo = validation.EffectiveTo;
            schedule.Note = model.Note?.Trim();
            schedule.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thời khóa biểu thành công." });
        }

        [HttpDelete("Api/ClassSchedule/{id:int}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var schedule = await _context.ClassSchedules.FindAsync(id);
            if (schedule == null)
                return Json(new { success = false, message = "Không tìm thấy lịch học." });

            schedule.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa lịch học thành công." });
        }

        [HttpPost("Api/ClassSchedule/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateSchedule(int id)
        {
            var schedule = await _context.ClassSchedules.IgnoreQueryFilters().FirstOrDefaultAsync(cs => cs.Id == id);
            if (schedule == null)
                return Json(new { success = false, message = "Không tìm thấy." });

            schedule.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục lịch học thành công." });
        }

        private async Task<ScheduleValidationResult> ValidateScheduleRequestAsync(SaveClassScheduleViewModel model, int? scheduleId)
        {
            if (!await _context.Classes.AnyAsync(c => c.Id == model.ClassId))
                return ScheduleValidationResult.Fail("L\u1edbp h\u1ecdc kh\u00f4ng t\u1ed3n t\u1ea1i.");

            if (!await _context.Subjects.AnyAsync(s => s.Id == model.SubjectId && s.IsActive))
                return ScheduleValidationResult.Fail("M\u00f4n h\u1ecdc kh\u00f4ng t\u1ed3n t\u1ea1i ho\u1eb7c \u0111\u00e3 ng\u1eebng s\u1eed d\u1ee5ng.");

            var selectedSubject = await _context.Subjects.FindAsync(model.SubjectId);

            if (model.LocationId.HasValue && model.LocationId > 0)
            {
                if (!await _context.Locations.AnyAsync(l => l.Id == model.LocationId))
                    return ScheduleValidationResult.Fail("\u0110\u1ecba \u0111i\u1ec3m h\u1ecdc kh\u00f4ng t\u1ed3n t\u1ea1i.");
            }

            if (model.DayOfWeek < 1 || model.DayOfWeek > 6)
                return ScheduleValidationResult.Fail("Ch\u1ec9 \u0111\u01b0\u1ee3c t\u1ea1o l\u1ecbch t\u1eeb Th\u1ee9 2 \u0111\u1ebfn Th\u1ee9 7.");

            if (!TimeOnly.TryParse(model.StartTime, out var startTime) || !TimeOnly.TryParse(model.EndTime, out var endTime))
                return ScheduleValidationResult.Fail("Khung gi\u1edd kh\u00f4ng h\u1ee3p l\u1ec7.");

            if (!DateOnly.TryParse(model.EffectiveFrom, out var effectiveFrom))
                return ScheduleValidationResult.Fail("Ng\u00e0y hi\u1ec7u l\u1ef1c b\u1eaft \u0111\u1ea7u kh\u00f4ng h\u1ee3p l\u1ec7.");

            DateOnly? effectiveTo = null;
            if (!string.IsNullOrWhiteSpace(model.EffectiveTo))
            {
                if (!DateOnly.TryParse(model.EffectiveTo, out var parsedEffectiveTo))
                    return ScheduleValidationResult.Fail("Ng\u00e0y hi\u1ec7u l\u1ef1c k\u1ebft th\u00fac kh\u00f4ng h\u1ee3p l\u1ec7.");
                effectiveTo = parsedEffectiveTo;
            }

            if (endTime <= startTime)
                return ScheduleValidationResult.Fail("Gi\u1edd k\u1ebft th\u00fac ph\u1ea3i l\u1edbn h\u01a1n gi\u1edd b\u1eaft \u0111\u1ea7u.");

            if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
                return ScheduleValidationResult.Fail("Ng\u00e0y k\u1ebft th\u00fac ph\u1ea3i l\u1edbn h\u01a1n ho\u1eb7c b\u1eb1ng ng\u00e0y b\u1eaft \u0111\u1ea7u.");

            if (startTime < SchoolStart || endTime > SchoolEnd)
                return ScheduleValidationResult.Fail("Ch\u1ec9 \u0111\u01b0\u1ee3c x\u1ebfp l\u1ecbch trong khung 06:45 - 17:00.");

            var lockedSlot = LockedScheduleSlots.FirstOrDefault(slot => TimeRangesOverlap(slot.Start, slot.End, startTime, endTime));
            if (lockedSlot != null)
                return ScheduleValidationResult.Fail($"Kh\u00f4ng \u0111\u01b0\u1ee3c x\u1ebfp l\u1ecbch trong khung {lockedSlot.Name} ({lockedSlot.Start:HH:mm} - {lockedSlot.End:HH:mm}).");

            if (startTime < LunchEnd && endTime > LunchStart)
                return ScheduleValidationResult.Fail("Th\u1eddi kh\u00f3a bi\u1ec3u kh\u00f4ng \u0111\u01b0\u1ee3c ch\u1ed3ng l\u00ean khung ngh\u1ec9 tr\u01b0a 11:00 - 14:00.");

            var matchingSlot = AllowedScheduleSlots.FirstOrDefault(slot => startTime >= slot.Start && endTime <= slot.End);
            if (matchingSlot == null)
                return ScheduleValidationResult.Fail("Ch\u1ec9 \u0111\u01b0\u1ee3c x\u1ebfp l\u1ecbch trong c\u00e1c khung 07:30-10:00, 10:00-11:00 ho\u1eb7c 14:00-16:30.");

            if (IsPlayTimeRange(startTime, endTime) && !IsPlaySubject(selectedSubject?.Name))
                return ScheduleValidationResult.Fail($"Khung 10:00 - 11:00 ch\u1ec9 \u0111\u01b0\u1ee3c ch\u1ecdn m\u00f4n h\u1ecdc \"{PlaySubjectName}\". {PlaySubjectCreateMessage}");

            var assignmentsCoveringStart = await _context.Assignments
                .Where(a => a.ClassId == model.ClassId
                            && a.IsActive
                            && a.StartDate <= effectiveFrom
                            && (a.EndDate == null || a.EndDate >= effectiveFrom))
                .ToListAsync();

            if (assignmentsCoveringStart.Count == 0)
                return ScheduleValidationResult.Fail("L\u1edbp ch\u01b0a c\u00f3 gi\u00e1o vi\u00ean ph\u1ee5 tr\u00e1ch t\u1ea1i ng\u00e0y b\u1eaft \u0111\u1ea7u hi\u1ec7u l\u1ef1c c\u1ee7a l\u1ecbch.");

            if (effectiveTo == null)
            {
                if (!assignmentsCoveringStart.Any(a => a.EndDate == null))
                {
                    effectiveTo = assignmentsCoveringStart
                        .Where(a => a.EndDate.HasValue)
                        .Max(a => a.EndDate);
                }
            }
            else
            {
                var hasCoveredTeacher = assignmentsCoveringStart.Any(a => a.EndDate == null || a.EndDate >= effectiveTo);
                if (!hasCoveredTeacher)
                {
                    var maxCoveredDate = assignmentsCoveringStart
                        .Where(a => a.EndDate.HasValue)
                        .Select(a => a.EndDate!.Value)
                        .DefaultIfEmpty(effectiveFrom)
                        .Max();

                    return ScheduleValidationResult.Fail($"Gi\u00e1o vi\u00ean ph\u1ee5 tr\u00e1ch l\u1edbp ch\u1ec9 bao ph\u1ee7 \u0111\u1ebfn {maxCoveredDate:dd/MM/yyyy}. Vui l\u00f2ng ch\u1ecdn ng\u00e0y k\u1ebft th\u00fac kh\u00f4ng v\u01b0\u1ee3t qu\u00e1 ng\u00e0y n\u00e0y.");
                }
            }

            var sameDayQuery = _context.ClassSchedules
                .Where(cs => cs.DayOfWeek == model.DayOfWeek && cs.IsActive);

            if (scheduleId.HasValue)
            {
                var currentScheduleId = scheduleId.Value;
                sameDayQuery = sameDayQuery.Where(cs => cs.Id != currentScheduleId);
            }

            var sameDaySchedules = await sameDayQuery.ToListAsync();

            var classOverlapSchedule = sameDaySchedules.FirstOrDefault(cs =>
                cs.ClassId == model.ClassId
                && DateRangesOverlap(cs.EffectiveFrom, cs.EffectiveTo, effectiveFrom, effectiveTo)
                && TimeRangesOverlap(cs.StartTime, cs.EndTime, startTime, endTime));

            if (classOverlapSchedule != null)
            {
                var subject = await _context.Subjects.FindAsync(classOverlapSchedule.SubjectId);
                return ScheduleValidationResult.Fail($"L\u1edbp h\u1ecdc \u0111\u00e3 c\u00f3 ti\u1ebft '{subject?.Name}' tr\u00f9ng khung gi\u1edd n\u00e0y ({classOverlapSchedule.StartTime:HH:mm} - {classOverlapSchedule.EndTime:HH:mm}).");
            }

            if (model.LocationId.HasValue)
            {
                var locationOverlap = sameDaySchedules.Any(cs =>
                    cs.LocationId == model.LocationId
                    && DateRangesOverlap(cs.EffectiveFrom, cs.EffectiveTo, effectiveFrom, effectiveTo)
                    && TimeRangesOverlap(cs.StartTime, cs.EndTime, startTime, endTime));

                if (locationOverlap)
                    return ScheduleValidationResult.Fail("Ph\u00f2ng h\u1ecdc/\u0110\u1ecba \u0111i\u1ec3m n\u00e0y \u0111\u00e3 \u0111\u01b0\u1ee3c s\u1eed d\u1ee5ng cho l\u1edbp kh\u00e1c trong khung gi\u1edd n\u00e0y.");
            }

            return ScheduleValidationResult.Ok(effectiveFrom, effectiveTo);
        }

        private static bool DateRangesOverlap(DateOnly leftStart, DateOnly? leftEnd, DateOnly rightStart, DateOnly? rightEnd)
        {
            var normalizedLeftEnd = leftEnd ?? DateOnly.MaxValue;
            var normalizedRightEnd = rightEnd ?? DateOnly.MaxValue;
            return leftStart <= normalizedRightEnd && rightStart <= normalizedLeftEnd;
        }

        private static bool TimeRangesOverlap(TimeOnly leftStart, TimeOnly leftEnd, TimeOnly rightStart, TimeOnly rightEnd)
        {
            return leftStart < rightEnd && rightStart < leftEnd;
        }

        private static bool IsPlayTimeRange(TimeOnly startTime, TimeOnly endTime)
        {
            return startTime >= PlayStart && endTime <= PlayEnd;
        }

        private static bool IsPlaySubject(string? subjectName)
        {
            return string.Equals(
                NormalizeSubjectName(subjectName),
                NormalizeSubjectName(PlaySubjectName),
                StringComparison.InvariantCultureIgnoreCase);
        }

        private static string NormalizeSubjectName(string? value)
        {
            return string.Join(' ', (value ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static DateOnly ResolveWeekStart(string? weekStart)
        {
            var date = DateOnly.TryParse(weekStart, out var parsed)
                ? parsed
                : DateOnly.FromDateTime(DateTime.Today);

            var offset = date.DayOfWeek switch
            {
                DayOfWeek.Monday => 0,
                DayOfWeek.Tuesday => 1,
                DayOfWeek.Wednesday => 2,
                DayOfWeek.Thursday => 3,
                DayOfWeek.Friday => 4,
                DayOfWeek.Saturday => 5,
                DayOfWeek.Sunday => 6,
                _ => 0
            };

            return date.AddDays(-offset);
        }

        private static string GetVietnameseDayLabel(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "Thứ 2",
                2 => "Thứ 3",
                3 => "Thứ 4",
                4 => "Thứ 5",
                5 => "Thứ 6",
                6 => "Thứ 7",
                _ => "Không xác định"
            };
        }

        private sealed record ScheduleValidationResult(bool Success, string? Message, DateOnly? EffectiveFrom, DateOnly? EffectiveTo)
        {
            public static ScheduleValidationResult Ok(DateOnly effectiveFrom, DateOnly? effectiveTo)
                => new(true, null, effectiveFrom, effectiveTo);

            public static ScheduleValidationResult Fail(string message)
                => new(false, message, null, null);
        }

        private sealed record ScheduleSlot(string Name, TimeOnly Start, TimeOnly End);
    }
}
