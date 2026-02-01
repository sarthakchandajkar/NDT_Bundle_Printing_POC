using System;
using System.Collections.Generic;
using NDTBundlePOC.Core.Models;

namespace NDTBundlePOC.Core.Services
{
    public interface INDTBundleService
    {
        // Process NDT cuts and form bundles
        void ProcessNDTCuts(int millId, int newNDTCuts);
        
        // Process NDT cuts from PLC (reads from PLC automatically)
        void ProcessNDTCutsFromPLC(int millId);
        
        // Get bundles ready for printing
        List<NDTBundle> GetBundlesReadyForPrinting();
        
        // Get all bundles (for UI display)
        List<NDTBundle> GetAllNDTBundles();
        
        // Mark bundle as printed
        void MarkBundleAsPrinted(int bundleId);
        
        // Get bundle details for printing
        NDTBundlePrintData GetBundlePrintData(int bundleId);
        
        // Print bundle tag (with PLC integration)
        bool PrintBundleTag(int bundleId, IPrinterService printerService, ExcelExportService excelService, IPLCService plcService = null);
        
        // Close partial bundles when PO is complete
        void ClosePartialBundlesForPO(int poPlanId);
        
        // Get total NDT pipes processed for a PO
        int GetTotalNDTPipesProcessed(int poPlanId);
    }
    
    public class NDTBundlePrintData
    {
        public string BundleNo { get; set; }
        public string BatchNo { get; set; }
        public int NDT_Pcs { get; set; }
        public string PO_No { get; set; }
        public string Pipe_Grade { get; set; }
        public string Pipe_Size { get; set; }
        public decimal Pipe_Len { get; set; }
        public DateTime BundleStartTime { get; set; }
        public DateTime BundleEndTime { get; set; }
        public bool IsReprint { get; set; }
    }
}

