using System;
using System.Threading.Tasks;

namespace NDTBundlePOC.Core.Services
{
    public interface IPLCService
    {
        bool IsConnected { get; }
        
        // Connect to PLC
        bool Connect(string ipAddress, int rack = 0, int slot = 1);
        
        // Disconnect from PLC
        void Disconnect();
        
        // Read NDT cuts count from PLC
        int ReadNDTCuts(int millId);
        
        // Write bundle information to PLC
        bool WriteBundleInfo(int millId, string bundleNo, string batchNo, int pcs);
        
        // Read PO Plan ID from PLC
        int ReadPOPlanID(int millId);
        
        // Read Slit ID from PLC
        int ReadSlitID(int millId);
        
        // Check if PO has ended
        bool IsPOEnded(int millId);
    }
}

