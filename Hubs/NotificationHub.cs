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
    }
}
