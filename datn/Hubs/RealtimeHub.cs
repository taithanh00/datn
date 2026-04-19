using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace datn.Hubs
{
    [Authorize]
    public class RealtimeHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "Manager")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Managers");
            }

            if (role == "Employee")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Employees");
            }

            await base.OnConnectedAsync();
        }
    }
}
