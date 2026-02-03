using System;
using System.Collections.Generic;
using System.Linq;
using NDTBundlePOC.Core.Models;
using NDTBundlePOC.Core.Services;

namespace NDTBundlePOC.Core.Services
{
    /// <summary>
    /// OK Bundle Service - Handles OK pipe bundle formation and printing
    /// Similar to NDTBundleService but for OK pipes
    /// </summary>
    public interface IOKBundleService
    {
        void ProcessOKCuts(int millId, int newOKCuts);
        List<OKBundle> GetBundlesReadyForPrinting();
        List<OKBundle> GetAllOKBundles();
        void MarkBundleAsPrinted(int bundleId);
        OKBundlePrintData GetBundlePrintData(int bundleId);
        bool PrintBundleTag(int bundleId, IPrinterService printerService, ExcelExportService excelService, IPLCService plcService = null);
        
        // Close partial bundles when PO is complete
        void ClosePartialBundlesForPO(int poPlanId);
        
        // Get total OK pipes processed for a PO
        int GetTotalOKPipesProcessed(int poPlanId);
    }

    public class OKBundlePrintData
    {
        public string BundleNo { get; set; }
        public string BatchNo { get; set; }
        public int OK_Pcs { get; set; }
        public string PO_No { get; set; }
        public string Pipe_Grade { get; set; }
        public string Pipe_Size { get; set; }
        public decimal Pipe_Len { get; set; }
        public DateTime BundleStartTime { get; set; }
        public DateTime BundleEndTime { get; set; }
        public bool IsReprint { get; set; }
    }

    public class OKBundle
    {
        public int OKBundle_ID { get; set; }
        public int PO_Plan_ID { get; set; }
        public int? Slit_ID { get; set; }
        public string Bundle_No { get; set; }
        public int OK_Pcs { get; set; }
        public decimal Bundle_Wt { get; set; }
        public int Status { get; set; } // 1=Active, 2=Completed, 3=Printed
        public bool IsFullBundle { get; set; }
        public DateTime BundleStartTime { get; set; }
        public DateTime? BundleEndTime { get; set; }
        public DateTime? OprDoneTime { get; set; }
        public string Batch_No { get; set; }
    }

    public class OKBundleService : IOKBundleService
    {
        private readonly IDataRepository _repository;
        private readonly IPLCService _plcService;
        
        // In-memory storage for OK bundles (similar to NDT bundles)
        private List<OKBundle> _okBundles = new List<OKBundle>();
        private int _nextOKBundleId = 1;
        private readonly object _lock = new object();

        public OKBundleService(IDataRepository repository, IPLCService plcService = null)
        {
            _repository = repository;
            _plcService = plcService;
        }

