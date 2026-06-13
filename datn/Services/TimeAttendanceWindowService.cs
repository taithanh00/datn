namespace datn.Services
{
    public interface ITimeAttendanceWindowService
    {
        DateTimeOffset GetVntNow();
        DateTimeOffset ToVnt(DateTime utc);
        TimeAttendanceWindowState GetWindowState(DateTimeOffset vntNow);
    }

    public sealed record TimeAttendanceWindowState(bool IsAllowed, string Message);

    public class TimeAttendanceWindowService : ITimeAttendanceWindowService
    {
        private static readonly TimeSpan WorkStart = new(6, 30, 0);
        private static readonly TimeSpan WorkEnd = new(17, 0, 0);
        private const string AllowedMessage = "Đang trong khung giờ chấm công.";
        private const string BlockedMessage = "Chỉ được chấm công từ Thứ 2 đến Thứ 7, 06:30 - 17:00 (VNT).";

        public DateTimeOffset GetVntNow()
        {
            var utcNow = DateTimeOffset.UtcNow;
            return TimeZoneInfo.ConvertTime(utcNow, ResolveVntTimeZone());
        }

        public DateTimeOffset ToVnt(DateTime utc)
        {
            var normalizedUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTime(new DateTimeOffset(normalizedUtc), ResolveVntTimeZone());
        }

        public TimeAttendanceWindowState GetWindowState(DateTimeOffset vntNow)
        {
            var day = vntNow.DayOfWeek;
            var isWorkingDay = day is >= DayOfWeek.Monday and <= DayOfWeek.Saturday;
            var time = vntNow.TimeOfDay;
            var isAllowed = isWorkingDay && time >= WorkStart && time <= WorkEnd;

            return new TimeAttendanceWindowState(isAllowed, isAllowed ? AllowedMessage : BlockedMessage);
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
