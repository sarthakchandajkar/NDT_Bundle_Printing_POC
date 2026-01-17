using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NDTBundlePOC.Core.Services;
using NDTBundlePOC.UI.Web.Hubs;

namespace NDTBundlePOC.UI.Web.Services
{
    /// <summary>
    /// SignalR implementation of IHeartbeatNotifier
    /// </summary>
    public class HeartbeatNotifier : IHeartbeatNotifier
    {
        private readonly IHubContext<HeartbeatHub> _hubContext;

        public HeartbeatNotifier(IHubContext<HeartbeatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyHeartbeatUpdate(int heartbeatValue, string plcStatus, string plcIp)
        {
            await _hubContext.Clients.All.SendAsync("HeartbeatUpdate", new
            {
                heartbeatValue = heartbeatValue,
                plcStatus = plcStatus,
                plcIp = plcIp,
                lastUpdateTime = DateTime.UtcNow
            });
        }
    }
}

