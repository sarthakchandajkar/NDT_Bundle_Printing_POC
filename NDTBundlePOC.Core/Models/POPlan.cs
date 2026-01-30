namespace NDTBundlePOC.Core.Models
{
    public class POPlan
    {
        public int PO_Plan_ID { get; set; }
        public int? PLC_POID { get; set; }
        public string PO_No { get; set; }
        public string Pipe_Type { get; set; }
        public string Pipe_Size { get; set; }
        public int PcsPerBundle { get; set; }
        public decimal Pipe_Len { get; set; }
        public decimal PipeWt_per_mtr { get; set; }
        public string SAP_Type { get; set; }
        public int? Shop_ID { get; set; }
    }
}

