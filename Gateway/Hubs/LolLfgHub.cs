using Gateway.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gateway.Hubs;

[AllowAnonymous]
public sealed class LolLfgHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.LolLfgGlobal);
        await base.OnConnectedAsync();
    }
}
