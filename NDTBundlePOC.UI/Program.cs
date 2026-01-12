using System;
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
            var repository = new InMemoryDataRepository();
            repository.InitializeDummyData();
            
            var bundleService = new NDTBundleService(repository);
            var printerService = new TelerikReportingPrinterService();
            var excelService = new ExcelExportService();
            
            // Create and show main form
            Application.Run(new MainForm(bundleService, printerService, excelService, repository));
        }
    }
}
