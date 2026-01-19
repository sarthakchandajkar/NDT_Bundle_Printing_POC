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

                Console.WriteLine($"→ Attempting to connect to printer at {_printerAddress}:{_printerPort}...");
                Console.WriteLine($"→ ZPL command length: {zplBytes.Length} bytes");

                // Connect to printer via TCP/IP (port 9100 is standard for raw printing)
                tcpClient = new TcpClient();
                
                // Set connection timeout (5 seconds)
                var connectTask = tcpClient.ConnectAsync(_printerAddress, _printerPort);
                if (!connectTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    Console.WriteLine($"✗ Connection timeout: Could not connect to printer at {_printerAddress}:{_printerPort} within 5 seconds");
                    Console.WriteLine($"  → Check if printer is powered on and on the same network");
                    Console.WriteLine($"  → Verify IP address: {_printerAddress}");
                    Console.WriteLine($"  → Verify port: {_printerPort}");
                    return false;
                }
                
                if (!tcpClient.Connected)
                {
                    Console.WriteLine($"✗ Failed to connect to printer at {_printerAddress}:{_printerPort}");
                    Console.WriteLine($"  → Check network connectivity: ping {_printerAddress}");
                    return false;
                }

                Console.WriteLine($"✓ Connected to printer successfully");

                // Send ZPL commands
                stream = tcpClient.GetStream();
                stream.Write(zplBytes, 0, zplBytes.Length);
                stream.Flush();

                Console.WriteLine($"✓ ZPL commands sent to printer ({zplBytes.Length} bytes)");

                // Wait a bit for the print job to complete
                Thread.Sleep(1000);

                Console.WriteLine($"✓ NDT Bundle Tag printed via network ({_printerAddress}:{_printerPort}): {printData.BundleNo}");
                return true;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"✗ Network socket error: {ex.Message}");
                Console.WriteLine($"  → Error code: {ex.SocketErrorCode}");
                Console.WriteLine($"  → Check if printer at {_printerAddress}:{_printerPort} is reachable");
                Console.WriteLine($"  → Try: ping {_printerAddress}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Network printing error: {ex.Message}");
                Console.WriteLine($"  → Stack trace: {ex.StackTrace}");
                return false;
            }
            finally
            {
                try
                {
                    stream?.Close();
                    tcpClient?.Close();
                }
                catch { }
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
            // This matches the Rpt_MillLabel design layout
            var zpl = new StringBuilder();

            // Start ZPL
            zpl.AppendLine("^XA"); // Start of label format
            zpl.AppendLine("^CF0,0"); // Set default font

            // Set label size (100mm x 100mm = ~394 dots x 394 dots at 203 DPI)
            zpl.AppendLine("^LH0,0"); // Label home position
            zpl.AppendLine("^LL394"); // Label length (in dots, 203 DPI)

            // Header: "AJSPC - OMAN" (centered, bold)
            zpl.AppendLine("^FO197,0^A0N,22,22^FB394,1,0,C^FDAJSPC - OMAN^FS");

            // Specification section
            zpl.AppendLine("^FO0,71^A0N,18,18^FDSPECIFICATION^FS");
            zpl.AppendLine($"^FO111,71^A0N,22,22^FB283,1,0,C^FD{printData.Pipe_Grade ?? "API 5L X42"}^FS");

            // Type, Size, Length, Pcs/Bnd row
            zpl.AppendLine("^FO0,111^A0N,20,20^FDType^FS");
            zpl.AppendLine($"^FO0,139^A0N,18,18^FD{printData.Pipe_Grade ?? "ERW"}^FS");

            zpl.AppendLine("^FO111,111^A0N,20,20^FDSize^FS");
            zpl.AppendLine($"^FO111,139^A0N,18,18^FD{printData.Pipe_Size ?? "4''"}^FS");

            zpl.AppendLine("^FO189,111^A0N,20,20^FDLength^FS");
            zpl.AppendLine($"^FO189,139^A0N,18,18^FD{printData.Pipe_Len}'^FS");

            zpl.AppendLine("^FO307,111^A0N,20,20^FDPcs/Bnd^FS");
            zpl.AppendLine($"^FO307,139^A0N,18,18^FD{printData.NDT_Pcs}^FS");

            // SLIT NUMBER
            zpl.AppendLine("^FO0,173^A0N,18,18^FDSLIT NUMBER^FS");
            zpl.AppendLine("^FO0,209^A0N,18,18^FDSLIT-001^FS");

            // BUNDLE NUMBER
            zpl.AppendLine("^FO0,241^A0N,18,18^FDBUNDLE NUMBER^FS");
            zpl.AppendLine($"^FO0,277^A0N,18,18^FD{printData.BundleNo}^FS");

            // Barcode (Code 128) - horizontal at top
            zpl.AppendLine($"^FO0,0^BY2^BCN,36,Y,N,N^FD{printData.BundleNo}^FS");

            // Barcode (Code 128) - vertical (rotated 90 degrees) at right side
            zpl.AppendLine($"^FO307,209^BY1^BCR,84,Y,N,N^FD{printData.BundleNo}^FS");

            // "MADE IN OMAN" (centered at bottom)
            zpl.AppendLine("^FO0,335^A0N,24,24^FB394,1,0,C^FDMADE IN OMAN^FS");

            // End ZPL
            zpl.AppendLine("^XZ"); // End of label format

            string zplCommands = zpl.ToString();
            
            // Save ZPL to file for debugging
            try
            {
                string zplFileName = $"ZPL_{printData.BundleNo}_{DateTime.Now:yyyyMMddHHmmss}.zpl";
                string zplFilePath = Path.Combine(_outputPath, zplFileName);
                File.WriteAllText(zplFilePath, zplCommands);
                Console.WriteLine($"→ ZPL commands saved to: {zplFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Could not save ZPL file for debugging: {ex.Message}");
            }

            return zplCommands;
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

