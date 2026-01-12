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
        
        // Formation Chart
        NDTBundleFormationChart GetNDTFormationChart(int millId, int? poPlanId);
        
        // PO Plans
        POPlan GetPOPlan(int poPlanId);
        List<POPlan> GetPOPlans();
        
        // Slits
        Slit GetActiveSlit(int poPlanId);
        void UpdateSlit(Slit slit);
        
        // Initialize with dummy data
        void InitializeDummyData();
    }
}