        public void ProcessOKCuts(int millId, int newOKCuts)
        {
            if (newOKCuts <= 0) return;

            // Get active PO and Slit
            var activeSlit = _repository.GetActiveSlit(0);
            if (activeSlit == null)
            {
                Console.WriteLine("⚠️  ProcessOKCuts: No active slit found. Cannot process OK cuts.");
                return;
            }

            var poPlan = _repository.GetPOPlan(activeSlit.PO_Plan_ID);
            if (poPlan == null)
            {
                Console.WriteLine($"⚠️  ProcessOKCuts: PO Plan not found for Slit ID {activeSlit.Slit_ID}, PO_Plan_ID {activeSlit.PO_Plan_ID}");
                return;
            }
            
            Console.WriteLine($"✓ ProcessOKCuts: Found active slit {activeSlit.Slit_No} for PO {poPlan.PO_No}, processing {newOKCuts} OK cuts");

            // For OK bundles, use PcsPerBundle from PO_Plan table
            int requiredOKPcs = poPlan.PcsPerBundle;

            int remainingCuts = newOKCuts;

            // Process cuts and create bundles until all cuts are allocated (similar to NDT bundle logic)
            while (remainingCuts > 0)
            {
                // Get current active bundle
                var currentBundle = GetActiveOKBundle(poPlan.PO_Plan_ID);
                
                if (currentBundle != null)
                {
                    // Calculate how many cuts go to current bundle
                    int cutsForCurrentBundle = Math.Min(remainingCuts, requiredOKPcs - currentBundle.OK_Pcs);
                    
                    // Update existing bundle
                    currentBundle.OK_Pcs += cutsForCurrentBundle;
                    remainingCuts -= cutsForCurrentBundle;
                    UpdateOKBundle(currentBundle);

                    // Check if bundle is complete
                    // Scenario 1: Count >= PcsPerBundle (full bundle)
                    // Scenario 2: PO end (partial bundle) - check ButtEnd signal
                    bool isPOEnded = _plcService != null && _plcService.IsPOEnded(millId);
                    bool isBundleComplete = currentBundle.OK_Pcs >= requiredOKPcs || isPOEnded;
                    
                    if (isBundleComplete)
                    {
                        // Bundle is complete - mark as completed and ready for printing
                        currentBundle.Status = 2; // Completed
                        currentBundle.BundleEndTime = DateTime.Now;
                        currentBundle.IsFullBundle = currentBundle.OK_Pcs >= requiredOKPcs; // Full bundle only if count >= PcsPerBundle
                        UpdateOKBundle(currentBundle);

                        // Create new bundle for remaining cuts (if any) - but only if PO hasn't ended
                        if (remainingCuts > 0 && !isPOEnded)
                        {
                            string newBatchNo = GenerateOKBatchNumber(poPlan.PO_Plan_ID, currentBundle.Batch_No);
                            string newBundleNo = CreateNewOKBundle(poPlan.PO_Plan_ID, activeSlit.Slit_ID, newBatchNo);
                            Console.WriteLine($"📦 Created new OK bundle: {newBundleNo} for remaining {remainingCuts} cuts");
                        }
                        
                        string bundleType = currentBundle.IsFullBundle ? "Full" : "Partial (PO ended)";
                        Console.WriteLine($"📦 OK BUNDLE COMPLETED: {currentBundle.Bundle_No} | Pieces: {currentBundle.OK_Pcs} | Type: {bundleType} | Status: Ready for printing");
                    }
                    else
                    {
                        // Bundle not complete yet - continue processing
                        // No more cuts to process
                        break;
                    }
                }
                else
                {
                    // No active bundle, create new one
                    string batchNo = GenerateOKBatchNumber(poPlan.PO_Plan_ID, "");
                    string newBundleNo = CreateNewOKBundle(poPlan.PO_Plan_ID, activeSlit.Slit_ID, batchNo);

                    // Get the newly created bundle and add cuts to it
                    var newBundle = GetOKBundles()
                        .FirstOrDefault(b => b.Bundle_No == newBundleNo);
                    if (newBundle != null)
                    {
                        int cutsForNewBundle = Math.Min(remainingCuts, requiredOKPcs);
                        newBundle.OK_Pcs = cutsForNewBundle;
                        remainingCuts -= cutsForNewBundle;
                        UpdateOKBundle(newBundle);
                        
                        // Check if bundle is complete immediately
                        bool isPOEnded = _plcService != null && _plcService.IsPOEnded(millId);
                        bool isBundleComplete = newBundle.OK_Pcs >= requiredOKPcs || isPOEnded;
                        
                        if (isBundleComplete)
                        {
                            newBundle.Status = 2; // Completed
                            newBundle.BundleEndTime = DateTime.Now;
                            newBundle.IsFullBundle = newBundle.OK_Pcs >= requiredOKPcs;
                            UpdateOKBundle(newBundle);
                            
                            string bundleType = newBundle.IsFullBundle ? "Full" : "Partial (PO ended)";
                            Console.WriteLine($"📦 OK BUNDLE COMPLETED: {newBundle.Bundle_No} | Pieces: {newBundle.OK_Pcs} | Type: {bundleType} | Status: Ready for printing");
                        }
                    }
                    else
                    {
                        break; // Could not create bundle, exit loop
                    }
                }
            }
        }

        private OKBundle GetActiveOKBundle(int poPlanId)
        {
            // This would query the database in production
            // For POC, return from in-memory collection
            return GetOKBundles()
                .FirstOrDefault(b => b.PO_Plan_ID == poPlanId && b.Status == 1);
        }

