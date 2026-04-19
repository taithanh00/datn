using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace datn.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("Api/Latest")]
        public async Task<IActionResult> GetLatest()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var notifications = await _notificationService.GetUserNotificationsAsync(username);
            return Json(new { success = true, data = notifications });
        }

        [HttpPost("Api/MarkRead/{id}")]
        public async Task<IActionResult> MarkRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Json(new { success = true });
        }
    }
}
