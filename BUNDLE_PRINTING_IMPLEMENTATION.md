# Bundle Printing Implementation Summary

## Overview
This document summarizes the implementation of tag printing logic for OK pipe bundles and NDT pipe bundles based on the specified requirements.

## Implementation Details

### 1. NDT Bundle Formation Chart Table

**File**: `Database/NDT_BundleFormationChart_Supabase.sql`

Created a PostgreSQL table for Supabase with size-based configuration:
- **Pipe_Size**: NULL means default for all sizes, specific sizes (0.5, 0.75, 1.0, etc.) have specific PcsPerBundle values
- **NDT_PcsPerBundle**: Number of NDT pieces required per bundle for each size
- Pre-populated with the provided size chart:
  - Default: 20 pieces
  - Size-specific: 0.5→250, 0.75→180, 1.0→150, 1.25→140, 1.5→120, 2.0→80, 2.4→60, 2.5→65, 3.0→45, 3.5→40, 4.0→35, 5.0→25, 6.0→20, 8.0→13

### 2. OK Bundle Printing Logic

**File**: `NDTBundlePOC.Core/Services/OKBundleService.cs`

**Scenario 1: Full Bundle**
- Bundle completes when `OK_Pcs >= PcsPerBundle` (fetched from `PO_Plan` table)
- Status set to 2 (Completed)
- `IsFullBundle = true`

**Scenario 2: PO End (Partial Bundle)**
- When PO ends (ButtEnd signal from PLC), bundle closes even if partial
- Status set to 2 (Completed)
- `IsFullBundle = false`
- Bundle is ready for printing

**Bundle Printing Conditions (Status = 3)**
- Bundle must be complete (Status = 2)
- PLC signals `L1L2_PipeDone` (DB250.DBX3.4)
- Bundle is at packing station (`PkIn == SectIn`)

### 3. NDT Bundle Printing Logic

**File**: `NDTBundlePOC.Core/Services/NDTBundleService.cs`

**Scenario 1: Batch Completion by Size**
- When sum of NDT Pcs of the Mill >= NDT Pcs from Formation Chart (based on Pipe_Size)
- Batch number ends and new batch number is created in series
- New batch continues with next sequential number

**Scenario 2: PO End/PO ID End**
- When sum of NDT Pcs < required Pcs from chart
- Batch ends by PO end/PO ID end
- Next PO gets a new batch number

**Batch Numbering**
- Format: `NDT_YY1####` (e.g., `NDT_2410001`)
- Increments sequentially within the same year
- New batch created when:
  - Previous batch sum >= required Pcs (Scenario 1)
  - PO changes or ends (Scenario 2)

### 4. PLC Integration

**File**: `NDTBundlePOC.Core/Services/RealS7PLCService.cs`

**New Methods Added:**
- `IsPOEnded(int millId)`: Reads ButtEnd signal from DB250.DBX2.2
- `IsBundleAtPackingStation(int millId)`: Checks if bundle is at packing station (PkIn == SectIn)
  - Note: Actual PLC addresses need to be determined based on your PLC structure

**Updated Interface**: `IPLCService.cs`
- Added `IsBundleAtPackingStation` method signature

### 5. Data Repository Updates

**File**: `NDTBundlePOC.Core/Services/SupabaseDataRepository.cs`

**Updated Method**: `GetNDTFormationChart`
- Changed from `GetNDTFormationChart(int millId, int? poPlanId)` 
- To: `GetNDTFormationChart(int millId, decimal? pipeSize)`
- First tries to get size-specific configuration
- Falls back to default (Pipe_Size = NULL) if size-specific not found

**File**: `NDTBundlePOC.Core/Services/InMemoryDataRepository.cs`
- Updated to match new interface signature
- Updated dummy data to use size-based configuration

### 6. Model Updates

**File**: `NDTBundlePOC.Core/Models/NDTBundleFormationChart.cs`

**Changed:**
- `PO_Plan_ID` → `Pipe_Size` (decimal?)
- NULL Pipe_Size means default for all sizes

### 7. PLC Polling Service Updates

**File**: `NDTBundlePOC.Core/Services/PLCPollingService.cs`

**Updated Printing Logic:**
- `CheckAndPrintOKBundles()`: Now checks all three conditions:
  1. Status = 2 (Completed) ✓
  2. L1L2_PipeDone signal ✓
  3. At packing station ✓
- `ProcessOKBundlePrint()`: Also checks packing station status before printing

### 8. Dependency Injection Updates

**File**: `NDTBundlePOC.UI.Web/Program.cs`

**Updated Service Registration:**
- OKBundleService and NDTBundleService now receive IPLCService dependency
- Services can check PO end status and packing station status

## Database Schema

### NDT_BundleFormationChart Table (Supabase/PostgreSQL)

```sql
CREATE TABLE "NDT_BundleFormationChart" (
    "NDTBundleFormationChart_ID" SERIAL PRIMARY KEY,
    "Mill_ID" INTEGER NOT NULL,
    "Pipe_Size" DECIMAL(10,2) NULL, -- NULL = default
    "NDT_PcsPerBundle" INTEGER NOT NULL,
    "IsActive" BOOLEAN DEFAULT true,
    "CreatedDate" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "ModifiedDate" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Usage

### 1. Execute SQL Script
Run `Database/NDT_BundleFormationChart_Supabase.sql` in your Supabase SQL Editor to create the table and insert default data.

### 2. Update PLC Addresses
In `RealS7PLCService.cs`, update the `IsBundleAtPackingStation` method with actual PLC addresses for `_Pack[0].PkIn` and `_Pack[0].SectIn`.

### 3. Configuration
The system automatically:
- Fetches `PcsPerBundle` from `PO_Plan` table for OK bundles
- Fetches `NDT_PcsPerBundle` from `NDT_BundleFormationChart` based on `Pipe_Size` for NDT bundles
- Monitors PLC signals for PO end and packing station status
- Prints tags when all conditions are met

## Key Features

1. **OK Bundle Printing**:
   - Uses `PcsPerBundle` from PO_Plan table
   - Handles full bundles (count >= PcsPerBundle)
   - Handles partial bundles (PO end)
   - Prints when: Status=2, L1L2_PipeDone, at packing station

2. **NDT Bundle Printing**:
   - Uses size-based formation chart
   - Handles batch numbering based on sum of NDT Pcs
   - Creates new batch when sum >= required Pcs or PO ends
   - Batch numbers increment in series

3. **PLC Integration**:
   - Reads ButtEnd signal (DB250.DBX2.2) for PO end detection
   - Reads L1L2_PipeDone signal (DB250.DBX3.4) for printing trigger
   - Checks packing station status (PkIn == SectIn)

## Notes

- The packing station check (`IsBundleAtPackingStation`) currently returns `true` by default. You need to update it with actual PLC addresses based on your PLC structure.
- Batch numbering for NDT bundles follows the format `NDT_YY1####` and increments sequentially.
- When PO changes, a new batch number is automatically created for NDT bundles.

