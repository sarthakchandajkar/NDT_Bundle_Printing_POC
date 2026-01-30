using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace NDTBundlePOC.Core.Services
{
    /// <summary>
    /// Controllable PLC Polling Service - Allows starting/stopping polling dynamically
    /// This service manages the polling loop and can be controlled via API
    /// </summary>
    public interface IControllablePLCPollingService
    {
        bool IsPolling { get; }
        bool Start();
        bool Stop();
        Task<bool> StartAsync();
        Task<bool> StopAsync();
    }

    public class ControllablePLCPollingService : IControllablePLCPollingService
    {
        private readonly IPLCService _plcService;
        private readonly INDTBundleService _ndtBundleService;
        private readonly IOKBundleService _okBundleService;
        private readonly IPrinterService _printerService;
        private readonly ExcelExportService _excelService;
        private readonly ILogger<ControllablePLCPollingService> _logger;
        private readonly IPipeCountingActivityService _activityService;
        private readonly int _millId;
        private readonly int _pollingIntervalMs;
        
        private CancellationTokenSource _cancellationTokenSource;
        private Task _pollingTask;
        private bool _isPolling = false;
        private readonly object _lockObject = new object();
        
        // Track previous values to detect changes
        private int _previousOKCuts = 0;
        private int _previousNDTCuts = 0;
        private bool _previousOKBundleDone = false;
        private bool _previousNDTBundleDone = false;
        private bool _isInitialized = false;

        public bool IsPolling 
        { 
            get 
            { 
                lock (_lockObject)
                {
                    return _isPolling && _pollingTask != null && !_pollingTask.IsCompleted;
                }
            } 
        }

        public ControllablePLCPollingService(
            IPLCService plcService,
            INDTBundleService ndtBundleService,
            IOKBundleService okBundleService,
            IPrinterService printerService,
            ExcelExportService excelService,
            ILogger<ControllablePLCPollingService> logger,
            IPipeCountingActivityService activityService,
            int millId = 1,
            int pollingIntervalMs = 1000)
        {
            _plcService = plcService;
            _ndtBundleService = ndtBundleService;
            _okBundleService = okBundleService;
            _printerService = printerService;
            _excelService = excelService;
            _logger = logger;
            _activityService = activityService;
            _millId = millId;
            _pollingIntervalMs = pollingIntervalMs;
        }

        public bool Start()
        {
            return StartAsync().GetAwaiter().GetResult();
        }

        public bool Stop()
        {
            return StopAsync().GetAwaiter().GetResult();
        }

        public async Task<bool> StartAsync()
        {
            lock (_lockObject)
            {
                if (_isPolling)
                {
                    _logger?.LogWarning("PLC Polling is already running");
                    return true;
                }

                if (!_plcService.IsConnected)
                {
                    _logger?.LogWarning("Cannot start PLC polling: PLC is not connected");
                    return false;
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _isPolling = true;
                _isInitialized = false; // Reset initialization flag when starting
            }

            try
            {
                // Start the polling loop in a background task
                _pollingTask = Task.Run(async () => await PollingLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

                _logger?.LogInformation("PLC Polling Service started successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start PLC Polling Service");
                lock (_lockObject)
                {
                    _isPolling = false;
                    _pollingTask = null;
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                }
                return false;
            }
        }

        public async Task<bool> StopAsync()
        {
            lock (_lockObject)
            {
                if (!_isPolling)
                {
                    _logger?.LogWarning("PLC Polling is not running");
                    return true;
                }

                _isPolling = false;
            }

            try
            {
                // Cancel the polling loop
                _cancellationTokenSource?.Cancel();
                
                // Wait for the task to complete (with timeout)
                if (_pollingTask != null)
                {
                    await Task.WhenAny(_pollingTask, Task.Delay(5000)); // 5 second timeout
                    
                    if (!_pollingTask.IsCompleted)
                    {
                        _logger?.LogWarning("PLC Polling Service did not stop within timeout");
                    }
                }

                // Cleanup
                _cancellationTokenSource?.Dispose();
                
                lock (_lockObject)
                {
                    _pollingTask = null;
                    _cancellationTokenSource = null;
                }

                _logger?.LogInformation("PLC Polling Service stopped successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping PLC Polling Service");
                return false;
            }
        }

        private async Task PollingLoop(CancellationToken cancellationToken)
        {
            _logger?.LogInformation("PLC Polling loop started");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_plcService.IsConnected)
                    {
                        // Read OK cuts from PLC
                        int currentOKCuts = ReadOKCuts();
                        
                        // Read NDT cuts from PLC
                        int currentNDTCuts = _plcService.ReadNDTCuts(_millId);
                        
                        // Update activity service
                        if (_activityService != null)
                        {
                            if (currentOKCuts > _previousOKCuts)
                            {
                                int newOKCuts = currentOKCuts - _previousOKCuts;
                                _activityService.LogActivity("OK", newOKCuts, currentOKCuts, currentNDTCuts, "PLC");
                            }
                            else if (_previousOKCuts == 0 && currentOKCuts > 0)
                            {
                                _activityService.LogActivity("OK", 0, currentOKCuts, currentNDTCuts, "PLC");
                            }
                            else if (currentOKCuts != _previousOKCuts)
                            {
                                _activityService.LogActivity("OK", 0, currentOKCuts, currentNDTCuts, "PLC");
                            }
                            
                            if (currentNDTCuts > _previousNDTCuts)
                            {
                                _activityService.LogActivity("NDT", currentNDTCuts - _previousNDTCuts, currentOKCuts, currentNDTCuts, "PLC");
                            }
                            else if (_previousNDTCuts == 0 && currentNDTCuts > 0)
                            {
                                _activityService.LogActivity("NDT", 0, currentOKCuts, currentNDTCuts, "PLC");
                            }
                            else if (currentNDTCuts != _previousNDTCuts && currentOKCuts == _previousOKCuts)
                            {
                                _activityService.LogActivity("NDT", 0, currentOKCuts, currentNDTCuts, "PLC");
                            }
                        }
                        
                        // Process new OK cuts
                        if (currentOKCuts > _previousOKCuts)
                        {
                            int newOKCuts = currentOKCuts - _previousOKCuts;
                            _previousOKCuts = currentOKCuts;
                            
                            _logger?.LogInformation($"Detected {newOKCuts} new OK cuts");
                            _okBundleService.ProcessOKCuts(_millId, newOKCuts);
                        }
                        else if (_previousOKCuts == 0 && currentOKCuts > 0)
                        {
                            _previousOKCuts = currentOKCuts;
                            _logger?.LogInformation($"Initialized OK cuts tracking: {currentOKCuts} (service started)");
                        }

                        // Process new NDT cuts
                        if (currentNDTCuts > _previousNDTCuts)
                        {
                            int newNDTCuts = currentNDTCuts - _previousNDTCuts;
                            _previousNDTCuts = currentNDTCuts;
                            
                            _logger?.LogInformation($"Detected {newNDTCuts} new NDT cuts");
                            _ndtBundleService.ProcessNDTCuts(_millId, newNDTCuts);
                        }
                        else if (_previousNDTCuts == 0 && currentNDTCuts > 0)
                        {
                            _previousNDTCuts = currentNDTCuts;
                            _logger?.LogInformation($"Initialized NDT cuts tracking: {currentNDTCuts} (service started)");
                        }

                        // Check for completed bundles and print (skip first cycle)
                        if (_isInitialized)
                        {
                            CheckAndPrintOKBundles();
                            CheckAndPrintNDTBundles();
                        }
                        else
                        {
                            _isInitialized = true;
                            _logger?.LogInformation("Service initialization complete. Skipped first cycle bundle printing to avoid stale bundles.");
                        }

                        // Check OK Bundle Done signal
                        bool okBundleDone = CheckOKBundleDone();
                        if (okBundleDone && !_previousOKBundleDone)
                        {
                            _logger?.LogInformation("OK Bundle Done signal received from PLC");
                            ProcessOKBundlePrint();
                            _previousOKBundleDone = true;
                        }
                        else if (!okBundleDone && _previousOKBundleDone)
                        {
                            _previousOKBundleDone = false;
                        }

                        // Check NDT Bundle Done signal
                        bool ndtBundleDone = CheckNDTBundleDone();
                        if (ndtBundleDone && !_previousNDTBundleDone)
                        {
                            _logger?.LogInformation("NDT Bundle Done signal received from PLC");
                            ProcessNDTBundlePrint();
                            _previousNDTBundleDone = true;
                        }
                        else if (!ndtBundleDone && _previousNDTBundleDone)
                        {
                            _previousNDTBundleDone = false;
                        }
                    }
                    else
                    {
                        _logger?.LogWarning("PLC not connected. Waiting for connection...");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in PLC polling loop");
                }

                await Task.Delay(_pollingIntervalMs, cancellationToken);
            }

            _logger?.LogInformation("PLC Polling loop stopped");
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
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reading OK cuts from PLC");
                return 0;
            }
        }

        private bool CheckOKBundleDone()
        {
            try
            {
                if (_plcService is RealS7PLCService realPlc)
                {
                    return realPlc.ReadOKBundleDone(_millId);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking OK Bundle Done signal");
                return false;
            }
        }

        private bool CheckNDTBundleDone()
        {
            try
            {
                if (_plcService is RealS7PLCService realPlc)
                {
                    return realPlc.ReadNDTBundleDone(_millId);
                }
                return false;
            }
            catch (Exception ex)
            {
                string errorMsg = ex.Message?.ToLower() ?? "";
                if (errorMsg.Contains("object does not exist") || 
                    errorMsg.Contains("does not exist") ||
                    errorMsg.Contains("not found") ||
                    errorMsg.Contains("out of range") ||
                    errorMsg.Contains("address out of range"))
                {
                    return false;
                }
                _logger?.LogError(ex, "Error checking NDT Bundle Done signal");
                return false;
            }
        }

        private void CheckAndPrintOKBundles()
        {
            try
            {
                var readyBundles = _okBundleService.GetBundlesReadyForPrinting();
                if (readyBundles.Count > 0)
                {
                    _logger?.LogInformation($"Found {readyBundles.Count} OK bundle(s) with Status=2 (Completed)");
                }
                
                foreach (var bundle in readyBundles)
                {
                    bool pipeDone = CheckOKBundleDone();
                    bool atPackingStation = _plcService != null && _plcService.IsBundleAtPackingStation(_millId);
                    
                    if (pipeDone && atPackingStation)
                    {
                        _logger?.LogInformation($"Printing OK bundle: {bundle.Bundle_No} (ID: {bundle.OKBundle_ID}, Pcs: {bundle.OK_Pcs}, Status: {bundle.Status})");
                        _logger?.LogInformation($"  Conditions met: L1L2_PipeDone={pipeDone}, AtPackingStation={atPackingStation}");
                        
                        bool printed = _okBundleService.PrintBundleTag(
                            bundle.OKBundle_ID,
                            _printerService,
                            _excelService,
                            _plcService
                        );
                        
                        if (printed)
                        {
                            _logger?.LogInformation($"✓ Successfully printed OK bundle: {bundle.Bundle_No}");
                        }
                        else
                        {
                            _logger?.LogWarning($"✗ Failed to print OK bundle: {bundle.Bundle_No}");
                        }
                    }
                    else
                    {
                        _logger?.LogDebug($"OK bundle {bundle.Bundle_No} waiting for printing conditions: L1L2_PipeDone={pipeDone}, AtPackingStation={atPackingStation}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error printing OK bundles");
            }
        }

        private void CheckAndPrintNDTBundles()
        {
            try
            {
                var readyBundles = _ndtBundleService.GetBundlesReadyForPrinting();
                if (readyBundles == null)
                {
                    _logger?.LogWarning("GetBundlesReadyForPrinting returned null - database may be unavailable");
                    return;
                }
                
                if (readyBundles.Count > 0)
                {
                    _logger?.LogInformation($"Found {readyBundles.Count} NDT bundle(s) ready for printing");
                }
                
                foreach (var bundle in readyBundles)
                {
                    try
                    {
                        _logger?.LogInformation($"Printing NDT bundle: {bundle.Bundle_No} (ID: {bundle.NDTBundle_ID}, Pcs: {bundle.NDT_Pcs}, Status: {bundle.Status})");
                        bool printed = _ndtBundleService.PrintBundleTag(
                            bundle.NDTBundle_ID,
                            _printerService,
                            _excelService,
                            _plcService
                        );
                        
                        if (printed)
                        {
                            _logger?.LogInformation($"✓ Successfully printed NDT bundle: {bundle.Bundle_No}");
                        }
                        else
                        {
                            _logger?.LogWarning($"✗ Failed to print NDT bundle: {bundle.Bundle_No}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"Error printing individual NDT bundle {bundle?.Bundle_No ?? "Unknown"}: {ex.Message}");
                        // Continue with next bundle instead of stopping
                    }
                }
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                _logger?.LogWarning($"Database connection error while checking NDT bundles: {ex.Message}. This is expected if database is unavailable.");
            }
            catch (Npgsql.NpgsqlException ex)
            {
                _logger?.LogWarning($"Database error while checking NDT bundles: {ex.Message}. This is expected if database is unavailable.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Unexpected error in CheckAndPrintNDTBundles: {ex.Message}");
            }
        }

        private void ProcessOKBundlePrint()
        {
            try
            {
                var readyBundles = _okBundleService.GetBundlesReadyForPrinting();
                if (readyBundles.Count > 0)
                {
                    var bundle = readyBundles.OrderByDescending(b => b.BundleEndTime).First();
                    
                    bool atPackingStation = _plcService != null && _plcService.IsBundleAtPackingStation(_millId);
                    
                    if (atPackingStation)
                    {
                        _logger?.LogInformation($"Processing OK bundle print: {bundle.Bundle_No} (Status: {bundle.Status}, AtPackingStation: {atPackingStation})");
                        
                        _okBundleService.PrintBundleTag(
                            bundle.OKBundle_ID,
                            _printerService,
                            _excelService,
                            _plcService
                        );

                        if (_plcService is RealS7PLCService realPlc)
                        {
                            realPlc.WriteAcknowledgment("DB260.DBX3.4", true);
                        }
                    }
                    else
                    {
                        _logger?.LogDebug($"OK bundle {bundle.Bundle_No} waiting for packing station (L1L2_PipeDone received but not at packing station)");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing OK bundle print");
            }
        }

        private void ProcessNDTBundlePrint()
        {
            try
            {
                var readyBundles = _ndtBundleService.GetBundlesReadyForPrinting();
                if (readyBundles.Count > 0)
                {
                    var bundle = readyBundles.OrderByDescending(b => b.BundleEndTime).First();
                    _logger?.LogInformation($"Processing NDT bundle print: {bundle.Bundle_No}");
                    
                    _ndtBundleService.PrintBundleTag(
                        bundle.NDTBundle_ID,
                        _printerService,
                        _excelService,
                        _plcService
                    );

                    if (_plcService is RealS7PLCService realPlc)
                    {
                        realPlc.WriteAcknowledgment("DB260.DBX6.0", true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing NDT bundle print");
            }
        }
    }
}


