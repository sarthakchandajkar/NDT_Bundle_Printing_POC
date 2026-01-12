public class PLCTagAddresses_TM
{
    #region DB250
    public string L1L2_DB250_Protect_Read { get; set; } = "DB250.DBB0";
    public string L1L2_HBeat { get; set; } = "DB250.DBB1";
    public string L1L2_LineRun { get; set; } = "DB250.DBX2.0";
    public string L1L2_MotorTurned { get; set; } = "DB250.DBX2.1";
    public string L1L2_ButtEnd { get; set; } = "DB250.DBX2.2";
    public string L1L2_PipeCut { get; set; } = "DB250.DBX2.3";
    public string L1L2_Slit_Start { get; set; } = "DB250.DBX2.4";
    public string L1L2_Slit_End { get; set; } = "DB250.DBX2.5";
    public string L1L2_New_Bundle_Start { get; set; } = "DB250.DBX2.6";
    public string L1L2_Bundle_End { get; set; } = "DB250.DBX2.7";
    public string L1L2_Bundle_Pk { get; set; } = "DB250.DBX3.0";
    public string L1L2_DB250_Spare_1 { get; set; } = "DB250.DBX3.1";
    public string L1L2_SectionDone { get; set; } = "DB250.DBX3.2";
    public string L1L2_SectionReprint { get; set; } = "DB250.DBX3.3";
    public string L1L2_PipeDone { get; set; } = "DB250.DBX3.4";
    public string L1L2_PipeReprint { get; set; } = "DB250.DBX3.5";
    public string L1L2_SectionSensor { get; set; } = "DB250.DBX3.6";
    public string L1L2_PipeSensor { get; set; } = "DB250.DBX3.7";
    public string L1L2_Al_SectStopUp { get; set; } = "DB250.DBX4.0";
    public string L1L2_Al_SectStopDn { get; set; } = "DB250.DBX4.1";
    public string L1L2_Al_PipeStopUp { get; set; } = "DB250.DBX4.2";
    public string L1L2_Al_PipeStopDn { get; set; } = "DB250.DBX4.3";
    public string L1L2_AckReconOn { get; set; } = "DB250.DBX4.4";
    public string L1L2_AckReconOff { get; set; } = "DB250.DBX4.5";
    public string L1L2_AckReconSlit { get; set; } = "DB250.DBX4.6";
    public string L1L2_AckReconPacking { get; set; } = "DB250.DBX4.7";

    public string L1L2_PipeOk { get; set; } = "DB250.DBX5.0";
    public string L1L2_PipeNok { get; set; } = "DB250.DBX5.1";
    public string L1L2_PipeNDT { get; set; } = "DB250.DBX5.2";
    public string L1L2_DB250_Spare_2 { get; set; } = "DB250.DBX5.3";
    public string L1L2_DB250_Spare_3 { get; set; } = "DB250.DBX5.4";
    public string L1L2_DB250_Spare_4 { get; set; } = "DB250.DBX5.5";
    public string L1L2_DB250_Spare_5 { get; set; } = "DB250.DBX5.6";
    public string L1L2_ShortCut { get; set; } = "DB250.DBX5.7";
    
    // NDT Bundle Tags
    public string L1L2_NDTBundleDone { get; set; } = "DB250.DBX6.0";
    public string L1L2_NDTBundleReprint { get; set; } = "DB250.DBX6.1";

    #endregion

    #region DB260
    public string L2L1_DB260_Protect_Read { get; set; } = "DB260.DBB0";
    public string L2L1_HBeat { get; set; } = "DB260.DBB1";
    public string L2L1_MotorLocked { get; set; } = "DB260.DBX2.0";
    public string L2L1_AckMotorTurned { get; set; } = "DB260.DBX2.1";
    public string L2L1_AckButtEnd { get; set; } = "DB260.DBX2.2";
    public string L2L1_AckPipeCut { get; set; } = "DB260.DBX2.3";
    public string L2L1_AckSlit_Start { get; set; } = "DB260.DBX2.4";
    public string L2L1_Ack_Slit_End { get; set; } = "DB260.DBX2.5";
    public string L2L1_AckNew_Bundle_Start { get; set; } = "DB260.DBX2.6";
    public string L2L1_Ack_Bundle_End { get; set; } = "DB260.DBX2.7";
    public string L2L1_Ack_Bundle_Pk { get; set; } = "DB260.DBX3.0";
    public string L2L1_Slit_Scan_Avail { get; set; } = "DB260.DBX3.1";
    public string L2L1_AckSectionDone { get; set; } = "DB260.DBX3.2";
    public string L2L1_AckSectionReprint { get; set; } = "DB260.DBX3.3";
    public string L2L1_AckPipeDone { get; set; } = "DB260.DBX3.4";
    public string L2L1_AckPipeReprint { get; set; } = "DB260.DBX3.5";
    public string L2L1_PASEn { get; set; } = "DB260.DBX3.6";
    public string L2L1_ReconOn { get; set; } = "DB260.DBX3.7";
    public string L2L1_ReconOff { get; set; } = "DB260.DBX4.0";
    public string L2L1_ReconSlit { get; set; } = "DB260.DBX4.1";
    public string L2L1_ReconPacking { get; set; } = "DB260.DBX4.2";
    public string L2L1_DB260_Spare_5 { get; set; } = "DB260.DBX4.3";
    public string L2L1_AckShortCut { get; set; } = "DB260.DBX4.4";
    public string L2L1_DB260_Spare_7 { get; set; } = "DB260.DBX4.5";
    public string L2L1_DB260_Spare_8 { get; set; } = "DB260.DBX4.6";
    public string L2L1_DB260_Spare_9 { get; set; } = "DB260.DBX4.7";
    public string L2L1_DB260_Spare_10 { get; set; } = "DB260.DBX5.0";
    public string L2L1_DB260_Spare_11 { get; set; } = "DB260.DBX5.1";
    public string L2L1_DB260_Spare_12 { get; set; } = "DB260.DBX5.2";
    public string L2L1_DB260_Spare_13 { get; set; } = "DB260.DBX5.3";
    public string L2L1_DB260_Spare_14 { get; set; } = "DB260.DBX5.4";
    public string L2L1_DB260_Spare_15 { get; set; } = "DB260.DBX5.5";
    public string L2L1_DB260_Spare_16 { get; set; } = "DB260.DBX5.6";
    public string L2L1_DB260_Spare_17 { get; set; } = "DB260.DBX5.7";
    
    // NDT Bundle Acknowledgment Tags
    public string L2L1_AckNDTBundleDone { get; set; } = "DB260.DBX6.0";
    public string L2L1_AckNDTBundleReprint { get; set; } = "DB260.DBX6.1";

    #endregion

    #region DB251
    public string L1L2_DB251_Protect_Read { get; set; } = "DB251.DBW0";
    public string L1L2_OKCut { get; set; } = "DB251.DBW2";
    public string L1L2_NOKCut { get; set; } = "DB251.DBW4";
    public string L1L2_NDTCut { get; set; } = "DB251.DBW6";
    public string L1L2_PLC_PO_ID { get; set; } = "DB251.DBW8";
    public string L1L2_PLC_Slit_ID { get; set; } = "DB251.DBW10";
    public string L1L2_Bundle_PCs_Count { get; set; } = "DB251.DBW12";
    public string L1L2_PLC_PO_ID_2 { get; set; } = "DB251.DBW14";
    public string L1L2_PLC_Slit_ID_2 { get; set; } = "DB251.DBW16";
    public string L1L2_Bundle_PCs_Count_2 { get; set; } = "DB251.DBW18";
    public string L1L2_Slit_ID_SlitEnd { get; set; } = "DB251.DBW20";
    public string L1L2_Slit_ID_BundleEnd { get; set; } = "DB251.DBW22";
    public string L1L2_Slit_ID_BundlePk { get; set; } = "DB251.DBW24";
    public string L1L2_DB251_Spare1 { get; set; } = "DB251.DBW26";
    
    // NDT Bundle Tags
    public string L1L2_NDTBundle_PCs_Count { get; set; } = "DB251.DBW28";
    public string L1L2_NDTBundle_No { get; set; } = "DB251.DBW30";

    #endregion

    #region DB261
    public string L2L1_DB261_Protect_Read { get; set; } = "DB261.DBW0";
    public string L2L1_PO_ID { get; set; } = "DB261.DBW2";
    public string L2L1_PO_Type_ID { get; set; } = "DB261.DBW4";
    public string L2L1_Slit_ID { get; set; } = "DB261.DBW6";
    public string L2L1_Pcs_Per_Bundle { get; set; } = "DB261.DBW8";
    public string L2L1_PackingSlitID { get; set; } = "DB261.DBW10";
    public string L2L1_PackingBundleID { get; set; } = "DB261.DBW12";
    public string L2L1_PackingPipeID { get; set; } = "DB261.DBW14";
    public string L2L1_PackingPieces { get; set; } = "DB261.DBW16";
    public string L2L1_DB261_Spare_5 { get; set; } = "DB261.DBW18";
    public string L2L1_DB261_Spare_6 { get; set; } = "DB261.DBW20";

    #endregion

    #region DB252
    public string L1L2_DB252_Protect_Read { get; set; } = "DB252.DBD0";
    public string L1L2_CutLength { get; set; } = "DB252.DBD4";
    public string L1L2_DB252_Spare_1 { get; set; } = "DB252.DBD8";
    public string L1L2_ShortLength { get; set; } = "DB252.DBD12";
    public string L1L2_DB252_Spare_3 { get; set; } = "DB252.DBD16";
    public string L1L2_DB252_Spare_4 { get; set; } = "DB252.DBD20";
    public string L1L2_DB252_Spare_5 { get; set; } = "DB252.DBD24";
    public string L1L2_DB252_Spare_6 { get; set; } = "DB252.DBD28";


    #endregion

    #region DB262
    public string L2L1_DB212_Protect_Read { get; set; } = "DB262.DBD0";
    public string L2L1_SlitLength { get; set; } = "DB262.DBD4";
    public string L2L1_SlitWeight { get; set; } = "DB262.DBD8"; 
    public string L2L1_SlitLengthPO { get; set; } = "DB262.DBD12";
    public string L2L1_SlitWeightPO { get; set; } = "DB262.DBD16";
    public string L2L1_DB262_Spare_5 { get; set; } = "DB262.DBD20";
    public string L2L1_DB262_Spare_6 { get; set; } = "DB262.DBD24";
    public string L2L1_PO_Length { get; set; } = "DB262.DBD28";

    #endregion
}

