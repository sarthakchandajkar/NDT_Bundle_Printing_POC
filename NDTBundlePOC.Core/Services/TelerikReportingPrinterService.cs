using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
// Telerik Reporting namespaces - uncomment after installing Telerik Reporting
// See TELERIK_SETUP.md for installation instructions
#if TELERIK_REPORTING
using Telerik.Reporting;
using Telerik.Reporting.Processing;
using Telerik.Reporting.ImageRendering;
#endif
using NDTBundlePOC.Core.Services;

namespace NDTBundlePOC.Core.Services
{
    public class TelerikReportingPrinterService : IPrinterService
    {
        private readonly string _printerAddress; // IP address or printer name
        private readonly int _printerPort; // TCP port (9100 for raw printing) or 0 for Windows printer
        private readonly bool _useNetwork; // true for Ethernet, false for Windows printer
        private readonly string _outputPath; // For fallback file output
        private readonly string _reportTemplatePath; // Path to .trdp or .trdx report file

        public TelerikReportingPrinterService(
            string printerAddress = "192.168.1.200", 
            int printerPort = 9100, 
            bool useNetwork = true, 
            string reportTemplatePath = null,
            string outputPath = null)
        {
            _printerAddress = printerAddress;
            _printerPort = printerPort;
            _useNetwork = useNetwork;
            _reportTemplatePath = reportTemplatePath;
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
#if TELERIK_REPORTING
                // Generate report using Telerik Reporting
                byte[] reportBytes = GenerateReport(printData);
                
                if (reportBytes == null || reportBytes.Length == 0)
                {
                    Console.WriteLine("✗ Failed to generate report");
                    return PrintToFile(printData);
                }

                // Print via network or Windows printer
                if (_useNetwork)
                {
                    if (TryPrintViaNetwork(reportBytes))
                    {
                        return true;
                    }
                }
                else
                {
                    if (TryPrintViaWindowsPrinter(reportBytes))
                    {
                        return true;
                    }
                }

                // Fallback to file output
                Console.WriteLine($"⚠ Printing failed ({(_useNetwork ? "network" : "Windows printer")}), falling back to file output");
                return PrintToFile(printData, reportBytes);
#else
                // Telerik Reporting not installed - use file output
                Console.WriteLine("⚠ Telerik Reporting not installed. Using file output.");
                return PrintToFile(printData);
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error printing tag: {ex.Message}");
                return PrintToFile(printData);
            }
        }

#if TELERIK_REPORTING
        private byte[] GenerateReport(NDTBundlePrintData printData)
        {
            try
            {
                Report report;

                // If report template file exists, load it
                if (!string.IsNullOrEmpty(_reportTemplatePath) && File.Exists(_reportTemplatePath))
                {
                    var serializer = new Telerik.Reporting.XmlSerialization.ReportXmlSerializer();
                    report = serializer.Deserialize(_reportTemplatePath);
                }
                else
                {
                    // Create report programmatically
                    report = CreateReportProgrammatically(printData);
                }

                // Set report parameters if they exist
                if (report.ReportParameters != null && report.ReportParameters.Count > 0)
                {
                    SetReportParameter(report, "BundleNo", printData.BundleNo);
                    SetReportParameter(report, "BatchNo", printData.BatchNo);
                    SetReportParameter(report, "PO_No", printData.PO_No);
                    SetReportParameter(report, "NDT_Pcs", printData.NDT_Pcs);
                    SetReportParameter(report, "Pipe_Grade", printData.Pipe_Grade);
                    SetReportParameter(report, "Pipe_Size", printData.Pipe_Size);
                    SetReportParameter(report, "Pipe_Len", printData.Pipe_Len);
                    SetReportParameter(report, "BundleStartTime", printData.BundleStartTime);
                    SetReportParameter(report, "BundleEndTime", printData.BundleEndTime);
                }

                // Render report to image
                ReportProcessor reportProcessor = new ReportProcessor();
                InstanceReportSource instanceReportSource = new InstanceReportSource
                {
                    ReportDocument = report
                };
                
                RenderingResult result = reportProcessor.RenderReport("IMAGE", instanceReportSource, null);

                return result.DocumentBytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error generating Telerik report: {ex.Message}");
                return null;
            }
        }

        private void SetReportParameter(Report report, string paramName, object value)
        {
            if (report.ReportParameters.Contains(paramName))
            {
                report.ReportParameters[paramName].Value = value;
            }
        }

