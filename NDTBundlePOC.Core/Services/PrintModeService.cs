using System;
using Microsoft.Extensions.Logging;

namespace NDTBundlePOC.Core.Services
{
    /// <summary>
    /// Service to manage printing mode (Test Mode vs Physical Printing)
    /// Allows dynamic switching between logging and physical printing
    /// </summary>
    public interface IPrintModeService
    {
        bool IsTestMode { get; }
        bool SetTestMode(bool testMode);
        string GetCurrentModeDescription();
    }

    public class PrintModeService : IPrintModeService
    {
        private bool _isTestMode;
        private readonly ILogger<PrintModeService> _logger;
        private readonly object _lockObject = new object();

        public bool IsTestMode 
        { 
            get 
            { 
                lock (_lockObject)
                {
                    return _isTestMode;
                }
            } 
        }

        public PrintModeService(ILogger<PrintModeService> logger, bool initialTestMode = true)
        {
            _logger = logger;
            _isTestMode = initialTestMode;
            _logger?.LogInformation($"Print Mode Service initialized: {(initialTestMode ? "TEST MODE" : "PRODUCTION MODE")}");
        }

        public bool SetTestMode(bool testMode)
        {
            lock (_lockObject)
            {
                bool previousMode = _isTestMode;
                _isTestMode = testMode;
                
                if (previousMode != testMode)
                {
                    string previousModeStr = previousMode ? "TEST MODE" : "PRODUCTION MODE";
                    string newModeStr = testMode ? "TEST MODE" : "PRODUCTION MODE";
                    _logger?.LogInformation($"Print Mode changed: {previousModeStr} → {newModeStr}");
                    Console.WriteLine($"🔄 Print Mode changed: {previousModeStr} → {newModeStr}");
                    Console.WriteLine($"   → Current mode: {(testMode ? "TEST MODE (Logging Only)" : "PRODUCTION MODE (Physical Printing)")}");
                }
                else
                {
                    Console.WriteLine($"⚠ Print Mode unchanged: Already in {(testMode ? "TEST MODE" : "PRODUCTION MODE")}");
                }
                
                return true;
            }
        }

        public string GetCurrentModeDescription()
        {
            lock (_lockObject)
            {
                return _isTestMode 
                    ? "TEST MODE - Tags logged to file (no physical printing)" 
                    : "PRODUCTION MODE - Physical printing enabled";
            }
        }
    }
}

