using System;

namespace NDTBundlePOC.Core.Models
{
    public class Slit
    {
        public int Slit_ID { get; set; }
        public int PO_Plan_ID { get; set; }
        public string Slit_No { get; set; }
        public int Status { get; set; } // 1=Available, 2=InProgress, 3=Completed
        public int Slit_NDT { get; set; }
        public DateTime? SlitMillStartTime { get; set; }
    }
}

