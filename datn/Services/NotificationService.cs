using datn.Data;
using datn.Hubs;
using datn.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace datn.Services
{
    public interface INotificationService
    {
        Task SendToUserAsync(int accountId, string title, string message, string type = "info", string? url = null);
        Task SendToRoleAsync(string roleName, string title, string message, string type = "info", string? url = null);
        Task SendToAllAsync(string title, string message, string type = "info", string? url = null);
        Task<List<Notification>> GetUserNotificationsAsync(string username, int limit = 10);
        Task MarkAsReadAsync(int notificationId);
    }

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<RealtimeHub> _hubContext;

        public NotificationService(AppDbContext context, IHubContext<RealtimeHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task SendToUserAsync(int accountId, string title, string message, string type = "info", string? url = null)
        {
            var notification = new Notification
            {
                RecipientId = accountId,
                Title = title,
                Message = message,
                Type = type,
                Url = url,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Gửi qua SignalR tới User cụ thể (nếu có cách track ConnectionId theo UserId)
            // Tạm thời gửi qua Hub theo Username nếu cần, hoặc gửi chung và client tự lọc.
            // Ở đây ta dùng Group riêng cho từng User (cần update Hub) hoặc gửi cho tất cả và lọc ở Client.
            // Đơn giản nhất: Gửi tới Group tương ứng với Role của User đó.
            var account = await _context.Accounts.Include(a => a.Role).FirstOrDefaultAsync(a => a.Id == accountId);
            if (account != null)
            {
                await _hubContext.Clients.User(account.Username).SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    title,
                    message,
                    type,
                    url,
                    createdAt = notification.CreatedAt
                });
            }
        }

        public async Task SendToRoleAsync(string roleName, string title, string message, string type = "info", string? url = null)
        {
            var notification = new Notification
            {
                RecipientRole = roleName,
                Title = title,
                Message = message,
                Type = type,
                Url = url,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Gửi tới Group của Role trong SignalR
            string groupName = roleName == "Manager" ? "Managers" : (roleName == "Employee" ? "Employees" : "Parents");
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                title,
                message,
                type,
                url,
                createdAt = notification.CreatedAt
            });
        }

        public async Task SendToAllAsync(string title, string message, string type = "info", string? url = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                Url = url,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                title,
                message,
                type,
                url,
                createdAt = notification.CreatedAt
            });
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string username, int limit = 10)
        {
            var account = await _context.Accounts.Include(a => a.Role).FirstOrDefaultAsync(a => a.Username == username);
            if (account == null) return new List<Notification>();

            return await _context.Notifications
                .Where(n => (n.RecipientId == account.Id) 
                         || (n.RecipientRole == account.Role.Name)
                         || (n.RecipientId == null && n.RecipientRole == null))
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
