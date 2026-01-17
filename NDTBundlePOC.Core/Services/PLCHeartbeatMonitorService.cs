using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NDTBundlePOC.Core.Services
{
    /// <summary>
    /// Interface for notifying heartbeat updates to clients
    /// </summary>
    public interface IHeartbeatNotifier
    {
        Task NotifyHeartbeatUpdate(int heartbeatValue, string plcStatus, string plcIp);
    }

    /// <summary>
    /// Background service that monitors PLC heartbeat at regular intervals
    /// Reads DB1.DBW6 and interprets the value:
    /// - 1: PLC is online and responding
    /// - 127: Heartbeat trigger / awaiting PLC response
    /// - Other: Invalid or unexpected state
    /// </summary>
    public class PLCHeartbeatMonitorService : BackgroundService
    {
        private readonly IPLCService _plcService;
        private readonly ILogger<PLCHeartbeatMonitorService> _logger;
        private readonly int _pollingIntervalMs;
        private readonly IHeartbeatNotifier _notifier;
        private readonly string _plcIp;

        public PLCHeartbeatMonitorService(
            IPLCService plcService,
            ILogger<PLCHeartbeatMonitorService> logger,
            IHeartbeatNotifier notifier,
            string plcIp,
            int pollingIntervalMs = 750)
        {
            _plcService = plcService;
            _logger = logger;
            _notifier = notifier;
            _plcIp = plcIp;
            _pollingIntervalMs = pollingIntervalMs;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PLC Heartbeat Monitor Service started. Polling interval: {Interval}ms", _pollingIntervalMs);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_plcService.IsConnected)
                    {
                        int heartbeatValue = _plcService.ReadHeartbeat();
                        string plcStatus = GetStatusFromHeartbeat(heartbeatValue);

                        // Notify clients via notifier (SignalR)
                        await _notifier.NotifyHeartbeatUpdate(heartbeatValue, plcStatus, _plcIp);

                        _logger.LogDebug("PLC Heartbeat: Value={Value}, Status={Status}, IP={IP}", heartbeatValue, plcStatus, _plcIp);
                    }
                    else
                    {
                        // PLC not connected
                        await _notifier.NotifyHeartbeatUpdate(-1, "OFFLINE", _plcIp);
                        _logger.LogWarning("PLC is not connected. Heartbeat monitoring paused.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading PLC heartbeat");
                    await _notifier.NotifyHeartbeatUpdate(-1, "OFFLINE", _plcIp);
                }

                await Task.Delay(_pollingIntervalMs, stoppingToken);
            }

            _logger.LogInformation("PLC Heartbeat Monitor Service stopped.");
        }

        /// <summary>
        /// Interpret heartbeat value and return human-readable status
        /// Values 1-127 indicate PLC is ONLINE (continuous counter)
        /// Value -1 indicates PLC is OFFLINE (not connected)
        /// Any other value indicates OFFLINE (error/invalid)
        /// </summary>
        private string GetStatusFromHeartbeat(int heartbeatValue)
        {
            if (heartbeatValue >= 1 && heartbeatValue <= 127)
            {
                return "ONLINE"; // All values 1-127 indicate PLC is online and responding
            }
            else if (heartbeatValue == -1)
            {
                return "OFFLINE"; // Not connected
            }
            else
            {
                return "OFFLINE"; // Invalid or error state
            }
        }
    }
}

