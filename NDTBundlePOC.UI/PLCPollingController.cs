using System;
using System.Windows.Forms;
using NDTBundlePOC.Core.Services;

namespace NDTBundlePOC.UI
{
    /// <summary>
    /// Simple PLC Polling Controller for WinForms
    /// Uses a Timer to poll the PLC at regular intervals
    /// </summary>
    public class PLCPollingController
    {
        private readonly IPLCService _plcService;
        private readonly INDTBundleService _ndtBundleService;
        private readonly IOKBundleService _okBundleService;
        private readonly int _millId;
        private readonly int _pollingIntervalMs;
        
        private System.Windows.Forms.Timer _pollingTimer;
        private int _previousOKCuts = 0;
        private int _previousNDTCuts = 0;
        private bool _isPolling = false;

        public bool IsPolling => _isPolling;
        public int MillId => _millId;

        public event EventHandler<string> StatusChanged;
        public event EventHandler<int> OKCutsChanged;
        public event EventHandler<int> NDTCutsChanged;

        public PLCPollingController(
            IPLCService plcService,
            INDTBundleService ndtBundleService,
            IOKBundleService okBundleService,
            int millId = 1,
            int pollingIntervalMs = 1000)
        {
            _plcService = plcService;
            _ndtBundleService = ndtBundleService;
            _okBundleService = okBundleService;
            _millId = millId;
            _pollingIntervalMs = pollingIntervalMs;

            _pollingTimer = new System.Windows.Forms.Timer();
            _pollingTimer.Interval = _pollingIntervalMs;
            _pollingTimer.Tick += PollingTimer_Tick;
            // Explicitly ensure timer is not started - polling is off by default
            _pollingTimer.Enabled = false;
        }

        public void Start()
        {
            if (_isPolling)
                return;

            if (!_plcService.IsConnected)
            {
                StatusChanged?.Invoke(this, "PLC not connected. Cannot start polling.");
                return;
            }

            _isPolling = true;
            _pollingTimer.Start();
            
            // Initialize previous values to current to avoid processing all existing cuts
            _previousOKCuts = ReadOKCuts();
            _previousNDTCuts = _plcService.ReadNDTCuts(_millId);
            
            StatusChanged?.Invoke(this, "PLC polling started");
        }

        public void Stop()
        {
            if (!_isPolling)
                return;

            _isPolling = false;
            _pollingTimer.Stop();
            _pollingTimer.Enabled = false; // Explicitly disable timer
            StatusChanged?.Invoke(this, "PLC polling stopped");
        }

        private void PollingTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (!_plcService.IsConnected)
                {
                    StatusChanged?.Invoke(this, "PLC disconnected. Polling paused.");
                    Stop();
                    return;
                }

                // Read OK cuts from PLC
                int currentOKCuts = ReadOKCuts();
                
                // Read NDT cuts from PLC
                int currentNDTCuts = _plcService.ReadNDTCuts(_millId);

                // Process new OK cuts (only when there's an increase)
                if (currentOKCuts > _previousOKCuts)
                {
                    int newOKCuts = currentOKCuts - _previousOKCuts;
                    _previousOKCuts = currentOKCuts;
                    
                    OKCutsChanged?.Invoke(this, newOKCuts);
                    _okBundleService.ProcessOKCuts(_millId, newOKCuts);
                }
                else if (_previousOKCuts == 0 && currentOKCuts > 0)
                {
                    // Initialize previous value to current to avoid processing all existing cuts
                    _previousOKCuts = currentOKCuts;
                }

                // Process new NDT cuts (only when there's an increase)
                if (currentNDTCuts > _previousNDTCuts)
                {
                    int newNDTCuts = currentNDTCuts - _previousNDTCuts;
                    _previousNDTCuts = currentNDTCuts;
                    
                    NDTCutsChanged?.Invoke(this, newNDTCuts);
                    _ndtBundleService.ProcessNDTCuts(_millId, newNDTCuts);
                }
                else if (_previousNDTCuts == 0 && currentNDTCuts > 0)
                {
                    // Initialize previous value to current to avoid processing all existing cuts
                    _previousNDTCuts = currentNDTCuts;
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Error in PLC polling: {ex.Message}");
            }
        }

        private int ReadOKCuts()
        {
            try
            {
                if (_plcService is RealS7PLCService realPlc)
                {
                    return realPlc.ReadOKCuts(_millId);
                }
                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public void Dispose()
        {
            Stop();
            _pollingTimer?.Dispose();
        }
    }
}

