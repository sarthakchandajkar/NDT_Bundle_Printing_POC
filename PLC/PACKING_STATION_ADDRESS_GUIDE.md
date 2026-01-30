# Finding Packing Station Addresses (_Pack[0].PkIn and _Pack[0].SectIn)

## Overview
The `_Pack[0].PkIn` and `_Pack[0].SectIn` are likely part of a User Defined Type (UDT) or structured data block in your Siemens PLC program. This guide explains how to find the actual DB addresses.

## Method 1: Check TIA Portal Project

### Step 1: Open Your PLC Project
1. Open your Siemens TIA Portal project
2. Navigate to **PLC Tags** or **Data Blocks**

### Step 2: Search for "_Pack"
1. In TIA Portal, use **Ctrl+F** to search for `_Pack`
2. Look for:
   - A Data Block (DB) named something like `DB_Pack`, `DB_Packing`, or similar
   - A UDT (User Defined Type) named `Pack` or `Packing`
   - A variable named `_Pack` in any Data Block

### Step 3: Find the Structure Definition
Once you find `_Pack`, check:
- **If it's a UDT**: Look at the UDT definition to see the structure members (`PkIn`, `SectIn`, etc.)
- **If it's in a DB**: Check the DB structure to see the byte offsets

### Step 4: Calculate DB Addresses
If `_Pack` is in a DB block (e.g., `DB100`), the addresses would be:
- `_Pack[0].PkIn` = `DB100.DBWX` (where X is the byte offset)
- `_Pack[0].SectIn` = `DB100.DBWY` (where Y is the byte offset)

**Example:**
If `_Pack` is in `DB100` and the structure is:
```
_Pack[0]
  PkIn    (offset: 0 bytes, type: WORD)   → DB100.DBW0
  SectIn  (offset: 2 bytes, type: WORD)    → DB100.DBW2
```

Then:
- `_Pack[0].PkIn` = `DB100.DBW0`
- `_Pack[0].SectIn` = `DB100.DBW2`

## Method 2: Check Existing PLC Code

### Check siemens.stl or Other PLC Files
1. Look in your PLC program files for references to `_Pack`
2. Search for `PkIn` or `SectIn` in the PLC code
3. Check if there are any comments or documentation about packing station addresses

## Method 3: Use PLC Watch Table

1. Connect to your PLC in TIA Portal
2. Open a **Watch Table**
3. Try to add `_Pack[0].PkIn` and `_Pack[0].SectIn` as watch variables
4. TIA Portal will show you the actual DB address if the variable exists

## Method 4: Alternative - Use Existing Packing Tags

Based on your existing PLC tag definitions, you might be able to use:

**From DB261 (L2L1 tags - Line 2 to Line 1):**
- `L2L1_PackingSlitID` → `DB261.DBW10`
- `L2L1_PackingBundleID` → `DB261.DBW12`
- `L2L1_PackingPipeID` → `DB261.DBW14`
- `L2L1_PackingPieces` → `DB261.DBW16`

**From DB250:**
- `L1L2_Bundle_Pk` → `DB250.DBX3.0` (Bundle Pack signal)

You might be able to determine packing station status from these existing tags instead of `_Pack[0].PkIn` and `_Pack[0].SectIn`.

## Method 5: Contact PLC Programmer

If you cannot find the addresses:
1. Contact the person who programmed the PLC
2. Ask for:
   - The DB block number containing `_Pack`
   - The byte offsets for `PkIn` and `SectIn` within the structure
   - Or the complete DB address (e.g., `DB100.DBW0`)

## Implementation in Code

Once you have the addresses, update `RealS7PLCService.cs`:

```csharp
public bool IsBundleAtPackingStation(int millId)
{
    try
    {
        if (!IsConnected)
        {
            return false;
        }

        lock (_lockObject)
        {
            if (_plc == null || !_plc.IsConnected)
            {
                return false;
            }

            // Replace these with actual addresses from your PLC
            var pkIn = _plc.Read("DB100.DBW0");    // _Pack[0].PkIn
            var sectIn = _plc.Read("DB100.DBW2");  // _Pack[0].SectIn
            
            if (pkIn != null && sectIn != null)
            {
                int pkInValue = Convert.ToInt32(pkIn);
                int sectInValue = Convert.ToInt32(sectIn);
                return pkInValue == sectInValue;
            }
        }
    }
    catch (Exception ex)
    {
        // Error handling...
        return false;
    }
}
```

## Temporary Workaround

If you cannot find the addresses immediately, the current implementation returns `true` by default, which allows printing to proceed. However, you should find and implement the actual addresses for proper operation.

## Notes

- `_Pack[0]` suggests it's an array - check if there are multiple packing stations (`_Pack[1]`, `_Pack[2]`, etc.)
- The data type might be `WORD` (16-bit) or `INT` (16-bit signed) - adjust the read method accordingly
- If the structure uses different data types, you may need to read as `DBW` (WORD) or `DBD` (DWORD) depending on the PLC definition

