using System;
using System.Collections.Generic;
using System.Linq;
using NDTBundlePOC.Core.Models;

namespace NDTBundlePOC.Core.Services
{
    public class InMemoryDataRepository : IDataRepository
    {
        private List<NDTBundle> _ndtBundles = new List<NDTBundle>();
        private List<NDTBundleFormationChart> _formationCharts = new List<NDTBundleFormationChart>();
        private List<POPlan> _poPlans = new List<POPlan>();
        private List<Slit> _slits = new List<Slit>();
        private int _nextBundleId = 1;
        private int _nextSlitId = 1;

        public void InitializeDummyData()
        {
            // Initialize Formation Chart with size-based configuration
            // Default configuration (Pipe_Size = null)
            _formationCharts.Add(new NDTBundleFormationChart
            {
                NDTBundleFormationChart_ID = 1,
                Mill_ID = 1,
                Pipe_Size = null, // Default for all sizes
                NDT_PcsPerBundle = 20,
                IsActive = true
            });

            // Size-specific configurations
            _formationCharts.Add(new NDTBundleFormationChart
            {
                NDTBundleFormationChart_ID = 2,
                Mill_ID = 1,
                Pipe_Size = 2.0m,
                NDT_PcsPerBundle = 80,
                IsActive = true
            });

            _formationCharts.Add(new NDTBundleFormationChart
            {
                NDTBundleFormationChart_ID = 3,
                Mill_ID = 1,
                Pipe_Size = 3.0m,
                NDT_PcsPerBundle = 45,
                IsActive = true
            });

            // Initialize PO Plans
            _poPlans.Add(new POPlan
            {
                PO_Plan_ID = 1,
                PLC_POID = 1,
                PO_No = "PO_001",
                Shop_ID = 1,
                Pipe_Type = "Type_A",
                Pipe_Size = "2\"",
                PcsPerBundle = 10,
                Pipe_Len = 6.0m,
                PipeWt_per_mtr = 2.5m,
                SAP_Type = "SAP_A"
            });

            _poPlans.Add(new POPlan
            {
                PO_Plan_ID = 2,
                PLC_POID = 2,
                PO_No = "PO_002",
                Shop_ID = 1,
                Pipe_Type = "Type_B",
                Pipe_Size = "3\"",
                PcsPerBundle = 15,
                Pipe_Len = 8.0m,
                PipeWt_per_mtr = 3.0m,
                SAP_Type = "SAP_B"
            });

            // Initialize Slits
            _slits.Add(new Slit
            {
                Slit_ID = _nextSlitId++,
                PO_Plan_ID = 1,
                Slit_No = "SLIT_001",
                Status = 2, // In Progress
                Slit_NDT = 0,
                SlitMillStartTime = DateTime.Now.AddHours(-2)
            });
        }

        public List<NDTBundle> GetNDTBundles() => _ndtBundles.ToList();
        
        public NDTBundle GetNDTBundle(int bundleId) => 
            _ndtBundles.FirstOrDefault(b => b.NDTBundle_ID == bundleId);
        
        public NDTBundle GetActiveNDTBundle(int poPlanId) =>
            _ndtBundles
                .Where(b => b.PO_Plan_ID == poPlanId && b.Status == 1)
                .OrderByDescending(b => b.BundleStartTime)
                .FirstOrDefault();
        
        public void AddNDTBundle(NDTBundle bundle)
        {
            bundle.NDTBundle_ID = _nextBundleId++;
            _ndtBundles.Add(bundle);
        }
        
        public void UpdateNDTBundle(NDTBundle bundle)
        {
            var existing = _ndtBundles.FirstOrDefault(b => b.NDTBundle_ID == bundle.NDTBundle_ID);
            if (existing != null)
            {
                var index = _ndtBundles.IndexOf(existing);
                _ndtBundles[index] = bundle;
            }
        }
        
        public NDTBundleFormationChart GetNDTFormationChart(int millId, decimal? pipeSize)
        {
            // First try to get size-specific chart
            if (pipeSize.HasValue)
            {
                var sizeSpecific = _formationCharts
                    .FirstOrDefault(f => f.Mill_ID == millId && f.Pipe_Size == pipeSize.Value && f.IsActive);
                
                if (sizeSpecific != null) return sizeSpecific;
            }
            
            // Fall back to default (Pipe_Size = null)
            return _formationCharts
                .FirstOrDefault(f => f.Mill_ID == millId && f.Pipe_Size == null && f.IsActive);
        }
        
        public POPlan GetPOPlan(int poPlanId) =>
            _poPlans.FirstOrDefault(p => p.PO_Plan_ID == poPlanId);
        
        public List<POPlan> GetPOPlans() => _poPlans.ToList();

        public void AddPOPlan(POPlan poPlan)
        {
            if (poPlan.PO_Plan_ID == 0)
            {
                poPlan.PO_Plan_ID = _poPlans.Count > 0 ? _poPlans.Max(p => p.PO_Plan_ID) + 1 : 1;
            }
            _poPlans.Add(poPlan);
        }

        public void UpdatePOPlan(POPlan poPlan)
        {
            var existing = _poPlans.FirstOrDefault(p => p.PO_Plan_ID == poPlan.PO_Plan_ID);
            if (existing != null)
            {
                var index = _poPlans.IndexOf(existing);
                _poPlans[index] = poPlan;
            }
        }

        public void DeletePOPlan(int poPlanId)
        {
            var existing = _poPlans.FirstOrDefault(p => p.PO_Plan_ID == poPlanId);
            if (existing != null)
            {
                _poPlans.Remove(existing);
            }
        }
        
        public Slit GetActiveSlit(int poPlanId)
        {
            // If poPlanId is 0, get any active slit (for POC)
            if (poPlanId == 0)
            {
                return _slits
                    .Where(s => s.Status == 2)
                    .OrderByDescending(s => s.SlitMillStartTime)
                    .FirstOrDefault();
            }
            
            return _slits
                .Where(s => s.PO_Plan_ID == poPlanId && s.Status == 2)
                .OrderByDescending(s => s.SlitMillStartTime)
                .FirstOrDefault();
        }
        
        public void UpdateSlit(Slit slit)
        {
            var existing = _slits.FirstOrDefault(s => s.Slit_ID == slit.Slit_ID);
            if (existing != null)
            {
                var index = _slits.IndexOf(existing);
                _slits[index] = slit;
            }
        }
    }
}

