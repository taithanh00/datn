using datn.Models;

namespace datn.Services
{
    public static class WorkAttendanceCalculator
    {
        private static readonly TimeOnly DefaultCheckOutTime = new(17, 0, 0);
        private const decimal FullDayMinutes = 480m;

        public static bool EnsurePayrollValues(WorkAttendance attendance)
        {
            if (attendance.CheckInAtUtc == null)
                return false;

            var changed = false;
            if (attendance.CheckOutAtUtc == null)
            {
                attendance.CheckOutAtUtc = GetDefaultCheckOutUtc(attendance.Date);
                changed = true;
            }

            if (attendance.WorkedMinutes == null || attendance.WorkUnit == null || changed)
            {
                ApplyWorkedTime(attendance);
                changed = true;
            }

            return changed;
        }

        public static void ApplyWorkedTime(WorkAttendance attendance)
        {
            if (attendance.CheckInAtUtc == null || attendance.CheckOutAtUtc == null)
                return;

            var checkInVnt = ToVnt(attendance.CheckInAtUtc.Value);
            var checkOutVnt = ToVnt(attendance.CheckOutAtUtc.Value);
            var workedMinutes = (int)Math.Max(0, (checkOutVnt - checkInVnt).TotalMinutes);
            var calculatedWorkUnit = Math.Round((decimal)workedMinutes / FullDayMinutes, 2, MidpointRounding.AwayFromZero);

            attendance.WorkedMinutes = workedMinutes;
            attendance.WorkUnit = Math.Min(1.0m, calculatedWorkUnit);
        }

        public static DateTime GetDefaultCheckOutUtc(DateOnly workDate)
        {
            var localDateTime = workDate.ToDateTime(DefaultCheckOutTime);
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, ResolveVntTimeZone());
        }

        public static DateTimeOffset ToVnt(DateTime utc)
        {
            var normalizedUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTime(new DateTimeOffset(normalizedUtc), ResolveVntTimeZone());
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
    }
}