        private List<OKBundle> GetOKBundles()
        {
            lock (_lock)
            {
                return _okBundles.ToList();
            }
        }

        private void UpdateOKBundle(OKBundle bundle)
        {
            lock (_lock)
            {
                var existing = _okBundles.FirstOrDefault(b => b.OKBundle_ID == bundle.OKBundle_ID);
                if (existing != null)
                {
                    var index = _okBundles.IndexOf(existing);
                    _okBundles[index] = bundle;
                }
                else
                {
                    // If bundle doesn't exist, add it (for newly created bundles)
                    if (bundle.OKBundle_ID == 0)
                    {
                        bundle.OKBundle_ID = _nextOKBundleId++;
                    }
                    _okBundles.Add(bundle);
                }
            }
        }

        private string GenerateOKBatchNumber(int poPlanId, string previousBatchNo)
        {
            if (string.IsNullOrEmpty(previousBatchNo))
            {
                var poPlan = _repository.GetPOPlan(poPlanId);
                return "OK_" + (poPlan?.PO_No ?? "001") + "001";
            }
            else
            {
                if (previousBatchNo.Contains("_"))
                {
                    string[] parts = previousBatchNo.Split('_');
                    if (parts.Length >= 2)
                    {
                        string numberPart = parts[parts.Length - 1];
                        if (int.TryParse(numberPart, out int batchNum))
                        {
                            return parts[0] + "_" + (batchNum + 1).ToString("D3");
                        }
                    }
                }
                return previousBatchNo + "_001";
            }
        }

        private string CreateNewOKBundle(int poPlanId, int? slitId, string batchNo)
        {
            var poPlan = _repository.GetPOPlan(poPlanId);
            if (poPlan == null) return "";

            // Generate bundle number based on existing bundles for this PO
            var existingBundles = GetOKBundles().Where(b => b.PO_Plan_ID == poPlanId).ToList();
            int bundleNumber = 1;
            
            if (existingBundles.Any())
            {
                var lastBundle = existingBundles.OrderByDescending(b => b.BundleStartTime).First();
                // Extract number from bundle number format: PO_NoOK001
                if (lastBundle.Bundle_No != null && lastBundle.Bundle_No.Contains("OK"))
                {
                    string numPart = lastBundle.Bundle_No.Replace(poPlan.PO_No + "OK", "");
                    if (int.TryParse(numPart, out int lastNum))
                    {
                        bundleNumber = lastNum + 1;
                    }
                }
            }
            
            string bundleNo = $"{poPlan.PO_No}OK{bundleNumber:D3}";

            // Create and store the new bundle
            OKBundle newBundle;
            lock (_lock)
            {
                newBundle = new OKBundle
                {
                    OKBundle_ID = _nextOKBundleId++,
                    PO_Plan_ID = poPlanId,
                    Slit_ID = slitId,
                    Bundle_No = bundleNo,
                    OK_Pcs = 0,
                    Status = 1, // Active
                    IsFullBundle = false,
                    BundleStartTime = DateTime.Now,
                    Batch_No = batchNo
                };
                _okBundles.Add(newBundle);
            }

            return bundleNo;
        }

        public List<OKBundle> GetBundlesReadyForPrinting()
        {
            return GetOKBundles()
                .Where(b => b.Status == 2)
                .ToList();
        }

        public List<OKBundle> GetAllOKBundles()
        {
            return GetOKBundles()
                .OrderByDescending(b => b.BundleStartTime)
                .ToList();
        }

