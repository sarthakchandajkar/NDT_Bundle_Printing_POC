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
        void MarkBundleAsPrinted(int bundleId);
        OKBundlePrintData GetBundlePrintData(int bundleId);
        bool PrintBundleTag(int bundleId, IPrinterService printerService, ExcelExportService excelService, IPLCService plcService = null);
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
        private const int DEFAULT_OK_PCS_PER_BUNDLE = 10; // Default OK pieces per bundle

        public OKBundleService(IDataRepository repository)
        {
            _repository = repository;
        }

        public void ProcessOKCuts(int millId, int newOKCuts)
        {
            if (newOKCuts <= 0) return;

            // Get active PO and Slit
            var activeSlit = _repository.GetActiveSlit(0);
            if (activeSlit == null) return;

            var poPlan = _repository.GetPOPlan(activeSlit.PO_Plan_ID);
            if (poPlan == null) return;

            // For OK bundles, use a default pieces per bundle (can be configured)
            int requiredOKPcs = DEFAULT_OK_PCS_PER_BUNDLE;

            // Get or create active OK bundle
            var activeBundle = GetActiveOKBundle(poPlan.PO_Plan_ID);
            bool needNewBundle = false;
            string bundleNo = "";

            int remainingCuts = newOKCuts;

            if (activeBundle != null)
            {
                // Calculate how many cuts go to current bundle
                int cutsForCurrentBundle = Math.Min(remainingCuts, requiredOKPcs - activeBundle.OK_Pcs);

                // Update existing bundle
                activeBundle.OK_Pcs += cutsForCurrentBundle;
                remainingCuts -= cutsForCurrentBundle;
                UpdateOKBundle(activeBundle);

                // Check if bundle is complete
                if (activeBundle.OK_Pcs >= requiredOKPcs)
                {
                    activeBundle.Status = 2; // Completed
                    activeBundle.BundleEndTime = DateTime.Now;
                    activeBundle.IsFullBundle = true;
                    UpdateOKBundle(activeBundle);

                    needNewBundle = true;
                    string newBatchNo = GenerateOKBatchNumber(poPlan.PO_Plan_ID, activeBundle.Batch_No);
                    bundleNo = CreateNewOKBundle(poPlan.PO_Plan_ID, activeSlit.Slit_ID, newBatchNo);
                }
            }
            else
            {
                // No active bundle, create new one
                needNewBundle = true;
            }

            if (needNewBundle && string.IsNullOrEmpty(bundleNo))
            {
                string batchNo = GenerateOKBatchNumber(poPlan.PO_Plan_ID, "");
                bundleNo = CreateNewOKBundle(poPlan.PO_Plan_ID, activeSlit.Slit_ID, batchNo);
            }

            // If we have a new bundle and remaining cuts, add them to the new bundle
            if (needNewBundle && remainingCuts > 0 && !string.IsNullOrEmpty(bundleNo))
            {
                var newBundle = GetOKBundles()
                    .FirstOrDefault(b => b.Bundle_No == bundleNo);
                if (newBundle != null)
                {
                    newBundle.OK_Pcs = remainingCuts;
                    UpdateOKBundle(newBundle);
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
            // In production, this would query the database
            // For POC, return empty list (OK bundles would be stored in database)
            return new List<OKBundle>();
        }

        private void UpdateOKBundle(OKBundle bundle)
        {
            // In production, this would update the database
            // For POC, this is a placeholder
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

            // Generate bundle number
            string bundleNo = poPlan.PO_No + "OK001"; // Simplified for POC

            // In production, this would insert into database
            // For POC, this is a placeholder
            return bundleNo;
        }

        public List<OKBundle> GetBundlesReadyForPrinting()
        {
            return GetOKBundles()
                .Where(b => b.Status == 2)
                .ToList();
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
                Pipe_Grade = poPlan.Pipe_Grade,
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

