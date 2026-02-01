using System;
using Microsoft.Extensions.Logging;

namespace NDTBundlePOC.Core.Services
{
    /// <summary>
    /// Switchable Printer Service - Wraps both LoggingPrinterService and HoneywellPD45SPrinterService
    /// Allows dynamic switching between test mode (logging) and production mode (physical printing)
    /// </summary>
    public class SwitchablePrinterService : IPrinterService
    {
        private readonly LoggingPrinterService _loggingPrinter;
        private readonly HoneywellPD45SPrinterService _physicalPrinter;
        private readonly IPrintModeService _printModeService;
        private readonly ILogger<SwitchablePrinterService> _logger;
        private readonly object _lockObject = new object();

        public SwitchablePrinterService(
            LoggingPrinterService loggingPrinter,
            HoneywellPD45SPrinterService physicalPrinter,
            IPrintModeService printModeService,
            ILogger<SwitchablePrinterService> logger)
        {
            _loggingPrinter = loggingPrinter ?? throw new ArgumentNullException(nameof(loggingPrinter));
            _physicalPrinter = physicalPrinter ?? throw new ArgumentNullException(nameof(physicalPrinter));
            _printModeService = printModeService ?? throw new ArgumentNullException(nameof(printModeService));
            _logger = logger;
        }

        public SwitchablePrinterService(
            IPrintModeService printModeService,
            ILogger<SwitchablePrinterService> logger,
            string logFilePath = null,
            string printerAddress = "192.168.0.125",
            int printerPort = 9100,
            bool useNetwork = true)
        {
            _loggingPrinter = new LoggingPrinterService(logFilePath);
            _physicalPrinter = new HoneywellPD45SPrinterService(printerAddress, printerPort, useNetwork);
            _printModeService = printModeService ?? throw new ArgumentNullException(nameof(printModeService));
            _logger = logger;
        }

        public bool PrintNDTBundleTag(NDTBundlePrintData printData)
        {
            lock (_lockObject)
            {
                bool isTestMode = _printModeService.IsTestMode;
                
                if (isTestMode)
                {
                    // Test Mode: Log to file
                    _logger?.LogDebug($"Printing in TEST MODE: {printData.BundleNo}");
                    return _loggingPrinter.PrintNDTBundleTag(printData);
                }
                else
                {
                    // Production Mode: Physical printing
                    _logger?.LogDebug($"Printing in PRODUCTION MODE: {printData.BundleNo}");
                    return _physicalPrinter.PrintNDTBundleTag(printData);
                }
            }
        }

        public string GetPrinterName()
        {
            lock (_lockObject)
            {
                bool isTestMode = _printModeService.IsTestMode;
                
                if (isTestMode)
                {
                    return _loggingPrinter.GetPrinterName();
                }
                else
                {
                    return _physicalPrinter.GetPrinterName();
                }
            }
        }
    }
}

