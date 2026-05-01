using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace datn.Hubs
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // Trả về AccountId từ Claim để SignalR gửi đúng người dùng
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
