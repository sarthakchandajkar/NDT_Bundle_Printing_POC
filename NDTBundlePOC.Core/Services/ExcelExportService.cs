using System;
using System.IO;
using OfficeOpenXml;
using NDTBundlePOC.Core.Services;

namespace NDTBundlePOC.Core.Services
{
    public class ExcelExportService
    {
        private readonly string _exportPath;

        public ExcelExportService(string exportPath = null)
        {
            _exportPath = exportPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "NDT_Bundle_POC_Exports");
            
            if (!Directory.Exists(_exportPath))
            {
                Directory.CreateDirectory(_exportPath);
            }

            // Set EPPlus license context (required for non-commercial use)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public void ExportNDTBundleToExcel(NDTBundlePrintData printData)
        {
            try
            {
                string fileName = $"{printData.PO_No}_{printData.BundleNo}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                string filePath = Path.Combine(_exportPath, fileName);

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("NDT_Bundle");

                    // Headers
                    worksheet.Cells[1, 1].Value = "PO_No";
                    worksheet.Cells[1, 2].Value = "Mill_Line";
                    worksheet.Cells[1, 3].Value = "Bundle_No";
                    worksheet.Cells[1, 4].Value = "NDT_Pcs";
                    worksheet.Cells[1, 5].Value = "Batch_No";
                    worksheet.Cells[1, 6].Value = "Bundle_Start";
                    worksheet.Cells[1, 7].Value = "Bundle_End";
                    worksheet.Cells[1, 8].Value = "Pipe_Grade";
                    worksheet.Cells[1, 9].Value = "Pipe_Size";
                    worksheet.Cells[1, 10].Value = "Pipe_Len";
                    worksheet.Cells[1, 11].Value = "Export_Date";

                    // Data
                    worksheet.Cells[2, 1].Value = printData.PO_No;
                    worksheet.Cells[2, 2].Value = 1; // Mill Line
                    worksheet.Cells[2, 3].Value = printData.BundleNo;
                    worksheet.Cells[2, 4].Value = printData.NDT_Pcs;
                    worksheet.Cells[2, 5].Value = printData.BatchNo;
                    worksheet.Cells[2, 6].Value = printData.BundleStartTime;
                    worksheet.Cells[2, 7].Value = printData.BundleEndTime;
                    worksheet.Cells[2, 8].Value = printData.Pipe_Grade;
                    worksheet.Cells[2, 9].Value = printData.Pipe_Size;
                    worksheet.Cells[2, 10].Value = printData.Pipe_Len;
                    worksheet.Cells[2, 11].Value = DateTime.Now;

                    // Format date columns
                    worksheet.Cells[2, 6].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                    worksheet.Cells[2, 7].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                    worksheet.Cells[2, 11].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";

                    // Auto-fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // Save file
                    package.SaveAs(new FileInfo(filePath));
                }

                Console.WriteLine($"✓ Excel export created: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error exporting to Excel: {ex.Message}");
                // Fallback to CSV
                ExportNDTBundleToCSV(printData);
            }
        }

        private void ExportNDTBundleToCSV(NDTBundlePrintData printData)
        {
            try
            {
                string fileName = $"{printData.PO_No}_{printData.BundleNo}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                string filePath = Path.Combine(_exportPath, fileName);

                var csv = new System.Text.StringBuilder();
                csv.AppendLine("PO_No,Mill_Line,Bundle_No,NDT_Pcs,Batch_No,Bundle_Start,Bundle_End,Pipe_Grade,Pipe_Size,Pipe_Len,Export_Date");
                csv.AppendLine($"{printData.PO_No},1,{printData.BundleNo},{printData.NDT_Pcs},{printData.BatchNo}," +
                              $"{printData.BundleStartTime:yyyy-MM-dd HH:mm:ss},{printData.BundleEndTime:yyyy-MM-dd HH:mm:ss}," +
                              $"{printData.Pipe_Grade},{printData.Pipe_Size},{printData.Pipe_Len},{DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                File.WriteAllText(filePath, csv.ToString());
                Console.WriteLine($"✓ CSV export created: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error exporting to CSV: {ex.Message}");
            }
        }
    }
}

