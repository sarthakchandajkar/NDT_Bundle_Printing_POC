using System;
using System.Collections.Generic;
using System.Linq;
using NDTBundlePOC.Core.Models;

namespace NDTBundlePOC.Core.Services
{
    public class NDTBundleService : INDTBundleService
    {
        private readonly IDataRepository _repository;

        public NDTBundleService(IDataRepository repository)
        {
            _repository = repository;
        }

        public void ProcessNDTCuts(int millId, int newNDTCuts)
        {
            if (newNDTCuts <= 0) return;

            // Get active PO and Slit
            var activeSlit = _repository.GetActiveSlit(0); // Get any active slit for POC
            if (activeSlit == null) return;

            var poPlan = _repository.GetPOPlan(activeSlit.PO_Plan_ID);
            if (poPlan == null) return;

            // Get NDT Pcs per bundle from chart
            var formationChart = _repository.GetNDTFormationChart(millId, poPlan.PO_Plan_ID);
            int requiredNDTPcs = formationChart?.NDT_PcsPerBundle ?? 10;

            // Get or create active bundle
            var activeBundle = _repository.GetActiveNDTBundle(poPlan.PO_Plan_ID);
            bool needNewBundle = false;
            string bundleNo = "";

            int remainingCuts = newNDTCuts;

            if (activeBundle != null)
            {
                // Calculate how many cuts go to current bundle
                int cutsForCurrentBundle = Math.Min(remainingCuts, requiredNDTPcs - activeBundle.NDT_Pcs);
                
                // Update existing bundle
                activeBundle.NDT_Pcs += cutsForCurrentBundle;
                remainingCuts -= cutsForCurrentBundle;
                _repository.UpdateNDTBundle(activeBundle);

                // Check if bundle is complete (Scenario 1)
                if (activeBundle.NDT_Pcs >= requiredNDTPcs)
                {
                    // Scenario 1: Sum >= Required -> End batch, create new batch in series
                    activeBundle.Status = 2; // Completed
                    activeBundle.BundleEndTime = DateTime.Now;
                    activeBundle.IsFullBundle = true;
                    _repository.UpdateNDTBundle(activeBundle);

                    needNewBundle = true;
                    string newBatchNo = GenerateNDTBatchNumber(poPlan.PO_Plan_ID, activeBundle.Batch_No);
                    bundleNo = CreateNewNDTBundle(poPlan.PO_Plan_ID, activeSlit.Slit_ID, newBatchNo);
                }
                else
                {
                    // Scenario 2: Check if PO ended
                    if (poPlan.Status >= 3) // PO completed
                    {
                        activeBundle.Status = 2;
                        activeBundle.BundleEndTime = DateTime.Now;
                        activeBundle.IsFullBundle = false;
                        _repository.UpdateNDTBundle(activeBundle);
                        needNewBundle = true;
                    }
                }
            }
            else
            {
                // No active bundle, create new one
                needNewBundle = true;
            }

            if (needNewBundle && string.IsNullOrEmpty(bundleNo))
            {
                string batchNo = GenerateNDTBatchNumber(poPlan.PO_Plan_ID, "");
                bundleNo = CreateNewNDTBundle(poPlan.PO_Plan_ID, activeSlit.Slit_ID, batchNo);
            }

            // If we have a new bundle and remaining cuts, add them to the new bundle
            if (needNewBundle && remainingCuts > 0 && !string.IsNullOrEmpty(bundleNo))
            {
                var newBundle = _repository.GetNDTBundles()
                    .FirstOrDefault(b => b.Bundle_No == bundleNo);
                if (newBundle != null)
                {
                    newBundle.NDT_Pcs = remainingCuts;
                    _repository.UpdateNDTBundle(newBundle);
                }
            }

            // Update Slit NDT count
            activeSlit.Slit_NDT += newNDTCuts;
            _repository.UpdateSlit(activeSlit);
        }

        private string GenerateNDTBatchNumber(int poPlanId, string previousBatchNo)
        {
            if (string.IsNullOrEmpty(previousBatchNo))
            {
                return $"NDT_{DateTime.Now:yy}1{1:D4}"; // NDT_YY10001
            }
            else
            {
                string yearPrefix = previousBatchNo.Substring(4, 2);
                int batchNum = int.Parse(previousBatchNo.Substring(7));
                
                if (yearPrefix == DateTime.Now.ToString("yy"))
                {
                    batchNum++;
                    return $"NDT_{yearPrefix}1{batchNum:D4}";
                }
                else
                {
                    return $"NDT_{DateTime.Now:yy}1{1:D4}";
                }
            }
        }

