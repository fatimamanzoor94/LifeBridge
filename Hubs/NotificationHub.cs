using Microsoft.AspNetCore.SignalR;

namespace Khoon_e_Hayat.Hubs
{
    public class NotificationHub : Hub
    {
        // Called when a new blood request is created
        public async Task BroadcastNewRequest(int requestId, string bloodGroup, string city)
        {
            await Clients.All.SendAsync("ReceiveNewRequest", requestId, bloodGroup, city);
        }

        // Called when a donor's availability changes
        public async Task BroadcastDonorStatusUpdate(int donorId, bool isAvailable)
        {
            await Clients.All.SendAsync("ReceiveDonorStatusUpdate", donorId, isAvailable);
        }

        // Called when a match is successfully made
        public async Task BroadcastMatchCompleted(int requestId, int donorId)
        {
            await Clients.All.SendAsync("ReceiveMatchCompleted", requestId, donorId);
        }

        // General notification for UI toasts
        public async Task BroadcastSystemNotification(string message, string type)
        {
            await Clients.All.SendAsync("ReceiveSystemNotification", message, type);
        }
    }
}