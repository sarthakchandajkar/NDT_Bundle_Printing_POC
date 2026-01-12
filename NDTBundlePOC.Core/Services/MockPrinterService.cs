using System;
using System.IO;
using System.Text;

namespace NDTBundlePOC.Core.Services
{
    public class MockPrinterService : IPrinterService
    {
        private readonly string _outputPath;

        public MockPrinterService(string outputPath = null)
        {
            _outputPath = outputPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "NDT_Bundle_POC_Prints");
            
            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
            }
        }

        public bool PrintNDTBundleTag(NDTBundlePrintData printData)
        {
            try
            {
                // Create a text file simulating the printed tag
                string fileName = $"NDT_Tag_{printData.BundleNo}_{DateTime.Now:yyyyMMddHHmmss}.txt";
                string filePath = Path.Combine(_outputPath, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("========================================");
                sb.AppendLine("     NDT BUNDLE TAG - HONEYWELL PD45S");
                sb.AppendLine("========================================");
                sb.AppendLine($"Bundle No:     {printData.BundleNo}");
                sb.AppendLine($"Batch No:      {printData.BatchNo}");
                sb.AppendLine($"PO No:         {printData.PO_No}");
                sb.AppendLine($"NDT Pieces:    {printData.NDT_Pcs}");
                sb.AppendLine($"Pipe Grade:    {printData.Pipe_Grade}");
                sb.AppendLine($"Pipe Size:     {printData.Pipe_Size}");
                sb.AppendLine($"Pipe Length:   {printData.Pipe_Len} m");
                sb.AppendLine($"Start Time:    {printData.BundleStartTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"End Time:      {printData.BundleEndTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine("========================================");
                sb.AppendLine($"Printed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine("========================================");

                File.WriteAllText(filePath, sb.ToString());
                
                Console.WriteLine($"✓ NDT Bundle Tag printed: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error printing tag: {ex.Message}");
                return false;
            }
        }

        public string GetPrinterName()
        {
            return "Honeywell_PD45S_NDT (Mock)";
        }
    }
}

