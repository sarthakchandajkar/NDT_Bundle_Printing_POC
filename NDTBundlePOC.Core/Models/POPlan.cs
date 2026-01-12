namespace NDTBundlePOC.Core.Models
{
    public class POPlan
    {
        public int PO_Plan_ID { get; set; }
        public string PO_No { get; set; }
        public int Shop_ID { get; set; }
        public string Pipe_Grade { get; set; }
        public string Pipe_Size { get; set; }
        public decimal Pipe_Thick { get; set; }
        public decimal Pipe_Len { get; set; }
        public int Status { get; set; } // 1=Active, 2=InProgress, 3=Completed
    }
}