        private Report CreateReportProgrammatically(NDTBundlePrintData printData)
        {
            // Create a new report programmatically
            Report report = new Report();
            report.Name = "NDT_Bundle_Tag";
            report.PageSettings.PaperKind = System.Drawing.Printing.PaperKind.Custom;
            report.PageSettings.Width = new Telerik.Reporting.Drawing.Unit(4, Telerik.Reporting.Drawing.UnitType.Inch);
            report.PageSettings.Height = new Telerik.Reporting.Drawing.Unit(2, Telerik.Reporting.Drawing.UnitType.Inch);
            report.PageSettings.Margins = new Telerik.Reporting.Drawing.MarginsU(
                new Telerik.Reporting.Drawing.Unit(0.1, Telerik.Reporting.Drawing.UnitType.Inch),
                new Telerik.Reporting.Drawing.Unit(0.1, Telerik.Reporting.Drawing.UnitType.Inch),
                new Telerik.Reporting.Drawing.Unit(0.1, Telerik.Reporting.Drawing.UnitType.Inch),
                new Telerik.Reporting.Drawing.Unit(0.1, Telerik.Reporting.Drawing.UnitType.Inch));

            // Create report header
            TextBox title = new TextBox();
            title.Name = "Title";
            title.Value = "NDT BUNDLE TAG";
            title.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0.2), Telerik.Reporting.Drawing.Unit.Inch(0.1));
            title.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(3.6), Telerik.Reporting.Drawing.Unit.Inch(0.2));
            title.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(12, Telerik.Reporting.Drawing.UnitType.Point);
            title.Style.Font.Bold = true;
            title.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            report.Items.Add(title);

            // Bundle Number
            AddLabel(report, "Bundle No:", 0.2, 0.4, 1.0, 0.15, 8);
            AddValue(report, printData.BundleNo, 1.2, 0.4, 2.4, 0.15, 10, true);

            // Batch Number
            AddLabel(report, "Batch No:", 0.2, 0.6, 1.0, 0.15, 8);
            AddValue(report, printData.BatchNo, 1.2, 0.6, 2.4, 0.15, 10, true);

            // PO Number
            AddLabel(report, "PO No:", 0.2, 0.8, 1.0, 0.15, 8);
            AddValue(report, printData.PO_No, 1.2, 0.8, 2.4, 0.15, 10, true);

            // NDT Pieces
            AddLabel(report, "NDT Pieces:", 0.2, 1.0, 1.0, 0.15, 8);
            AddValue(report, printData.NDT_Pcs.ToString(), 1.2, 1.0, 0.8, 0.15, 10, true);

            // Pipe Grade
            AddLabel(report, "Pipe Grade:", 2.0, 1.0, 1.0, 0.15, 8);
            AddValue(report, printData.Pipe_Grade, 3.0, 1.0, 0.8, 0.15, 10, true);

            // Pipe Size
            AddLabel(report, "Pipe Size:", 0.2, 1.2, 1.0, 0.15, 8);
            AddValue(report, printData.Pipe_Size, 1.2, 1.2, 0.8, 0.15, 10, true);

            // Pipe Length
            AddLabel(report, "Pipe Length:", 2.0, 1.2, 1.0, 0.15, 8);
            AddValue(report, $"{printData.Pipe_Len} m", 3.0, 1.2, 0.8, 0.15, 10, true);

            // Start Time
            AddLabel(report, "Start:", 0.2, 1.4, 0.6, 0.15, 7);
            AddValue(report, printData.BundleStartTime.ToString("yyyy-MM-dd HH:mm:ss"), 0.8, 1.4, 1.6, 0.15, 7);

            // End Time
            AddLabel(report, "End:", 2.4, 1.4, 0.6, 0.15, 7);
            AddValue(report, printData.BundleEndTime.ToString("yyyy-MM-dd HH:mm:ss"), 3.0, 1.4, 0.8, 0.15, 7);

            // Barcode (using text representation for now)
            TextBox barcode = new TextBox();
            barcode.Name = "Barcode";
            barcode.Value = $"*{printData.BundleNo}*";
            barcode.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0.2), Telerik.Reporting.Drawing.Unit.Inch(1.6));
            barcode.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(3.6), Telerik.Reporting.Drawing.Unit.Inch(0.2));
            barcode.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(14, Telerik.Reporting.Drawing.UnitType.Point);
            barcode.Style.Font.Bold = true;
            barcode.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            report.Items.Add(barcode);

            return report;
        }

        private void AddLabel(Report report, string text, double x, double y, double width, double height, int fontSize)
        {
            TextBox label = new TextBox();
            label.Name = $"Label_{text.Replace(" ", "_").Replace(":", "")}";
            label.Value = text;
            label.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(x), Telerik.Reporting.Drawing.Unit.Inch(y));
            label.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(width), Telerik.Reporting.Drawing.Unit.Inch(height));
            label.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(fontSize, Telerik.Reporting.Drawing.UnitType.Point);
            report.Items.Add(label);
        }

        private void AddValue(Report report, string value, double x, double y, double width, double height, int fontSize, bool bold = false)
        {
            TextBox textBox = new TextBox();
            textBox.Name = $"Value_{x}_{y}";
            textBox.Value = value;
            textBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(x), Telerik.Reporting.Drawing.Unit.Inch(y));
            textBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(width), Telerik.Reporting.Drawing.Unit.Inch(height));
            textBox.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(fontSize, Telerik.Reporting.Drawing.UnitType.Point);
            textBox.Style.Font.Bold = bold;
            report.Items.Add(textBox);
        }
