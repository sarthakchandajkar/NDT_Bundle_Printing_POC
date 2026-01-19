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
                    // Create mock Rpt_MillLabel report (matches original Rpt_MillLabel design)
                    report = CreateMockMillLabelReport(printData);
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

        /// <summary>
        /// Creates a mock Rpt_MillLabel report matching the original design
        /// This is used for testing without requiring database connection
        /// </summary>
        private Report CreateMockMillLabelReport(NDTBundlePrintData printData)
        {
            Report report = new Report();
            report.Name = "Rpt_MillLabel_Mock";
            report.PageSettings.PaperKind = System.Drawing.Printing.PaperKind.Custom;
            report.PageSettings.Width = new Telerik.Reporting.Drawing.Unit(100, Telerik.Reporting.Drawing.UnitType.Mm);
            report.PageSettings.Height = new Telerik.Reporting.Drawing.Unit(100, Telerik.Reporting.Drawing.UnitType.Mm);
            report.PageSettings.Margins = new Telerik.Reporting.Drawing.MarginsU(
                Telerik.Reporting.Drawing.Unit.Mm(0),
                Telerik.Reporting.Drawing.Unit.Mm(0),
                Telerik.Reporting.Drawing.Unit.Mm(0),
                Telerik.Reporting.Drawing.Unit.Mm(0));

            DetailSection detail = new DetailSection();
            detail.Height = new Telerik.Reporting.Drawing.Unit(9.7, Telerik.Reporting.Drawing.UnitType.Cm);
            detail.Style.Padding.Top = new Telerik.Reporting.Drawing.Unit(0, Telerik.Reporting.Drawing.UnitType.Cm);

            Panel panel1 = new Panel();
            panel1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Mm(2), Telerik.Reporting.Drawing.Unit.Mm(2));
            panel1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Cm(9.7), Telerik.Reporting.Drawing.Unit.Cm(9.5));
            panel1.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            panel1.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);

            // Header: "AJSPC - OMAN" (textBox5)
            TextBox textBox5 = new TextBox();
            textBox5.Name = "textBox5";
            textBox5.Value = "AJSPC - OMAN";
            textBox5.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(4.8), Telerik.Reporting.Drawing.Unit.Cm(0));
            textBox5.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(1.181), Telerik.Reporting.Drawing.Unit.Inch(0.709));
            textBox5.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            textBox5.Style.Font.Bold = true;
            textBox5.Style.Font.Name = "Microsoft New Tai Lue";
            textBox5.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(11, Telerik.Reporting.Drawing.UnitType.Point);
            textBox5.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textBox5.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            textBox5.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textBox5);

            // PictureBox1 (logo placeholder - using text for now)
            TextBox pictureBox1 = new TextBox();
            pictureBox1.Name = "PictureBox1";
            pictureBox1.Value = "[LOGO]";
            pictureBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(3.071), Telerik.Reporting.Drawing.Unit.Inch(0));
            pictureBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.748), Telerik.Reporting.Drawing.Unit.Inch(0.709));
            pictureBox1.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            pictureBox1.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            pictureBox1.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            pictureBox1.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(pictureBox1);

            // SPECIFICATION label (textBox6)
            TextBox textBox6 = new TextBox();
            textBox6.Name = "textBox6";
            textBox6.Value = "SPECIFICATION";
            textBox6.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0), Telerik.Reporting.Drawing.Unit.Cm(1.8));
            textBox6.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(1.102), Telerik.Reporting.Drawing.Unit.Inch(0.394));
            textBox6.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            textBox6.Style.Font.Bold = true;
            textBox6.Style.Font.Name = "Microsoft New Tai Lue";
            textBox6.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(9, Telerik.Reporting.Drawing.UnitType.Point);
            textBox6.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textBox6.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            textBox6.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textBox6);

            // Specification value (textSpecification)
            TextBox textSpecification = new TextBox();
            textSpecification.Name = "textSpecification";
            textSpecification.Value = printData.Pipe_Grade ?? "API 5L X42";
            textSpecification.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(2.8), Telerik.Reporting.Drawing.Unit.Cm(1.8));
            textSpecification.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(2.716), Telerik.Reporting.Drawing.Unit.Inch(0.394));
            textSpecification.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            textSpecification.Style.Font.Bold = false;
            textSpecification.Style.Font.Name = "Microsoft New Tai Lue";
            textSpecification.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(11, Telerik.Reporting.Drawing.UnitType.Point);
            textSpecification.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textSpecification.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            textSpecification.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textSpecification);

            // Type label (textBox7)
            TextBox textBox7 = new TextBox();
            textBox7.Name = "textBox7";
            textBox7.Value = "Type";
            textBox7.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0), Telerik.Reporting.Drawing.Unit.Cm(2.8));
            textBox7.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(1.102), Telerik.Reporting.Drawing.Unit.Inch(0.276));
            textBox7.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            textBox7.Style.Font.Bold = true;
            textBox7.Style.Font.Name = "Microsoft New Tai Lue";
            textBox7.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(10, Telerik.Reporting.Drawing.UnitType.Point);
            textBox7.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textBox7.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            textBox7.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textBox7);

            // Type value (textType)
            TextBox textType = new TextBox();
            textType.Name = "textType";
            textType.Value = printData.Pipe_Grade ?? "ERW";
            textType.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0), Telerik.Reporting.Drawing.Unit.Cm(3.5));
            textType.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(1.102), Telerik.Reporting.Drawing.Unit.Inch(0.349));
            textType.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            textType.Style.Font.Bold = false;
            textType.Style.Font.Name = "Microsoft New Tai Lue";
            textType.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(9, Telerik.Reporting.Drawing.UnitType.Point);
            textType.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textType.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            textType.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textType);

            // Size label (textBox1)
            TextBox textBox1 = new TextBox();
            textBox1.Name = "textBox1";
            textBox1.Value = "Size";
            textBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(2.8), Telerik.Reporting.Drawing.Unit.Cm(2.8));
            textBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.787), Telerik.Reporting.Drawing.Unit.Inch(0.276));
            textBox1.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            textBox1.Style.Font.Bold = true;
            textBox1.Style.Font.Name = "Microsoft New Tai Lue";
            textBox1.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(10, Telerik.Reporting.Drawing.UnitType.Point);
            textBox1.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textBox1.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            textBox1.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textBox1);

            // Size value (textSize)
            TextBox textSize = new TextBox();
            textSize.Name = "textSize";
            textSize.Value = printData.Pipe_Size ?? "4''";
            textSize.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(2.8), Telerik.Reporting.Drawing.Unit.Cm(3.5));
            textSize.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.787), Telerik.Reporting.Drawing.Unit.Inch(0.349));
            textSize.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            textSize.Style.Font.Bold = false;
            textSize.Style.Font.Name = "Microsoft New Tai Lue";
            textSize.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(9, Telerik.Reporting.Drawing.UnitType.Point);
            textSize.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textSize.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            textSize.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textSize);

            // Length label (textBox2)
            TextBox textBox2 = new TextBox();
            textBox2.Name = "textBox2";
            textBox2.Value = "Length";
            textBox2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(4.8), Telerik.Reporting.Drawing.Unit.Cm(2.8));
            textBox2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(1.181), Telerik.Reporting.Drawing.Unit.Inch(0.276));
            textBox2.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            textBox2.Style.Font.Bold = true;
            textBox2.Style.Font.Name = "Microsoft New Tai Lue";
            textBox2.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(10, Telerik.Reporting.Drawing.UnitType.Point);
            textBox2.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textBox2.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            textBox2.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textBox2);

            // Length value (textLen)
            TextBox textLen = new TextBox();
            textLen.Name = "textLen";
            textLen.Value = $"{printData.Pipe_Len}'";
            textLen.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(4.8), Telerik.Reporting.Drawing.Unit.Cm(3.5));
            textLen.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(1.181), Telerik.Reporting.Drawing.Unit.Inch(0.349));
            textLen.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            textLen.Style.Font.Bold = false;
            textLen.Style.Font.Name = "Microsoft New Tai Lue";
            textLen.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(9, Telerik.Reporting.Drawing.UnitType.Point);
            textLen.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textLen.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            textLen.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textLen);

            // Pcs/Bnd label (textBox9)
            TextBox textBox9 = new TextBox();
            textBox9.Name = "textBox9";
            textBox9.Value = "Pcs/Bnd";
            textBox9.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.8), Telerik.Reporting.Drawing.Unit.Cm(2.8));
            textBox9.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.748), Telerik.Reporting.Drawing.Unit.Inch(0.276));
            textBox9.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            textBox9.Style.Font.Bold = true;
            textBox9.Style.Font.Name = "Microsoft New Tai Lue";
            textBox9.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(10, Telerik.Reporting.Drawing.UnitType.Point);
            textBox9.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textBox9.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            textBox9.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textBox9);

            // Pcs/Bnd value (textPcsBund)
            TextBox textPcsBund = new TextBox();
            textPcsBund.Name = "textPcsBund";
            textPcsBund.Value = printData.NDT_Pcs.ToString();
            textPcsBund.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(7.8), Telerik.Reporting.Drawing.Unit.Cm(3.5));
            textPcsBund.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.748), Telerik.Reporting.Drawing.Unit.Inch(0.349));
            textPcsBund.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            textPcsBund.Style.Font.Bold = false;
            textPcsBund.Style.Font.Name = "Microsoft New Tai Lue";
            textPcsBund.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(9, Telerik.Reporting.Drawing.UnitType.Point);
            textPcsBund.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textPcsBund.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            textPcsBund.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textPcsBund);

            // SLIT NUMBER label (textBox8)
            TextBox textBox8 = new TextBox();
            textBox8.Name = "textBox8";
            textBox8.Value = "SLIT NUMBER";
            textBox8.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0), Telerik.Reporting.Drawing.Unit.Cm(4.386));
            textBox8.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(1.299), Telerik.Reporting.Drawing.Unit.Inch(0.36));
            textBox8.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.None;
            textBox8.Style.Font.Bold = true;
            textBox8.Style.Font.Name = "Microsoft New Tai Lue";
            textBox8.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(9, Telerik.Reporting.Drawing.UnitType.Point);
            textBox8.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textBox8.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            textBox8.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textBox8);

            // Slit Number value (textBox3)
            TextBox textBox3 = new TextBox();
            textBox3.Name = "textBox3";
            textBox3.Value = "SLIT-001";
            textBox3.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0), Telerik.Reporting.Drawing.Unit.Cm(5.3));
            textBox3.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(1.299), Telerik.Reporting.Drawing.Unit.Inch(0.315));
            textBox3.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.None;
            textBox3.Style.Font.Bold = false;
            textBox3.Style.Font.Name = "Microsoft New Tai Lue";
            textBox3.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(9, Telerik.Reporting.Drawing.UnitType.Point);
            textBox3.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textBox3.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            textBox3.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textBox3);

            // BUNDLE NUMBER label (textBox10)
            TextBox textBox10 = new TextBox();
            textBox10.Name = "textBox10";
            textBox10.Value = "BUNDLE NUMBER";
            textBox10.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0), Telerik.Reporting.Drawing.Unit.Cm(6.1));
            textBox10.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(1.299), Telerik.Reporting.Drawing.Unit.Inch(0.354));
            textBox10.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.None;
            textBox10.Style.Font.Bold = true;
            textBox10.Style.Font.Name = "Microsoft New Tai Lue";
            textBox10.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(9, Telerik.Reporting.Drawing.UnitType.Point);
            textBox10.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textBox10.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            textBox10.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textBox10);

            // Bundle Number value (textBundleNo)
            TextBox textBundleNo = new TextBox();
            textBundleNo.Name = "textBundleNo";
            textBundleNo.Value = printData.BundleNo;
            textBundleNo.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0), Telerik.Reporting.Drawing.Unit.Cm(7));
            textBundleNo.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(1.299), Telerik.Reporting.Drawing.Unit.Inch(0.315));
            textBundleNo.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.None;
            textBundleNo.Style.Font.Bold = false;
            textBundleNo.Style.Font.Name = "Microsoft New Tai Lue";
            textBundleNo.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(9, Telerik.Reporting.Drawing.UnitType.Point);
            textBundleNo.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textBundleNo.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
            textBundleNo.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textBundleNo);

            // Barcode 1 (horizontal)
            Barcode barcode1 = new Barcode();
            barcode1.Name = "barcode1";
            barcode1.Encoder = new Telerik.Reporting.Barcodes.Code128Encoder();
            barcode1.Value = printData.BundleNo;
            barcode1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Mm(0), Telerik.Reporting.Drawing.Unit.Mm(0));
            barcode1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Mm(48), Telerik.Reporting.Drawing.Unit.Mm(18));
            barcode1.Stretch = false;
            barcode1.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            barcode1.Style.Font.Name = "Calibri";
            barcode1.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(12, Telerik.Reporting.Drawing.UnitType.Point);
            barcode1.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            barcode1.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            barcode1.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Bottom;
            panel1.Items.Add(barcode1);

            // Barcode 2 (vertical - rotated 90 degrees)
            Barcode barcode2 = new Barcode();
            barcode2.Name = "barcode2";
            barcode2.Encoder = new Telerik.Reporting.Barcodes.Code128Encoder();
            barcode2.Angle = 90;
            barcode2.Value = printData.BundleNo;
            barcode2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Mm(78), Telerik.Reporting.Drawing.Unit.Mm(53));
            barcode2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Mm(19), Telerik.Reporting.Drawing.Unit.Mm(42));
            barcode2.Stretch = false;
            barcode2.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            barcode2.Style.Font.Name = "Calibri";
            barcode2.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(12, Telerik.Reporting.Drawing.UnitType.Point);
            barcode2.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            barcode2.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            barcode2.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Bottom;
            panel1.Items.Add(barcode2);

            // "MADE IN OMAN" (textBox4)
            TextBox textBox4 = new TextBox();
            textBox4.Name = "textBox4";
            textBox4.Value = "MADE IN OMAN";
            textBox4.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0), Telerik.Reporting.Drawing.Unit.Cm(8.5));
            textBox4.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(3.071), Telerik.Reporting.Drawing.Unit.Inch(0.394));
            textBox4.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.Solid;
            textBox4.Style.Font.Bold = true;
            textBox4.Style.Font.Name = "Malgun Gothic";
            textBox4.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(12, Telerik.Reporting.Drawing.UnitType.Point);
            textBox4.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            textBox4.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
            textBox4.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
            panel1.Items.Add(textBox4);

            // Reprint indicator (reprintInd) - empty for new prints
            TextBox reprintInd = new TextBox();
            reprintInd.Name = "reprintInd";
            reprintInd.Value = "";
            reprintInd.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Cm(0), Telerik.Reporting.Drawing.Unit.Cm(8.5));
            reprintInd.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.394), Telerik.Reporting.Drawing.Unit.Inch(0.394));
            reprintInd.Style.BorderStyle.Default = Telerik.Reporting.Drawing.BorderType.None;
            reprintInd.Style.Font.Bold = false;
            reprintInd.Style.Font.Name = "Microsoft New Tai Lue";
            reprintInd.Style.Font.Size = new Telerik.Reporting.Drawing.Unit(25, Telerik.Reporting.Drawing.UnitType.Point);
            reprintInd.Style.LineWidth = new Telerik.Reporting.Drawing.Unit(3, Telerik.Reporting.Drawing.UnitType.Point);
            reprintInd.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
            reprintInd.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Top;
            panel1.Items.Add(reprintInd);

            detail.Items.Add(panel1);
            report.Items.Add(detail);

            return report;
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
