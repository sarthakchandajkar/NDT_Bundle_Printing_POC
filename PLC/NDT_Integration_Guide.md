# NDT Bundle Tag Printing - Integration Guide

## Overview
This guide explains how to integrate the NDT bundle tag printing system into your existing PLC communication code (PLCThread_TM.cs).

## Prerequisites
1. ✅ Database schema created (run `Database/NDT_Database_Schema.sql`)
2. ✅ PLC tag classes updated (PLCTags_DB250, PLCTags_DB260, PLCTags_DB251)
3. ✅ PLC tag addresses updated (PLCTagAddresses_TM.cs)
4. ✅ Telerik Reporting installed and configured
5. ✅ NDT printer configured in PlantDevice table

## Integration Steps

### Step 1: Add Using Statements
Add to the top of your `PLCThread_TM.cs`:

```csharp
using NDTBundlePOC.PLC;
```

### Step 2: Initialize NDT Handlers
In your class constructor or initialization method, add:

```csharp
private NDTBundleFormationLogic _ndtBundleFormation;
private NDTBundlePrintHandler _ndtBundlePrintHandler;

// In constructor or initialization:
_ndtBundleFormation = new NDTBundleFormationLogic(_millId);
_ndtBundlePrintHandler = new NDTBundlePrintHandler(_millId);
```

### Step 3: Add to Main PLC Loop
In your main PLC reading loop (where you process other PLC signals), add the following code:

```csharp
// ============================================
// NDT Bundle Formation (process NDT cuts)
// ============================================
_ndtBundleFormation.Process_NDTBundleFormation(ref sqlcmd, PLCTags_DB251.L1L2_NDTCut);

// ============================================
// NDT Bundle Print Trigger
// ============================================
if (PLCTags_DB250.L1L2_NDTBundleDone && !PLCTags_DB260.L2L1_AckNDTBundleDone)
{
    ushort slitIdFromPLC = PLCTags_DB251.L1L2_Slit_ID_BundlePk; // Or appropriate tag
    _ndtBundlePrintHandler.Process_NDTBundlePrint(ref sqlcmd, slitIdFromPLC);
    Write(PLCTagAddresses.L2L1_AckNDTBundleDone, true, true);
}

// Acknowledge reset
if (!PLCTags_DB250.L1L2_NDTBundleDone && PLCTags_DB260.L2L1_AckNDTBundleDone)
{
    Write(PLCTagAddresses.L2L1_AckNDTBundleDone, false, true);
}

// ============================================
// NDT Bundle Reprint
// ============================================
if (PLCTags_DB250.L1L2_NDTBundleReprint && !PLCTags_DB260.L2L1_AckNDTBundleReprint)
{
    ushort slitIdFromPLC = PLCTags_DB251.L1L2_Slit_ID_BundlePk; // Or appropriate tag
    _ndtBundlePrintHandler.Process_NDTBundleReprint(ref sqlcmd, slitIdFromPLC);
    Write(PLCTagAddresses.L2L1_AckNDTBundleReprint, true, true);
}

if (!PLCTags_DB250.L1L2_NDTBundleReprint && PLCTags_DB260.L2L1_AckNDTBundleReprint)
{
    Write(PLCTagAddresses.L2L1_AckNDTBundleReprint, false, true);
}
```

### Step 4: Update M1_Slit Table
In your pipe cut processing logic, add code to update Slit_NDT:

```csharp
// Update Slit_NDT with current NDT count from PLC
sqlcmd.CommandText = "UPDATE M" + _millId.ToString() + "_Slit SET 
                      Slit_NDT = " + PLCTags_DB251.L1L2_NDTCut.ToString() + 
                      " WHERE Slit_ID = " + Slit_ID.ToString();
sqlcmd.ExecuteNonQuery();
```

### Step 5: Reset NDT Counter on New Slit/PO
When starting a new slit or PO, reset the NDT cut counter:

```csharp
_ndtBundleFormation.ResetNDTCutCounter();
```

## PLC Tag Mapping

### DB250 (PLC → L2)
- `DB250.DBX6.0` → `L1L2_NDTBundleDone` (PLC signals bundle is done)
- `DB250.DBX6.1` → `L1L2_NDTBundleReprint` (PLC signals reprint request)

### DB260 (L2 → PLC)
- `DB260.DBX6.0` → `L2L1_AckNDTBundleDone` (Acknowledge bundle done)
- `DB260.DBX6.1` → `L2L1_AckNDTBundleReprint` (Acknowledge reprint)

### DB251 (PLC → L2)
- `DB251.DBW6` → `L1L2_NDTCut` (Current NDT cut count - already exists)
- `DB251.DBW28` → `L1L2_NDTBundle_PCs_Count` (NDT pieces in current bundle)
- `DB251.DBW30` → `L1L2_NDTBundle_No` (Bundle number reference)

## Telerik Report Setup

1. **Create Report**: Create `Rpt_NDTLabel` report in Telerik Report Designer
   - Similar structure to `Rpt_MillLabel` but for NDT bundles
   - Use stored procedure `SP_SAPData_Mill_NDTBundle` as data source
   - Parameters: `MillLine`, `MillPOId`, `NDTBundleID`, `isReprint`, `ConnectionString`

2. **Report Location**: Place report in your `IIOTReport` namespace/assembly

## CSV Export Path

Update the CSV export path in `CSVUtility.CreateNDTBundleCSV` to match your project structure:

```csharp
string csvPath = baseDir.Replace("FoxPasMill_" + millId, "PSR") + 
    "PAS-SAP\\To SAP\\TM\\NDT Bundle\\";
```

Ensure this directory exists or update the path to match your actual file structure.

## Testing Checklist

- [ ] Database tables created and accessible
- [ ] Stored procedure `SP_SAPData_Mill_NDTBundle` tested
- [ ] PLC tags mapped correctly in TIA Portal
- [ ] NDT bundle formation processes cuts correctly
- [ ] Bundle completion triggers print
- [ ] Tag prints to configured printer
- [ ] CSV file created in correct location
- [ ] Bundle status updates correctly (1→2→3→4)
- [ ] PLC acknowledgment works correctly
- [ ] Reprint functionality works

## Troubleshooting

### Bundle Not Forming
- Check NDT cut count is incrementing in PLC
- Verify `NDT_BundleFormationChart` has entry for your Mill_ID
- Check database connection string

### Print Not Triggering
- Verify `L1L2_NDTBundleDone` is set in PLC
- Check bundle status is 2 (Completed)
- Verify printer name in PlantDevice table
- Check Telerik Reporting is configured

### CSV Not Created
- Verify stored procedure executes successfully
- Check file path exists and is writable
- Verify connection string is correct

## Notes

- All database operations use `ConfigurationManager.ConnectionStrings["ServerConnectionString"]`
- Ensure this connection string points to your production database
- PLC connection should use `CpuType.S71200` with `rack=0, slot=0`
- Update printer IP address in PlantDevice table before deployment

