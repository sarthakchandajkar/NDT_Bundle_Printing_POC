using System;
using System.Linq;
using NDTBundlePOC.Core.Models;
using NDTBundlePOC.Core.Services;

namespace NDTBundlePOC.UI
{
    public class ConsoleUI
    {
        private readonly INDTBundleService _bundleService;
        private readonly IPrinterService _printerService;
        private readonly ExcelExportService _excelService;

        public ConsoleUI(INDTBundleService bundleService, IPrinterService printerService, ExcelExportService excelService)
        {
            _bundleService = bundleService;
            _printerService = printerService;
            _excelService = excelService;
        }

        public void Run()
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("  NDT Bundle Tag Printing - POC");
            Console.WriteLine("========================================");
            Console.WriteLine();

            while (true)
            {
                ShowMenu();
                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        AddNDTCuts();
                        break;
                    case "2":
                        ShowBundles();
                        break;
                    case "3":
                        PrintBundle();
                        break;
                    case "4":
                        ShowBundles();
                        break;
                    case "5":
                        Console.WriteLine("Exiting...");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        Console.WriteLine();
                        break;
                }
            }
        }

        private void ShowMenu()
        {
            Console.WriteLine("Menu:");
            Console.WriteLine("  1. Add NDT Cuts");
            Console.WriteLine("  2. View Bundles Ready for Printing");
            Console.WriteLine("  3. Print Selected Bundle Tag");
            Console.WriteLine("  4. Refresh Bundle List");
            Console.WriteLine("  5. Exit");
            Console.Write("\nEnter your choice: ");
        }

        private void AddNDTCuts()
        {
            Console.Write("Enter number of NDT cuts to add: ");
            if (int.TryParse(Console.ReadLine(), out int cuts) && cuts > 0)
            {
                try
                {
                    _bundleService.ProcessNDTCuts(1, cuts);
                    Console.WriteLine($"✓ Added {cuts} NDT cuts. Bundle formation logic executed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("✗ Invalid input. Please enter a positive number.");
            }
            Console.WriteLine();
        }

        private void ShowBundles()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("  Bundles Ready for Printing");
            Console.WriteLine("========================================");
            
            var bundles = _bundleService.GetBundlesReadyForPrinting();
            
            if (!bundles.Any())
            {
                Console.WriteLine("No bundles ready for printing.");
            }
            else
            {
                Console.WriteLine($"{"ID",-5} {"Bundle No",-15} {"Batch No",-15} {"NDT Pcs",-10} {"PO Plan ID",-12} {"Status",-20}");
                Console.WriteLine(new string('-', 90));
                
                foreach (var bundle in bundles)
                {
                    string status = bundle.Status == 2 ? "Ready for Print" : bundle.Status == 3 ? "Printed" : "Active";
                    Console.WriteLine($"{bundle.NDTBundle_ID,-5} {bundle.Bundle_No,-15} {bundle.Batch_No,-15} {bundle.NDT_Pcs,-10} {bundle.PO_Plan_ID,-12} {status,-20}");
                }
            }
            
            Console.WriteLine("========================================\n");
        }

        private void PrintBundle()
        {
            ShowBundles();
            
            Console.Write("Enter Bundle ID to print (or 0 to cancel): ");
            if (int.TryParse(Console.ReadLine(), out int bundleId) && bundleId > 0)
            {
                try
                {
                    var printData = _bundleService.GetBundlePrintData(bundleId);
                    if (printData == null)
                    {
                        Console.WriteLine("✗ Could not retrieve bundle data.");
                        return;
                    }

                    Console.WriteLine($"\nPrinting bundle: {printData.BundleNo}");
                    
                    // Print tag
                    bool printed = _printerService.PrintNDTBundleTag(printData);
                    
                    // Export to Excel
                    _excelService.ExportNDTBundleToExcel(printData);

                    // Mark as printed
                    if (printed)
                    {
                        _bundleService.MarkBundleAsPrinted(bundleId);
                        Console.WriteLine($"✓ Bundle {printData.BundleNo} tag printed and exported to Excel.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error printing: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("✗ Invalid Bundle ID.");
            }
            Console.WriteLine();
        }
    }
}

