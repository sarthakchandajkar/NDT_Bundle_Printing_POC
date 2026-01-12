using System;

namespace NDTBundlePOC.Core.Models
{
    public class NDTBundle
    {
        public int NDTBundle_ID { get; set; }
        public int PO_Plan_ID { get; set; }
        public int? Slit_ID { get; set; }
        public string Bundle_No { get; set; }
        public int NDT_Pcs { get; set; }
        public decimal Bundle_Wt { get; set; }
        public int Status { get; set; } // 1=Active, 2=Completed, 3=Printed
        public bool IsFullBundle { get; set; }
        public DateTime BundleStartTime { get; set; }
        public DateTime? BundleEndTime { get; set; }
        public DateTime? OprDoneTime { get; set; }
        public string Batch_No { get; set; }
        public string Parent_BundleNo { get; set; }
    }
}

