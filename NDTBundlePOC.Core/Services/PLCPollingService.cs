using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NDTBundlePOC.Core.Services
{
    /// <summary>
    /// PLC Polling Service - Continuously reads from PLC and processes pipe cuts
    /// This service runs in the background and automatically:
    /// 1. Reads OK and NDT cuts from PLC
    /// 2. Forms bundles automatically
    /// 3. Triggers printing when bundles complete
    /// </summary>
    public class PLCPollingService : BackgroundService
    {
        private readonly IPLCService _plcService;
        private readonly INDTBundleService _ndtBundleService;
        private readonly IOKBundleService _okBundleService;
        private readonly IPrinterService _printerService;
        private readonly ExcelExportService _excelService;
        private readonly ILogger<PLCPollingService> _logger;
        private readonly IPipeCountingActivityService _activityService;
        
        private readonly int _millId;
        private readonly int _pollingIntervalMs;
        
        // Track previous values to detect changes
        private int _previousOKCuts = 0;
        private int _previousNDTCuts = 0;
        private bool _previousOKBundleDone = false;
        private bool _previousNDTBundleDone = false;

        public PLCPollingService(
            IPLCService plcService,
            INDTBundleService ndtBundleService,
            IOKBundleService okBundleService,
            IPrinterService printerService,
            ExcelExportService excelService,
            ILogger<PLCPollingService> logger,
            int millId = 1,
            int pollingIntervalMs = 1000,
            IPipeCountingActivityService activityService = null) // Optional activity service
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger?.LogInformation("PLC Polling Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_plcService.IsConnected)
                    {
                        // Read OK cuts from PLC
                        int currentOKCuts = ReadOKCuts();
                        if (currentOKCuts > _previousOKCuts)
                        {
                            int newOKCuts = currentOKCuts - _previousOKCuts;
                            _previousOKCuts = currentOKCuts;
                            
                            _logger?.LogInformation($"Detected {newOKCuts} new OK cuts");
                            
                            // Log activity if service is available
                            _activityService?.LogActivity("OK", newOKCuts, currentOKCuts, _previousNDTCuts, "PLC");
                            
                            _okBundleService.ProcessOKCuts(_millId, newOKCuts);
                            
                            // Check for completed OK bundles and print
                            CheckAndPrintOKBundles();
                        }

                        // Read NDT cuts from PLC
                        int currentNDTCuts = _plcService.ReadNDTCuts(_millId);
                        if (currentNDTCuts > _previousNDTCuts)
                        {
                            int newNDTCuts = currentNDTCuts - _previousNDTCuts;
                            _previousNDTCuts = currentNDTCuts;
                            
                            _logger?.LogInformation($"Detected {newNDTCuts} new NDT cuts");
                            
                            // Log activity if service is available
                            _activityService?.LogActivity("NDT", newNDTCuts, _previousOKCuts, currentNDTCuts, "PLC");
                            
                            _ndtBundleService.ProcessNDTCuts(_millId, newNDTCuts);
                            
                            // Check for completed NDT bundles and print
                            CheckAndPrintNDTBundles();
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

                await Task.Delay(_pollingIntervalMs, stoppingToken);
            }

            _logger?.LogInformation("PLC Polling Service stopped");
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
                // Check if error is due to object not existing or address out of range (DB250.DBX6.0 may not be configured)
                string errorMsg = ex.Message?.ToLower() ?? "";
                if (errorMsg.Contains("object does not exist") || 
                    errorMsg.Contains("does not exist") ||
                    errorMsg.Contains("not found") ||
                    errorMsg.Contains("out of range") ||
                    errorMsg.Contains("address out of range"))
                {
                    // Silently handle missing NDT Bundle Done signal - just return false
                    _logger?.LogDebug("NDT Bundle Done signal (DB250.DBX6.0) not found or out of range in PLC - using bundle completion logic instead");
                    return false;
                }
                else
                {
                    // Log other errors (connection issues, etc.)
                    _logger?.LogError(ex, "Error checking NDT Bundle Done signal");
                    return false;
                }
            }
        }

        private void CheckAndPrintOKBundles()
        {
            try
            {
                var readyBundles = _okBundleService.GetBundlesReadyForPrinting();
                foreach (var bundle in readyBundles)
                {
                    _logger?.LogInformation($"Printing OK bundle: {bundle.Bundle_No}");
                    _okBundleService.PrintBundleTag(
                        bundle.OKBundle_ID,
                        _printerService,
                        _excelService,
                        _plcService
                    );
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
                foreach (var bundle in readyBundles)
                {
                    _logger?.LogInformation($"Printing NDT bundle: {bundle.Bundle_No}");
                    _ndtBundleService.PrintBundleTag(
                        bundle.NDTBundle_ID,
                        _printerService,
                        _excelService,
                        _plcService
                    );
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error printing NDT bundles");
            }
        }

        private void ProcessOKBundlePrint()
        {
            try
            {
                // Get the most recent completed OK bundle
                var readyBundles = _okBundleService.GetBundlesReadyForPrinting();
                if (readyBundles.Count > 0)
                {
                    var bundle = readyBundles.OrderByDescending(b => b.BundleEndTime).First();
                    _logger?.LogInformation($"Processing OK bundle print: {bundle.Bundle_No}");
                    
                    _okBundleService.PrintBundleTag(
                        bundle.OKBundle_ID,
                        _printerService,
                        _excelService,
                        _plcService
                    );

                    // Acknowledge to PLC
                    if (_plcService is RealS7PLCService realPlc)
                    {
                        realPlc.WriteAcknowledgment("DB260.DBX3.4", true); // L2L1_AckPipeDone
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
                // Get the most recent completed NDT bundle
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

                    // Acknowledge to PLC
                    if (_plcService is RealS7PLCService realPlc)
                    {
                        realPlc.WriteAcknowledgment("DB260.DBX6.0", true); // L2L1_AckNDTBundleDone
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing NDT bundle print");
            }
        }

        public override void Dispose()
        {
            _logger?.LogInformation("PLC Polling Service disposing");
            base.Dispose();
        }
    }
}

