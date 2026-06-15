using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace datn.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class NotificationController : BaseController
    {
        private readonly INotificationService _notificationService;

        public NotificationController(AppDbContext context, INotificationService notificationService) : base(context)
        {
            _notificationService = notificationService;
        }

        [HttpGet("Api/Latest")]
        public async Task<IActionResult> GetLatest()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var account = await _context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Username == username);
            if (account == null) return Unauthorized();

            var notifications = await _notificationService.GetUserNotificationsAsync(username);
            var data = new List<object>();
            foreach (var notification in notifications)
            {
                var resolvedUrl = await ResolveNotificationUrlAsync(notification, account.Role.Name);
                var resolvedMessage = ResolveNotificationMessage(notification, account.Role.Name);
                data.Add(new
                {
                    notification.Id,
                    notification.Title,
                    Message = resolvedMessage,
                    Url = resolvedUrl,
                    notification.Type,
                    notification.CreatedAt,
                    notification.IsRead
                });
            }

            return Json(new { success = true, data });
        }

        [HttpPost("Api/MarkRead/{id}")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var account = await GetCurrentAccountAsync();
            if (account == null) return Unauthorized();

            var notification = await GetVisibleNotificationsQuery(account)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification == null)
                return NotFound(new { success = false, message = "Không tìm thấy thông báo." });

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost("Api/MarkAllRead")]
        public async Task<IActionResult> MarkAllRead()
        {
            var account = await GetCurrentAccountAsync();
            if (account == null) return Unauthorized();

            var notifications = await GetVisibleNotificationsQuery(account)
                .Where(n => !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        private async Task<Account?> GetCurrentAccountAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;

            return await _context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Username == username);
        }

        private IQueryable<Notification> GetVisibleNotificationsQuery(Account account)
        {
            return _context.Notifications
                .Where(n => n.RecipientId == account.Id
                    || n.RecipientRole == account.Role.Name
                    || (n.RecipientId == null && n.RecipientRole == null));
        }

        private async Task<string?> ResolveNotificationUrlAsync(Notification notification, string roleName)
        {
            var url = notification.Url;
            var title = notification.Title ?? string.Empty;
            var message = notification.Message ?? string.Empty;

            if (roleName == "Parent" && IsClassClosureNotification(notification))
            {
                var classId = await FindClassIdFromTextAsync(title + " " + message);
                var date = ParseFirstDate(title + " " + message);
                if (classId.HasValue && date.HasValue)
                    return $"/Parent/ClassClosure?classId={classId.Value}&date={date.Value:yyyy-MM-dd}";
            }

            if (IsHolidayNotification(notification))
            {
                var holidayId = await FindHolidayIdFromTextAsync(title + " " + message);
                if (holidayId.HasValue)
                {
                    if (roleName == "Parent") return $"/Parent/HolidayDetail/{holidayId.Value}";
                    if (roleName == "Employee") return $"/Employee/HolidayDetail/{holidayId.Value}";
                    if (roleName == "Manager") return "/HolidayManagement";
                }
            }

            return url;
        }

        private static string? ResolveNotificationMessage(Notification notification, string roleName)
        {
            if (roleName != "Parent" || !IsClassClosureNotification(notification))
                return notification.Message;

            var text = notification.Message ?? string.Empty;
            return SanitizeParentFacingClassClosureText(text);
        }

        private static bool IsClassClosureNotification(Notification notification)
        {
            var text = $"{notification.Title} {notification.Message}";
            return (text.Contains("nghỉ học", StringComparison.OrdinalIgnoreCase))
                && (notification.Url == null
                    || notification.Url.StartsWith("/Parent/Children", StringComparison.OrdinalIgnoreCase)
                    || notification.Url.StartsWith("/Parent/ClassClosure", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsHolidayNotification(Notification notification)
        {
            var text = $"{notification.Title} {notification.Message}";
            return text.Contains("nghỉ lễ", StringComparison.OrdinalIgnoreCase)
                || text.Contains("ngày lễ", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<int?> FindClassIdFromTextAsync(string text)
        {
            var classes = await _context.Classes.IgnoreQueryFilters()
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            return classes
                .Where(c => !string.IsNullOrWhiteSpace(c.Name) && text.Contains(c.Name, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.Name.Length)
                .Select(c => (int?)c.Id)
                .FirstOrDefault();
        }

        private async Task<int?> FindHolidayIdFromTextAsync(string text)
        {
            var date = ParseFirstDate(text);
            if (date.HasValue)
            {
                var byDate = await _context.Holidays.IgnoreQueryFilters()
                    .Where(h => h.Date == date.Value)
                    .Select(h => (int?)h.Id)
                    .FirstOrDefaultAsync();
                if (byDate.HasValue) return byDate;
            }

            var holidays = await _context.Holidays.IgnoreQueryFilters()
                .Select(h => new { h.Id, h.Name })
                .ToListAsync();

            return holidays
                .Where(h => !string.IsNullOrWhiteSpace(h.Name) && text.Contains(h.Name, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(h => h.Name.Length)
                .Select(h => (int?)h.Id)
                .FirstOrDefault();
        }

        private static DateOnly? ParseFirstDate(string text)
        {
            var match = Regex.Match(text, @"\b(?<day>\d{1,2})/(?<month>\d{1,2})/(?<year>\d{4})\b");
            if (!match.Success) return null;

            if (int.TryParse(match.Groups["day"].Value, out var day)
                && int.TryParse(match.Groups["month"].Value, out var month)
                && int.TryParse(match.Groups["year"].Value, out var year))
            {
                try { return new DateOnly(year, month, day); }
                catch { return null; }
            }

            return null;
        }

        private static string SanitizeParentFacingClassClosureText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var sanitizedReason = "Giáo viên phụ trách có việc đột xuất";
            var normalized = text.ToLowerInvariant();
            if (!normalized.Contains("không phép") && !normalized.Contains("khong phep") && !normalized.Contains("check-in"))
                return text;

            var marker = "Lý do:";
            var markerIndex = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

            if (markerIndex < 0)
                return sanitizedReason;

            return text[..(markerIndex + marker.Length)].TrimEnd() + " " + sanitizedReason + ".";
        }
    }
}
