using Microsoft.AspNetCore.SignalR;

namespace ElectricVehicleDealerManagermentSystem.SignalR
{
    public class SignalRHub : Hub
    {
        public async Task BroadcastUpdateAsync(string action, object? data = null)
        {
            await Clients.All.SendAsync("ReceiveMessage", action, data);
        }
    }
}
