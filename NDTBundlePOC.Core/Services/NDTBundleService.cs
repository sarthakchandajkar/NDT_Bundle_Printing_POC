using System;
using System.Collections.Generic;
using System.Linq;
using NDTBundlePOC.Core.Models;

namespace NDTBundlePOC.Core.Services
{
    public class NDTBundleService : INDTBundleService
    {
        private readonly IDataRepository _repository;
        private readonly IPLCService _plcService;
        
        // Track current PO_Plan_ID to detect PO changes
        private int? _currentPOPlanId = null;

        public NDTBundleService(IDataRepository repository, IPLCService plcService = null)
        {
            _repository = repository;
            _plcService = plcService;
        }

        public void ProcessNDTCuts(int millId, int newNDTCuts)
        {
            if (newNDTCuts <= 0) return;

            // Get active PO and Slit
            var activeSlit = _repository.GetActiveSlit(0); // Get any active slit for POC
            if (activeSlit == null) return;

            var poPlan = _repository.GetPOPlan(activeSlit.PO_Plan_ID);
            if (poPlan == null) return;

            // Get NDT Pcs per bundle from chart based on Pipe_Size
            // Parse Pipe_Size from PO_Plan (e.g., "2.5", "3.0", etc.)
            decimal? pipeSize = null;
            if (!string.IsNullOrEmpty(poPlan.Pipe_Size) && decimal.TryParse(poPlan.Pipe_Size, out decimal parsedSize))
            {
                pipeSize = parsedSize;
            }
            
            var formationChart = _repository.GetNDTFormationChart(millId, pipeSize);
            int requiredNDTPcs = formationChart?.NDT_PcsPerBundle ?? 20; // Default to 20 pipes per bundle

            // Check if PO has changed (Scenario 2: PO end/PO ID end)
            bool isPOChanged = _currentPOPlanId.HasValue && _currentPOPlanId.Value != poPlan.PO_Plan_ID;
            bool isPOEnded = _plcService != null && _plcService.IsPOEnded(millId);
            
            // If PO changed or ended, close current batch and start new one
            if (isPOChanged || isPOEnded)
            {
                var activeBundle = _repository.GetActiveNDTBundle(_currentPOPlanId ?? poPlan.PO_Plan_ID);
                if (activeBundle != null && activeBundle.Status == 1)
                {
                    // Close partial bundle due to PO end/change
                    activeBundle.Status = 2; // Completed
                    activeBundle.BundleEndTime = DateTime.Now;
                    activeBundle.IsFullBundle = false; // Partial bundle
                    _repository.UpdateNDTBundle(activeBundle);
                    Console.WriteLine($"✓ NDT Bundle {activeBundle.Bundle_No} closed (partial) due to PO end/change. Ready for printing.");
                }
            }
            
            // Update current PO tracking
            _currentPOPlanId = poPlan.PO_Plan_ID;

            int remainingCuts = newNDTCuts;

            // Process cuts and create bundles until all cuts are allocated
            while (remainingCuts > 0)
            {
                // Get current active bundle
                var currentBundle = _repository.GetActiveNDTBundle(poPlan.PO_Plan_ID);
                
                // Calculate sum of NDT Pcs for current batch (all bundles with same Batch_No)
                int currentBatchSum = 0;
                if (currentBundle != null && !string.IsNullOrEmpty(currentBundle.Batch_No))
                {
                    currentBatchSum = _repository.GetNDTBundles()
                        .Where(b => b.Batch_No == currentBundle.Batch_No && b.Status != 3)
                        .Sum(b => b.NDT_Pcs);
                }
                
                if (currentBundle != null)
                {
                    // Calculate how many cuts go to current bundle
                    int cutsForCurrentBundle = Math.Min(remainingCuts, requiredNDTPcs - currentBundle.NDT_Pcs);
                    
                    // Update existing bundle
                    currentBundle.NDT_Pcs += cutsForCurrentBundle;
                    remainingCuts -= cutsForCurrentBundle;
                    _repository.UpdateNDTBundle(currentBundle);
                    
                    // Recalculate batch sum after update
                    if (!string.IsNullOrEmpty(currentBundle.Batch_No))
                    {
                        currentBatchSum = _repository.GetNDTBundles()
                            .Where(b => b.Batch_No == currentBundle.Batch_No && b.Status != 3)
                            .Sum(b => b.NDT_Pcs);
                    }

                    // Check if bundle is complete
                    // Scenario 1: Bundle reaches required PcsPerBundle
                    bool isBundleComplete = currentBundle.NDT_Pcs >= requiredNDTPcs;
                    
                    // Scenario 1: If sum of NDT Pcs of the Mill >= required Pcs, end batch and create new batch in series
                    bool shouldEndBatch = currentBatchSum >= requiredNDTPcs;
                    
                    // Scenario 2: If PO ends and sum < required Pcs, end batch by PO end
                    bool shouldEndBatchByPO = isPOEnded && currentBatchSum < requiredNDTPcs;
                    
                    if (isBundleComplete)
                    {
                        // Bundle is complete - mark as completed and ready for printing
                        currentBundle.Status = 2; // Completed
                        currentBundle.BundleEndTime = DateTime.Now;
                        currentBundle.IsFullBundle = true;
                        _repository.UpdateNDTBundle(currentBundle);
                        
                        // Scenario 1: If batch sum >= required Pcs, end batch and create new batch in series
                        if (shouldEndBatch && remainingCuts > 0)
                        {
                            string newBatchNo = GenerateNDTBatchNumber(poPlan.PO_Plan_ID, currentBundle.Batch_No);
                            string newBundleNo = CreateNewNDTBundle(poPlan.PO_Plan_ID, activeSlit.Slit_ID, newBatchNo);
                            Console.WriteLine($"✓ Created new NDT bundle {newBundleNo} with new batch {newBatchNo} (batch sum {currentBatchSum} >= {requiredNDTPcs})");
                        }
                        // Create new bundle in same batch if batch not complete yet
                        else if (!shouldEndBatch && remainingCuts > 0)
                        {
                            string newBundleNo = CreateNewNDTBundle(poPlan.PO_Plan_ID, activeSlit.Slit_ID, currentBundle.Batch_No);
                            Console.WriteLine($"✓ Created new NDT bundle {newBundleNo} in same batch {currentBundle.Batch_No} for remaining {remainingCuts} cuts");
                        }
                        
                        Console.WriteLine($"✓ NDT Bundle {currentBundle.Bundle_No} completed with {currentBundle.NDT_Pcs} pipes. Ready for printing.");
                    }
                    else if (shouldEndBatchByPO)
                    {
                        // Scenario 2: PO ended and sum < required Pcs - end batch by PO end
                        currentBundle.Status = 2; // Completed
                        currentBundle.BundleEndTime = DateTime.Now;
                        currentBundle.IsFullBundle = false; // Partial bundle
                        _repository.UpdateNDTBundle(currentBundle);
                        Console.WriteLine($"✓ NDT Bundle {currentBundle.Bundle_No} closed (partial, PO ended) with {currentBundle.NDT_Pcs} pipes. Batch sum: {currentBatchSum} < {requiredNDTPcs}. Ready for printing.");
                        
                        // Next PO will get new batch number (handled at start of ProcessNDTCuts)
                    }
                    else
                    {
                        // Bundle not complete yet - continue processing
                        break;
                    }
                }
                else
                {
                    // No active bundle, create new one
                    // Check if we need a new batch (PO changed or previous batch ended)
                    string batchNo;
                    if (isPOChanged || isPOEnded)
                    {
                        // Scenario 2: New PO gets new batch number
                        batchNo = GenerateNDTBatchNumber(poPlan.PO_Plan_ID, "");
                    }
                    else
                    {
                        // Check if there's a previous batch for this PO
                        var lastBundle = _repository.GetNDTBundles()
                            .Where(b => b.PO_Plan_ID == poPlan.PO_Plan_ID)
                            .OrderByDescending(b => b.BundleStartTime)
                            .FirstOrDefault();
                        
                        if (lastBundle != null && !string.IsNullOrEmpty(lastBundle.Batch_No))
                        {
                            // Check if previous batch sum >= required Pcs
                            int lastBatchSum = _repository.GetNDTBundles()
                                .Where(b => b.Batch_No == lastBundle.Batch_No && b.Status != 3)
                                .Sum(b => b.NDT_Pcs);
                            
                            if (lastBatchSum >= requiredNDTPcs)
                            {
                                // Scenario 1: Previous batch completed, create new batch in series
                                batchNo = GenerateNDTBatchNumber(poPlan.PO_Plan_ID, lastBundle.Batch_No);
                            }
                            else
                            {
                                // Continue with same batch
                                batchNo = lastBundle.Batch_No;
                            }
                        }
                        else
                        {
                            // First bundle for this PO
                            batchNo = GenerateNDTBatchNumber(poPlan.PO_Plan_ID, "");
                        }
                    }
                    
                    string newBundleNo = CreateNewNDTBundle(poPlan.PO_Plan_ID, activeSlit.Slit_ID, batchNo);

                    // Get the newly created bundle and add cuts to it
                    var newBundle = _repository.GetNDTBundles()
                        .FirstOrDefault(b => b.Bundle_No == newBundleNo);
                    if (newBundle != null)
                    {
                        int cutsForNewBundle = Math.Min(remainingCuts, requiredNDTPcs);
                        newBundle.NDT_Pcs = cutsForNewBundle;
                        remainingCuts -= cutsForNewBundle;
                        _repository.UpdateNDTBundle(newBundle);
                        
                        // If bundle is complete immediately, mark it
                        if (newBundle.NDT_Pcs >= requiredNDTPcs)
                        {
                            newBundle.Status = 2; // Completed
                            newBundle.BundleEndTime = DateTime.Now;
                            newBundle.IsFullBundle = true;
                            _repository.UpdateNDTBundle(newBundle);
                            
                            Console.WriteLine($"✓ NDT Bundle {newBundle.Bundle_No} completed with {newBundle.NDT_Pcs} pipes. Ready for printing.");
                        }
                    }
                    else
                    {
                        break; // Could not create bundle, exit loop
                    }
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
            try
            {
                // Only return full bundles (IsFullBundle = true) for printing
                // Partial bundles are only printed when PO ends (they are explicitly marked as ready)
                // For Case 1: 30 NDT pipes with PcsPerBundle=13 should create:
                //   - 2 full bundles (13+13) → Status=2, IsFullBundle=true → PRINT
                //   - 1 partial bundle (4) → Status=1, IsFullBundle=false → DON'T PRINT (until PO ends)
                return _repository.GetNDTBundles()
                    .Where(b => b.Status == 2 && b.IsFullBundle == true) // Only full bundles ready for printing
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error in GetBundlesReadyForPrinting: {ex.Message}");
                // Return empty list to prevent polling service from crashing
                return new List<NDTBundle>();
            }
        }

        public List<NDTBundle> GetAllNDTBundles()
        {
            try
            {
                return _repository.GetNDTBundles()
                    .OrderByDescending(b => b.BundleStartTime)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error in GetAllNDTBundles: {ex.Message}");
                // Return empty list to prevent UI from crashing
                return new List<NDTBundle>();
            }
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
                Pipe_Grade = poPlan.Pipe_Type, // Map Pipe_Type to Pipe_Grade for backward compatibility
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

