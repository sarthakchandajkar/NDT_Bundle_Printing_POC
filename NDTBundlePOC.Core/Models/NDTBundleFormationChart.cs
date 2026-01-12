namespace NDTBundlePOC.Core.Models
{
    public class NDTBundleFormationChart
    {
        public int NDTBundleFormationChart_ID { get; set; }
        public int Mill_ID { get; set; }
        public int? PO_Plan_ID { get; set; } // NULL = default for all POs
        public int NDT_PcsPerBundle { get; set; }
        public bool IsActive { get; set; }
    }
}