        /// <summary>
        /// Close all partial bundles (Status = 1) for a given PO when PO is complete
        /// </summary>
        public void ClosePartialBundlesForPO(int poPlanId)
        {
            try
            {
                var activeBundles = GetOKBundles()
                    .Where(b => b.PO_Plan_ID == poPlanId && b.Status == 1)
                    .ToList();

                foreach (var bundle in activeBundles)
                {
                    bundle.Status = 2; // Completed
                    bundle.BundleEndTime = DateTime.Now;
                    bundle.IsFullBundle = false; // Partial bundle
                    UpdateOKBundle(bundle);
                    Console.WriteLine($"📦 OK BUNDLE COMPLETED (PO End): {bundle.Bundle_No} | Pieces: {bundle.OK_Pcs} | Type: Partial (PO Complete) | Status: Ready for printing");
                }

                if (activeBundles.Count > 0)
                {
                    Console.WriteLine($"✅ Closed {activeBundles.Count} partial OK bundle(s) for PO Plan ID {poPlanId} (PO complete - all pipes processed)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error closing partial OK bundles for PO {poPlanId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get total OK pipes processed for a PO (sum of all bundles)
        /// </summary>
        public int GetTotalOKPipesProcessed(int poPlanId)
        {
            try
            {
                return GetOKBundles()
                    .Where(b => b.PO_Plan_ID == poPlanId)
                    .Sum(b => b.OK_Pcs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error getting total OK pipes processed for PO {poPlanId}: {ex.Message}");
                return 0;
            }
        }

        public void MarkBundleAsPrinted(int bundleId)
        {
            var bundle = GetOKBundles().FirstOrDefault(b => b.OKBundle_ID == bundleId);
            if (bundle != null)
            {
                bundle.Status = 3;
                bundle.OprDoneTime = DateTime.Now;
                UpdateOKBundle(bundle);
            }
        }

        public OKBundlePrintData GetBundlePrintData(int bundleId)
        {
            var bundle = GetOKBundles().FirstOrDefault(b => b.OKBundle_ID == bundleId);
            if (bundle == null) return null;

            var poPlan = _repository.GetPOPlan(bundle.PO_Plan_ID);
            if (poPlan == null) return null;

            return new OKBundlePrintData
            {
                BundleNo = bundle.Bundle_No,
                BatchNo = bundle.Batch_No,
                OK_Pcs = bundle.OK_Pcs,
                PO_No = poPlan.PO_No,
                Pipe_Grade = poPlan.Pipe_Type, // Map Pipe_Type to Pipe_Grade for backward compatibility
                Pipe_Size = poPlan.Pipe_Size,
                Pipe_Len = poPlan.Pipe_Len,
                BundleStartTime = bundle.BundleStartTime,
                BundleEndTime = bundle.BundleEndTime ?? DateTime.Now
            };
        }

        public bool PrintBundleTag(int bundleId, IPrinterService printerService, ExcelExportService excelService, IPLCService plcService = null)
        {
            try
            {
                var printData = GetBundlePrintData(bundleId);
                if (printData == null)
                {
                    Console.WriteLine("✗ OK Bundle not found for printing");
                    return false;
                }

                // Convert OK bundle data to NDT format for printing (reuse printer service)
                var ndtPrintData = new NDTBundlePrintData
                {
                    BundleNo = printData.BundleNo,
                    BatchNo = printData.BatchNo,
                    NDT_Pcs = printData.OK_Pcs,
                    PO_No = printData.PO_No,
                    Pipe_Grade = printData.Pipe_Grade,
                    Pipe_Size = printData.Pipe_Size,
                    Pipe_Len = printData.Pipe_Len,
                    BundleStartTime = printData.BundleStartTime,
                    BundleEndTime = printData.BundleEndTime,
                    IsReprint = printData.IsReprint
                };

                // Write bundle info to PLC if connected
                if (plcService != null && plcService.IsConnected)
                {
                    int millId = 1;
                    plcService.WriteBundleInfo(millId, printData.BundleNo, printData.BatchNo, printData.OK_Pcs);
                }

                // Print tag
                bool printed = printerService.PrintNDTBundleTag(ndtPrintData);

                // Export to Excel
                excelService.ExportNDTBundleToExcel(ndtPrintData);

                // Mark as printed
                if (printed)
                {
                    MarkBundleAsPrinted(bundleId);
                    Console.WriteLine($"✓ OK Bundle {printData.BundleNo} tag printed and exported to Excel");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error printing OK bundle tag: {ex.Message}");
                return false;
            }
        }
    }
}

