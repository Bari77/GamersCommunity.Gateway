using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gateway.Hubs;

[Authorize]
public sealed class MessengerHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var subject = Context.User?.FindFirstValue("sub")
            ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(subject))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, Realtime.RealtimeGroups.User(subject));
        }

        await base.OnConnectedAsync();
    }
}
