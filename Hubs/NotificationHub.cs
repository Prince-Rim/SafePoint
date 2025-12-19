using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SafePoint_IRS.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendIncidentNotification(string title, string location, double latitude, double longitude, int incidentId, string status, string reporterId)
        {
            await Clients.All.SendAsync("ReceiveIncidentNotification", title, location, latitude, longitude, incidentId, status, reporterId);
        }

        public async Task SendBadgeNotification(string userId, string badgeName)
        {
            await Clients.All.SendAsync("ReceiveBadgeNotification", userId, badgeName);
        }

        public async Task SendResolutionNotification(string title, int incidentId, string reporterId)
        {
            await Clients.All.SendAsync("ReceiveResolutionNotification", title, incidentId, reporterId);
        }
    }
}
