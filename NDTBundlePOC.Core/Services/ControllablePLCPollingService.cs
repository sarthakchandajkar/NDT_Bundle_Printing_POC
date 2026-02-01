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
        private readonly IDataRepository _repository;
        private readonly int _millId;
        private readonly int _pollingIntervalMs;
        private readonly bool _bypassOKBundlePLCConditions; // For testing: bypass PLC signal requirements
        
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
        
        // Track printed tags for summary
        private int _totalOKTagsPrinted = 0;
        private int _totalNDTTagsPrinted = 0;

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
            IDataRepository repository,
            int millId = 1,
            int pollingIntervalMs = 1000,
            bool bypassOKBundlePLCConditions = true) // Default to true for testing
        {
            _plcService = plcService;
            _ndtBundleService = ndtBundleService;
            _okBundleService = okBundleService;
            _printerService = printerService;
            _excelService = excelService;
            _logger = logger;
            _activityService = activityService;
            _repository = repository;
            _millId = millId;
            _pollingIntervalMs = pollingIntervalMs;
            _bypassOKBundlePLCConditions = bypassOKBundlePLCConditions;
            
            if (_bypassOKBundlePLCConditions)
            {
                _logger?.LogWarning("⚠ TEST MODE: OK bundle PLC conditions are BYPASSED. Bundles will print automatically when Status=2.");
                _logger?.LogWarning("  → Set bypassOKBundlePLCConditions=false for production (requires L1L2_PipeDone signal and packing station)");
            }
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
                            
                            _logger?.LogInformation($"📊 OK Pipes: +{newOKCuts} new (Total: {currentOKCuts})");
                            _okBundleService.ProcessOKCuts(_millId, newOKCuts);
                        }
                        else if (_previousOKCuts == 0 && currentOKCuts > 0)
                        {
                            _previousOKCuts = currentOKCuts;
                            _logger?.LogInformation($"📊 OK Pipes initialized: {currentOKCuts} total");
                        }

                        // Process new NDT cuts
                        if (currentNDTCuts > _previousNDTCuts)
                        {
                            int newNDTCuts = currentNDTCuts - _previousNDTCuts;
                            _previousNDTCuts = currentNDTCuts;
                            
                            _logger?.LogInformation($"📊 NDT Pipes: +{newNDTCuts} new (Total: {currentNDTCuts})");
                            _ndtBundleService.ProcessNDTCuts(_millId, newNDTCuts);
                        }
                        else if (_previousNDTCuts == 0 && currentNDTCuts > 0)
                        {
                            _previousNDTCuts = currentNDTCuts;
                            _logger?.LogInformation($"📊 NDT Pipes initialized: {currentNDTCuts} total");
                        }

                        // Check for completed bundles and print (skip first cycle)
                        if (_isInitialized)
                        {
                            CheckAndPrintOKBundles();
                            CheckAndPrintNDTBundles();
                            
                            // Check if PO is complete (all pipes processed) and close partial bundles
                            CheckAndClosePartialBundlesIfPOComplete(currentOKCuts, currentNDTCuts);
                            
                            // Log summary periodically (every 10 cycles)
                            if ((_totalOKTagsPrinted + _totalNDTTagsPrinted) > 0 && 
                                (_totalOKTagsPrinted + _totalNDTTagsPrinted) % 10 == 0)
                            {
                                _logger?.LogInformation($"📊 PRINTING SUMMARY: OK Tags: {_totalOKTagsPrinted} | NDT Tags: {_totalNDTTagsPrinted} | Total: {_totalOKTagsPrinted + _totalNDTTagsPrinted}");
                            }
                        }
                        else
                        {
                            _isInitialized = true;
                            _logger?.LogInformation("Service initialized. Starting bundle processing...");
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
                if (readyBundles == null)
                {
                    return; // Silently return if database unavailable
                }
                
                if (readyBundles.Count == 0)
                {
                    return; // No bundles ready, skip logging
                }
                
                foreach (var bundle in readyBundles)
                {
                    bool pipeDone = CheckOKBundleDone();
                    bool atPackingStation = _plcService != null && _plcService.IsBundleAtPackingStation(_millId);
                    
                    // Check if we should print: either PLC conditions met OR bypass enabled for testing
                    bool shouldPrint = (pipeDone && atPackingStation) || _bypassOKBundlePLCConditions;
                    
                    if (shouldPrint)
                    {
                        _logger?.LogInformation($"🖨️  PRINTING OK BUNDLE: {bundle.Bundle_No} | Pieces: {bundle.OK_Pcs} | Type: {(bundle.IsFullBundle ? "Full" : "Partial")}");
                        
                        bool printed = _okBundleService.PrintBundleTag(
                            bundle.OKBundle_ID,
                            _printerService,
                            _excelService,
                            _plcService
                        );
                        
                        if (printed)
                        {
                            _totalOKTagsPrinted++;
                            _logger?.LogInformation($"✅ OK TAG PRINTED #{_totalOKTagsPrinted}: {bundle.Bundle_No} ({bundle.OK_Pcs} pieces)");
                        }
                        else
                        {
                            _logger?.LogWarning($"❌ FAILED to print OK bundle: {bundle.Bundle_No}");
                        }
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

        /// <summary>
        /// Check if PO is complete (all pipes processed) and close any partial bundles
        /// PO is complete when: Total OK pipes processed = PLC OK count AND Total NDT pipes processed = PLC NDT count
        /// </summary>
        private void CheckAndClosePartialBundlesIfPOComplete(int currentOKCuts, int currentNDTCuts)
        {
            try
            {
                // Get active PO
                var activeSlit = _repository?.GetActiveSlit(0);
                if (activeSlit == null) return;

                var poPlan = _repository?.GetPOPlan(activeSlit.PO_Plan_ID);
                if (poPlan == null) return;

                // Get total pipes processed for this PO
                int totalOKProcessed = _okBundleService.GetTotalOKPipesProcessed(poPlan.PO_Plan_ID);
                int totalNDTProcessed = _ndtBundleService.GetTotalNDTPipesProcessed(poPlan.PO_Plan_ID);

                // Check if all pipes are processed
                bool allOKProcessed = totalOKProcessed >= currentOKCuts;
                bool allNDTProcessed = totalNDTProcessed >= currentNDTCuts;

                if (allOKProcessed && allNDTProcessed && (totalOKProcessed > 0 || totalNDTProcessed > 0))
                {
                    // PO is complete - close any partial bundles
                    _logger?.LogInformation($"✅ PO Complete Detected: OK={totalOKProcessed}/{currentOKCuts}, NDT={totalNDTProcessed}/{currentNDTCuts}. Closing partial bundles...");
                    
                    _okBundleService.ClosePartialBundlesForPO(poPlan.PO_Plan_ID);
                    _ndtBundleService.ClosePartialBundlesForPO(poPlan.PO_Plan_ID);
                    
                    // After closing partial bundles, check for printing again
                    CheckAndPrintOKBundles();
                    CheckAndPrintNDTBundles();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error checking PO completion: {ex.Message}");
            }
        }

        private void ProcessOKBundlePrint()
        {
            try
            {
                var readyBundles = _okBundleService.GetBundlesReadyForPrinting();
                if (readyBundles == null || readyBundles.Count == 0)
                {
                    return;
                }
                
                var bundle = readyBundles.OrderByDescending(b => b.BundleEndTime).First();
                
                bool atPackingStation = _plcService != null && _plcService.IsBundleAtPackingStation(_millId);
                
                // Check if we should print: either at packing station OR bypass enabled for testing
                bool shouldPrint = atPackingStation || _bypassOKBundlePLCConditions;
                
                if (shouldPrint)
                {
                    if (_bypassOKBundlePLCConditions && !atPackingStation)
                    {
                        _logger?.LogInformation($"Processing OK bundle print: {bundle.Bundle_No} (TEST MODE - packing station check bypassed)");
                        _logger?.LogInformation($"  → Actual AtPackingStation: {atPackingStation}");
                    }
                    else
                    {
                        _logger?.LogInformation($"Processing OK bundle print: {bundle.Bundle_No} (Status: {bundle.Status}, AtPackingStation: {atPackingStation})");
                    }
                    
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
                    _logger?.LogInformation($"OK bundle {bundle.Bundle_No} waiting for packing station (L1L2_PipeDone received but not at packing station)");
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



