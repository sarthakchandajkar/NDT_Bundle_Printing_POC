using System;
using System.Configuration;
using System.Windows.Forms;
using NDTBundlePOC.Core.Services;
using NDTBundlePOC.UI;

namespace NDTBundlePOC.UI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Initialize services
            // To use Supabase database, replace InMemoryDataRepository with SupabaseDataRepository:
            // var repository = new SupabaseDataRepository();
            var repository = new InMemoryDataRepository();
            repository.InitializeDummyData();
            
            var bundleService = new NDTBundleService(repository);
            var okBundleService = new OKBundleService(repository);
            var printerService = new TelerikReportingPrinterService();
            var excelService = new ExcelExportService();
            
            // Initialize PLC service (optional - can be null if not using PLC)
            IPLCService plcService = null;
            PLCPollingController pollingController = null;
            
            try
            {
                // Try to read PLC configuration from app.config
                var plcIp = ConfigurationManager.AppSettings["PLC:IPAddress"];
                if (string.IsNullOrEmpty(plcIp)) plcIp = "192.168.0.74";
                
                var millIdStr = ConfigurationManager.AppSettings["PLC:MillId"];
                var millId = int.TryParse(millIdStr, out int m) ? m : 1;
                
                var pollingIntervalStr = ConfigurationManager.AppSettings["PLC:PollingIntervalMs"];
                var pollingInterval = int.TryParse(pollingIntervalStr, out int p) ? p : 1000;
                
                // Initialize PLC service
                plcService = new RealS7PLCService();
                // Note: Connection will be attempted when Start button is clicked
                
                // Create polling controller
                pollingController = new PLCPollingController(plcService, bundleService, okBundleService, millId, pollingInterval);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Warning: Could not initialize PLC service: {ex.Message}\n\nPLC polling will be disabled.", 
                    "PLC Initialization Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
            // Create and show main form
            Application.Run(new MainForm(bundleService, printerService, excelService, repository, plcService, pollingController));
        }
    }
}
