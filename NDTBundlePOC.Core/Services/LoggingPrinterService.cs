using System;
using System.IO;
using System.Text;

namespace NDTBundlePOC.Core.Services
{
    /// <summary>
    /// Logging Printer Service - Logs all tag print attempts to a file for testing/verification
    /// Use this instead of actual printing during testing to verify tag data without physical printing
    /// </summary>
    public class LoggingPrinterService : IPrinterService
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();

        public LoggingPrinterService(string logFilePath = null)
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                // Default log file location
                string logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "NDT_Bundle_POC_PrintLogs");
                
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                _logFilePath = Path.Combine(logDirectory, $"PrintLog_{DateTime.Now:yyyyMMdd}.txt");
            }
            else
            {
                _logFilePath = logFilePath;
                string logDirectory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
            }
            
            // Write header if file is new
            if (!File.Exists(_logFilePath))
            {
                WriteLogHeader();
            }
        }

        public bool PrintNDTBundleTag(NDTBundlePrintData printData)
        {
            try
            {
                lock (_lockObject)
                {
                    var logEntry = new StringBuilder();
                    logEntry.AppendLine("========================================");
                    logEntry.AppendLine($"TAG PRINT ATTEMPT - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    logEntry.AppendLine("========================================");
                    logEntry.AppendLine($"Bundle Type:     {(printData.NDT_Pcs > 0 ? "NDT Bundle" : "OK Bundle")}");
                    logEntry.AppendLine($"Bundle No:       {printData.BundleNo}");
                    logEntry.AppendLine($"Batch No:        {printData.BatchNo ?? "N/A"}");
                    logEntry.AppendLine($"PO No:           {printData.PO_No ?? "N/A"}");
                    logEntry.AppendLine($"Pieces:          {printData.NDT_Pcs}");
                    logEntry.AppendLine($"Pipe Grade:      {printData.Pipe_Grade ?? "N/A"}");
                    logEntry.AppendLine($"Pipe Size:       {printData.Pipe_Size ?? "N/A"}");
                    logEntry.AppendLine($"Pipe Length:     {printData.Pipe_Len} m");
                    logEntry.AppendLine($"Bundle Start:    {printData.BundleStartTime:yyyy-MM-dd HH:mm:ss}");
                    logEntry.AppendLine($"Bundle End:      {printData.BundleEndTime:yyyy-MM-dd HH:mm:ss}");
                    logEntry.AppendLine($"Is Reprint:      {printData.IsReprint}");
                    logEntry.AppendLine($"Status:          LOGGED (Test Mode - No Physical Print)");
                    logEntry.AppendLine("========================================");
                    logEntry.AppendLine();

                    // Append to log file
                    File.AppendAllText(_logFilePath, logEntry.ToString());
                    
                    // Also write to console (minimal logging - main logging is in ControllablePLCPollingService)
                    // Console output removed to reduce noise - check log file for details
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error logging tag: {ex.Message}");
                Console.WriteLine($"  Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public string GetPrinterName()
        {
            return $"Logging Printer Service (Test Mode) - Log: {_logFilePath}";
        }

        private void WriteLogHeader()
        {
            try
            {
                var header = new StringBuilder();
                header.AppendLine("========================================");
                header.AppendLine("  NDT BUNDLE PRINTING - TEST MODE LOG");
                header.AppendLine("========================================");
                header.AppendLine($"Log Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                header.AppendLine($"Log File: {_logFilePath}");
                header.AppendLine("========================================");
                header.AppendLine("NOTE: This is TEST MODE - No physical printing is performed.");
                header.AppendLine("All tag print attempts are logged here for verification.");
                header.AppendLine("========================================");
                header.AppendLine();
                
                File.WriteAllText(_logFilePath, header.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Could not write log header: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the current log file path
        /// </summary>
        public string GetLogFilePath()
        {
            return _logFilePath;
        }
    }
}