#endif

        private bool TryPrintViaNetwork(byte[] reportBytes)
        {
            TcpClient tcpClient = null;
            NetworkStream stream = null;
            
            try
            {
                // Connect to printer via TCP/IP (port 9100 is standard for raw printing)
                tcpClient = new TcpClient();
                tcpClient.Connect(_printerAddress, _printerPort);
                
                if (!tcpClient.Connected)
                {
                    Console.WriteLine($"✗ Failed to connect to printer at {_printerAddress}:{_printerPort}");
                    return false;
                }

                // Send report bytes
                stream = tcpClient.GetStream();
                stream.Write(reportBytes, 0, reportBytes.Length);
                stream.Flush();

                // Wait a bit for the print job to complete
                Thread.Sleep(500);

                Console.WriteLine($"✓ NDT Bundle Tag printed via network ({_printerAddress}:{_printerPort})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Network printing error: {ex.Message}");
                return false;
            }
            finally
            {
                stream?.Close();
                tcpClient?.Close();
            }
        }

        private bool TryPrintViaWindowsPrinter(byte[] reportBytes)
        {
            try
            {
                // Use Windows default printer or specified printer name
                System.Drawing.Printing.PrintDocument printDoc = new System.Drawing.Printing.PrintDocument();
                printDoc.PrinterSettings.PrinterName = _printerAddress; // Use printer name if not IP
                
                if (string.IsNullOrEmpty(printDoc.PrinterSettings.PrinterName))
                {
                    printDoc.PrinterSettings.PrinterName = System.Drawing.Printing.PrinterSettings.InstalledPrinters[0];
                }

                // Convert report bytes to image and print
                using (MemoryStream ms = new MemoryStream(reportBytes))
                {
                    System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
                    printDoc.PrintPage += (sender, e) =>
                    {
                        e.Graphics.DrawImage(img, e.PageBounds);
                        e.HasMorePages = false;
                    };
                    printDoc.Print();
                }

                Console.WriteLine($"✓ NDT Bundle Tag printed via Windows printer: {printDoc.PrinterSettings.PrinterName}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Windows printer error: {ex.Message}");
                return false;
            }
        }

        private bool PrintToFile(NDTBundlePrintData printData, byte[] reportBytes = null)
        {
            try
            {
                string fileName = $"NDT_Tag_{printData.BundleNo}_{DateTime.Now:yyyyMMddHHmmss}";
                string filePath;

                if (reportBytes != null)
                {
                    // Save as image/PDF
                    filePath = Path.Combine(_outputPath, $"{fileName}.png");
                    File.WriteAllBytes(filePath, reportBytes);
                }
                else
                {
                    // Fallback to text file
                    filePath = Path.Combine(_outputPath, $"{fileName}.txt");
                    var sb = new StringBuilder();
                    sb.AppendLine("========================================");
                    sb.AppendLine("     NDT BUNDLE TAG - TELERIK REPORTING");
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
                }
                
                Console.WriteLine($"✓ NDT Bundle Tag saved to file: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error saving tag to file: {ex.Message}");
                return false;
            }
        }

        public string GetPrinterName()
        {
            if (_useNetwork)
            {
                return $"Telerik_Reporting_Printer (Network: {_printerAddress}:{_printerPort})";
            }
            else
            {
                return $"Telerik_Reporting_Printer (Windows: {_printerAddress})";
            }
        }
    }
}
