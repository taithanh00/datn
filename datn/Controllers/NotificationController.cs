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
            await _notificationService.MarkAsReadAsync(id);
            return Json(new { success = true });
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
            return (text.Contains("ngh\u1ec9 h\u1ecdc", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("ngh\u00e1\u00bb\u2030 h\u00c3\u00a1\u00bb\u008dc", StringComparison.OrdinalIgnoreCase))
                && (notification.Url == null
                    || notification.Url.StartsWith("/Parent/Children", StringComparison.OrdinalIgnoreCase)
                    || notification.Url.StartsWith("/Parent/ClassClosure", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsHolidayNotification(Notification notification)
        {
            var text = $"{notification.Title} {notification.Message}";
            return text.Contains("ngh\u1ec9 l\u1ec5", StringComparison.OrdinalIgnoreCase)
                || text.Contains("ng\u00e0y l\u1ec5", StringComparison.OrdinalIgnoreCase)
                || text.Contains("ngh\u00e1\u00bb\u2030 l\u00c3\u00a1\u00bb\u00a6", StringComparison.OrdinalIgnoreCase)
                || text.Contains("ng\u00c3\u00a0y l\u00c3\u00a1\u00bb\u00a6", StringComparison.OrdinalIgnoreCase);
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

            var sanitizedReason = "Gi\u00e1o vi\u00ean ph\u1ee5 tr\u00e1ch c\u00f3 vi\u1ec7c \u0111\u1ed9t xu\u1ea5t";
            var normalized = text.ToLowerInvariant();
            if (!normalized.Contains("kh\u00f4ng ph\u00e9p") && !normalized.Contains("khong phep") && !normalized.Contains("check-in"))
                return text;

            var marker = "L\u00fd do:";
            var markerIndex = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
                markerIndex = text.IndexOf("LÃ½ do:", StringComparison.OrdinalIgnoreCase);

            if (markerIndex < 0)
                return sanitizedReason;

            return text[..(markerIndex + marker.Length)].TrimEnd() + " " + sanitizedReason + ".";
        }
    }
}
