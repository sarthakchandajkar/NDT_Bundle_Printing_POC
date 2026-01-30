namespace NDTBundlePOC.Core.Models
{
    public class NDTBundleFormationChart
    {
        public int NDTBundleFormationChart_ID { get; set; }
        public int Mill_ID { get; set; }
        public decimal? Pipe_Size { get; set; } // NULL = default for all sizes
        public int NDT_PcsPerBundle { get; set; }
        public bool IsActive { get; set; }
    }
}