        private string CreateNewNDTBundle(int poPlanId, int slitId, string batchNo)
        {
            var bundles = _repository.GetNDTBundles();
            string bundleNo;
            
            if (bundles.Any())
            {
                var lastBundle = bundles.OrderByDescending(b => b.BundleStartTime).First();
                string yearPrefix = DateTime.Now.ToString("yy");
                if (lastBundle.Bundle_No != null && lastBundle.Bundle_No.StartsWith(yearPrefix))
                {
                    // Format: YY1NDT0001, extract number from position 6
                    if (lastBundle.Bundle_No.Length >= 10)
                    {
                        string numPart = lastBundle.Bundle_No.Substring(6);
                        if (int.TryParse(numPart, out int bundleNum))
                        {
                            bundleNum++;
                            bundleNo = $"{yearPrefix}1NDT{bundleNum:D4}";
                        }
                        else
                        {
                            bundleNo = $"{yearPrefix}1NDT{1:D4}";
                        }
                    }
                    else
                    {
                        bundleNo = $"{yearPrefix}1NDT{1:D4}";
                    }
                }
                else
                {
                    bundleNo = $"{yearPrefix}1NDT{1:D4}";
                }
            }
            else
            {
                bundleNo = $"{DateTime.Now:yy}1NDT{1:D4}";
            }

            var newBundle = new NDTBundle
            {
                PO_Plan_ID = poPlanId,
                Slit_ID = slitId,
                Bundle_No = bundleNo,
                NDT_Pcs = 0,
                Status = 1, // Active
                IsFullBundle = false,
                BundleStartTime = DateTime.Now,
                Batch_No = batchNo
            };

            _repository.AddNDTBundle(newBundle);
            return bundleNo;
        }

        public List<NDTBundle> GetBundlesReadyForPrinting()
        {
            return _repository.GetNDTBundles()
                .Where(b => b.Status == 2) // Completed but not printed
                .ToList();
        }

        public List<NDTBundle> GetAllNDTBundles()
        {
            return _repository.GetNDTBundles()
                .OrderByDescending(b => b.BundleStartTime)
                .ToList();
        }

        public void MarkBundleAsPrinted(int bundleId)
        {
            var bundle = _repository.GetNDTBundle(bundleId);
            if (bundle != null)
            {
                bundle.Status = 3; // Printed
                bundle.OprDoneTime = DateTime.Now;
                _repository.UpdateNDTBundle(bundle);
            }
        }

        public NDTBundlePrintData GetBundlePrintData(int bundleId)
        {
            var bundle = _repository.GetNDTBundle(bundleId);
            if (bundle == null) return null;

            var poPlan = _repository.GetPOPlan(bundle.PO_Plan_ID);
            if (poPlan == null) return null;

            return new NDTBundlePrintData
            {
                BundleNo = bundle.Bundle_No,
                BatchNo = bundle.Batch_No,
                NDT_Pcs = bundle.NDT_Pcs,
                PO_No = poPlan.PO_No,
                Pipe_Grade = poPlan.Pipe_Grade,
                Pipe_Size = poPlan.Pipe_Size,
                Pipe_Len = poPlan.Pipe_Len,
                BundleStartTime = bundle.BundleStartTime,
                BundleEndTime = bundle.BundleEndTime ?? DateTime.Now
            };
        }

        public void ProcessNDTCutsFromPLC(int millId)
        {
            // This method will be called by a background service that polls the PLC
            // For now, it's a placeholder - actual implementation would read from PLC
            // and call ProcessNDTCuts with the value
        }

        public bool PrintBundleTag(int bundleId, IPrinterService printerService, ExcelExportService excelService, IPLCService plcService = null)
        {
            try
            {
                var printData = GetBundlePrintData(bundleId);
                if (printData == null)
                {
                    Console.WriteLine("✗ Bundle not found for printing");
                    return false;
                }

                // Write bundle info to PLC if connected
                if (plcService != null && plcService.IsConnected)
                {
                    int millId = 1; // Default mill ID
                    plcService.WriteBundleInfo(millId, printData.BundleNo, printData.BatchNo, printData.NDT_Pcs);
                }

                // Print tag
                bool printed = printerService.PrintNDTBundleTag(printData);
                
                // Export to Excel
                excelService.ExportNDTBundleToExcel(printData);

                // Mark as printed
                if (printed)
                {
                    MarkBundleAsPrinted(bundleId);
                    Console.WriteLine($"✓ Bundle {printData.BundleNo} tag printed and exported to Excel");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error printing bundle tag: {ex.Message}");
                return false;
            }
        }
    }
}

