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
            var validationMessage = await ValidateScheduleRequestAsync(model, null);
            if (validationMessage != null)
                return Json(new { success = false, message = validationMessage });

            var schedule = new ClassSchedule
            {
                ClassId = model.ClassId,
                SubjectId = model.SubjectId,
                EmployeeId = null,
                DayOfWeek = model.DayOfWeek,
                StartTime = TimeOnly.Parse(model.StartTime),
                EndTime = TimeOnly.Parse(model.EndTime),
                LocationId = model.LocationId,
                EffectiveFrom = DateOnly.Parse(model.EffectiveFrom),
                EffectiveTo = string.IsNullOrWhiteSpace(model.EffectiveTo) ? null : DateOnly.Parse(model.EffectiveTo),
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

            var validationMessage = await ValidateScheduleRequestAsync(model, id);
            if (validationMessage != null)
                return Json(new { success = false, message = validationMessage });

            schedule.ClassId = model.ClassId;
            schedule.SubjectId = model.SubjectId;
            schedule.EmployeeId = null;
            schedule.DayOfWeek = model.DayOfWeek;
            schedule.StartTime = TimeOnly.Parse(model.StartTime);
            schedule.EndTime = TimeOnly.Parse(model.EndTime);
            schedule.LocationId = model.LocationId;
            schedule.EffectiveFrom = DateOnly.Parse(model.EffectiveFrom);
            schedule.EffectiveTo = string.IsNullOrWhiteSpace(model.EffectiveTo) ? null : DateOnly.Parse(model.EffectiveTo);
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

        private async Task<string?> ValidateScheduleRequestAsync(SaveClassScheduleViewModel model, int? scheduleId)
        {
            if (!await _context.Classes.AnyAsync(c => c.Id == model.ClassId))
                return "Lớp học không tồn tại.";

            if (!await _context.Subjects.AnyAsync(s => s.Id == model.SubjectId && s.IsActive))
                return "Môn học không tồn tại hoặc đã ngừng sử dụng.";

            if (model.LocationId.HasValue && model.LocationId > 0)
            {
                if (!await _context.Locations.AnyAsync(l => l.Id == model.LocationId))
                    return "Địa điểm học không tồn tại.";
            }

            if (model.DayOfWeek < 1 || model.DayOfWeek > 6)
                return "Chỉ được tạo lịch từ Thứ 2 đến Thứ 7.";

            if (!TimeOnly.TryParse(model.StartTime, out var startTime) || !TimeOnly.TryParse(model.EndTime, out var endTime))
                return "Khung giờ không hợp lệ.";

            if (!DateOnly.TryParse(model.EffectiveFrom, out var effectiveFrom))
                return "Ngày hiệu lực bắt đầu không hợp lệ.";

            DateOnly? effectiveTo = null;
            if (!string.IsNullOrWhiteSpace(model.EffectiveTo))
            {
                if (!DateOnly.TryParse(model.EffectiveTo, out var parsedEffectiveTo))
                    return "Ngày hiệu lực kết thúc không hợp lệ.";
                effectiveTo = parsedEffectiveTo;
            }

            if (endTime <= startTime)
                return "Giờ kết thúc phải lớn hơn giờ bắt đầu.";

            if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
                return "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.";

            if (startTime < SchoolStart || endTime > SchoolEnd)
                return "Chỉ được xếp lịch trong khung 06:45 - 17:00.";

            var lockedSlot = LockedScheduleSlots.FirstOrDefault(slot => TimeRangesOverlap(slot.Start, slot.End, startTime, endTime));
            if (lockedSlot != null)
                return $"Không được xếp lịch trong khung {lockedSlot.Name} ({lockedSlot.Start:HH:mm} - {lockedSlot.End:HH:mm}).";

            if (startTime < LunchEnd && endTime > LunchStart)
                return "Thời khóa biểu không được chồng lên khung nghỉ trưa 11:00 - 14:00.";

            var matchingSlot = AllowedScheduleSlots.FirstOrDefault(slot => startTime >= slot.Start && endTime <= slot.End);
            if (matchingSlot == null)
                return "Chỉ được xếp lịch trong các khung 07:30-10:00, 10:00-11:00 hoặc 14:00-16:30.";

            var hasCoveredTeacher = await _context.Assignments.AnyAsync(a =>
                a.ClassId == model.ClassId &&
                a.IsActive &&
                a.StartDate <= effectiveFrom &&
                (effectiveTo == null
                    ? a.EndDate == null
                    : a.EndDate == null || a.EndDate >= effectiveTo));

            if (!hasCoveredTeacher)
                return "Lớp chưa có giáo viên phụ trách bao phủ toàn bộ thời gian hiệu lực của lịch.";

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
                return $"Lớp học đã có tiết '{subject?.Name}' trùng khung giờ này ({classOverlapSchedule.StartTime:HH:mm} - {classOverlapSchedule.EndTime:HH:mm}).";
            }

            if (model.LocationId.HasValue)
            {
                var locationOverlap = sameDaySchedules.Any(cs =>
                    cs.LocationId == model.LocationId
                    && DateRangesOverlap(cs.EffectiveFrom, cs.EffectiveTo, effectiveFrom, effectiveTo)
                    && TimeRangesOverlap(cs.StartTime, cs.EndTime, startTime, endTime));

                if (locationOverlap)
                    return "Phòng học/Địa điểm này đã được sử dụng cho lớp khác trong khung giờ này.";
            }

            return null;
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

        private sealed record ScheduleSlot(string Name, TimeOnly Start, TimeOnly End);
    }
}
