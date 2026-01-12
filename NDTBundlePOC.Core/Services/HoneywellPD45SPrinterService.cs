using System;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NDTBundlePOC.Core.Services
{
    public class HoneywellPD45SPrinterService : IPrinterService
    {
        private readonly string _printerAddress; // IP address or COM port
        private readonly int _printerPort; // TCP port (9100 for raw printing) or baud rate for serial
        private readonly bool _useNetwork; // true for Ethernet, false for serial
        private readonly string _outputPath; // For fallback file output

        public HoneywellPD45SPrinterService(string printerAddress = "192.168.1.200", int printerPort = 9100, bool useNetwork = true, string outputPath = null)
        {
            _printerAddress = printerAddress;
            _printerPort = printerPort;
            _useNetwork = useNetwork;
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
                // Try to print via network (Ethernet) or serial based on configuration
                if (_useNetwork)
                {
                    if (TryPrintViaNetwork(printData))
                    {
                        return true;
                    }
                }
                else
                {
                    if (TryPrintViaSerial(printData))
                    {
                        return true;
                    }
                }

                // Fallback to file output if printing fails
                Console.WriteLine($"⚠ Printing failed ({(_useNetwork ? "network" : "serial")}), falling back to file output");
                return PrintToFile(printData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error printing tag: {ex.Message}");
                // Fallback to file output
                return PrintToFile(printData);
            }
        }

        private bool TryPrintViaNetwork(NDTBundlePrintData printData)
        {
            TcpClient tcpClient = null;
            NetworkStream stream = null;
            
            try
            {
                // Generate ZPL (Zebra Programming Language) commands for Honeywell PD45S
                string zplCommands = GenerateZPLCommands(printData);
                byte[] zplBytes = Encoding.UTF8.GetBytes(zplCommands);

                // Connect to printer via TCP/IP (port 9100 is standard for raw printing)
                tcpClient = new TcpClient();
                tcpClient.Connect(_printerAddress, _printerPort);
                
                if (!tcpClient.Connected)
                {
                    Console.WriteLine($"✗ Failed to connect to printer at {_printerAddress}:{_printerPort}");
                    return false;
                }

                // Send ZPL commands
                stream = tcpClient.GetStream();
                stream.Write(zplBytes, 0, zplBytes.Length);
                stream.Flush();

                // Wait a bit for the print job to complete
                Thread.Sleep(500);

                Console.WriteLine($"✓ NDT Bundle Tag printed via network ({_printerAddress}:{_printerPort}): {printData.BundleNo}");
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

        private bool TryPrintViaSerial(NDTBundlePrintData printData)
        {
            SerialPort serialPort = null;
            
            try
            {
                // Open serial port
                serialPort = new SerialPort(_printerAddress, _printerPort, Parity.None, 8, StopBits.One);
                serialPort.Handshake = Handshake.None;
                serialPort.ReadTimeout = 1000;
                serialPort.WriteTimeout = 1000;

                serialPort.Open();

                // Generate ZPL (Zebra Programming Language) commands for Honeywell PD45S
                string zplCommands = GenerateZPLCommands(printData);

                // Send to printer
                serialPort.Write(zplCommands);

                // Wait a bit for the print job to complete
                Thread.Sleep(500);

                Console.WriteLine($"✓ NDT Bundle Tag printed via serial ({_printerAddress}): {printData.BundleNo}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Serial printing error: {ex.Message}");
                return false;
            }
            finally
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    try { serialPort.Close(); } catch { }
                }
            }
        }

        private string GenerateZPLCommands(NDTBundlePrintData printData)
        {
            // Honeywell PD45S uses ZPL (Zebra Programming Language)
            // Adjust these commands based on your actual label template
            var zpl = new StringBuilder();

            // Start ZPL
            zpl.AppendLine("^XA"); // Start of label format

            // Set label size (4x2 inches)
            zpl.AppendLine("^LH0,0"); // Label home position
            zpl.AppendLine("^LL400"); // Label length (in dots, 203 DPI)

            // Title
            zpl.AppendLine("^FO20,20^A0N,30,30^FDNDT BUNDLE TAG^FS");

            // Bundle Number
            zpl.AppendLine($"^FO20,60^A0N,25,25^FDBundle No:^FS");
            zpl.AppendLine($"^FO20,90^A0N,30,30^FD{printData.BundleNo}^FS");

            // Batch Number
            zpl.AppendLine($"^FO20,130^A0N,25,25^FDBatch No:^FS");
            zpl.AppendLine($"^FO20,160^A0N,30,30^FD{printData.BatchNo}^FS");

            // PO Number
            zpl.AppendLine($"^FO20,200^A0N,25,25^FDPO No:^FS");
            zpl.AppendLine($"^FO20,230^A0N,30,30^FD{printData.PO_No}^FS");

            // NDT Pieces
            zpl.AppendLine($"^FO20,270^A0N,25,25^FDNDT Pieces:^FS");
            zpl.AppendLine($"^FO20,300^A0N,30,30^FD{printData.NDT_Pcs}^FS");

            // Pipe Grade
            zpl.AppendLine($"^FO20,340^A0N,25,25^FDPipe Grade:^FS");
            zpl.AppendLine($"^FO20,370^A0N,30,30^FD{printData.Pipe_Grade}^FS");

            // Pipe Size
            zpl.AppendLine($"^FO220,60^A0N,25,25^FDPipe Size:^FS");
            zpl.AppendLine($"^FO220,90^A0N,30,30^FD{printData.Pipe_Size}^FS");

            // Pipe Length
            zpl.AppendLine($"^FO220,130^A0N,25,25^FDPipe Length:^FS");
            zpl.AppendLine($"^FO220,160^A0N,30,30^FD{printData.Pipe_Len} m^FS");

            // Start Time
            zpl.AppendLine($"^FO220,200^A0N,20,20^FDStart: {printData.BundleStartTime:yyyy-MM-dd HH:mm:ss}^FS");

            // End Time
            zpl.AppendLine($"^FO220,230^A0N,20,20^FDEnd: {printData.BundleEndTime:yyyy-MM-dd HH:mm:ss}^FS");

            // Barcode (Code 128) for Bundle Number
            zpl.AppendLine($"^FO20,400^BY2^BCN,50,Y,N,N^FD{printData.BundleNo}^FS");

            // Print timestamp
            zpl.AppendLine($"^FO220,270^A0N,20,20^FDPrinted: {DateTime.Now:yyyy-MM-dd HH:mm:ss}^FS");

            // End ZPL
            zpl.AppendLine("^XZ"); // End of label format

            return zpl.ToString();
        }

        private bool PrintToFile(NDTBundlePrintData printData)
        {
            try
            {
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
                return $"Honeywell_PD45S_NDT (Network: {_printerAddress}:{_printerPort})";
            }
            else
            {
                return $"Honeywell_PD45S_NDT (Serial: {_printerAddress})";
            }
        }
    }
}

