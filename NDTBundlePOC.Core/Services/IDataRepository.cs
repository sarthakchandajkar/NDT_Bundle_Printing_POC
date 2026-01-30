using System.Collections.Generic;
using NDTBundlePOC.Core.Models;

namespace NDTBundlePOC.Core.Services
{
    public interface IDataRepository
    {
        // NDT Bundles
        List<NDTBundle> GetNDTBundles();
        NDTBundle GetNDTBundle(int bundleId);
        NDTBundle GetActiveNDTBundle(int poPlanId);
        void AddNDTBundle(NDTBundle bundle);
        void UpdateNDTBundle(NDTBundle bundle);
        
        // Formation Chart - Get by Size (for NDT bundles)
        NDTBundleFormationChart GetNDTFormationChart(int millId, decimal? pipeSize);
        
        // PO Plans
        POPlan GetPOPlan(int poPlanId);
        List<POPlan> GetPOPlans();
        void AddPOPlan(POPlan poPlan);
        void UpdatePOPlan(POPlan poPlan);
        void DeletePOPlan(int poPlanId);
        
        // Slits
        Slit GetActiveSlit(int poPlanId);
        void UpdateSlit(Slit slit);
        
        // Initialize with dummy data
        void InitializeDummyData();
    }
}

