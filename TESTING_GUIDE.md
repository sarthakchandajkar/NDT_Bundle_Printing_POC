# NDT Bundle Tag Printing - Testing Guide

## Overview
This guide provides step-by-step instructions to test the complete NDT bundle tag printing flow in the POC environment.

## Testing Methods

### Method 1: Web UI Testing (Recommended for POC)

#### Step 1: Start the Web Server
```bash
cd /Users/sarthakchandajkar/NDT_Bundle_Printing_POC
dotnet run --project NDTBundlePOC.UI.Web --urls "http://localhost:5001"
```

#### Step 2: Open Browser
Navigate to: `http://localhost:5001`

#### Step 3: Test Bundle Formation
1. **Add NDT Cuts**: 
   - Enter number of NDT cuts (e.g., 5)
   - Click "Add NDT Cuts"
   - Observe bundles being created in the grid

2. **Verify Bundle Creation**:
   - Check that bundles appear in the grid
   - Verify Bundle No, Batch No, NDT Pcs are populated
   - Check Status changes from "Active" → "Completed"

3. **Test Multiple Cuts**:
   - Add 3 cuts → Bundle should be Active
   - Add 5 more cuts → Bundle should complete (if threshold is 10)
   - New bundle should be created automatically

#### Step 4: Test Printing
1. **Select a Bundle**: Click on a completed bundle (Status = "Completed")
2. **Print Bundle**: Click "Print Selected Bundle" or "Print Bundles"
3. **Verify Output**:
   - Check console for print messages
   - Verify file created in: `Documents/NDT_Bundle_POC_Prints/`
   - Verify Excel export in: `Documents/NDT_Bundle_POC_Exports/`

#### Step 5: Test Filtering
1. **Date Filter**: Set From/To dates and click "Filter"
2. **PO Filter**: Type PO number in PO field
3. **Search**: Type bundle number in search box
4. **Verify**: Grid updates to show filtered results

#### Step 6: Test Excel Export
1. **Select Bundles**: Select one or more bundles
2. **Export**: Click "Excel Export" button
3. **Verify**: CSV file downloads with bundle data

### Method 2: Console UI Testing

#### Step 1: Run Console UI
```bash
cd /Users/sarthakchandajkar/NDT_Bundle_Printing_POC
dotnet run --project NDTBundlePOC.UI
```

#### Step 2: Follow Menu Prompts
- Option 1: Add NDT Cuts
- Option 2: View Bundles
- Option 3: Print Bundle Tag
- Option 4: Exit

### Method 3: Unit Testing (Code-Level)

#### Test Bundle Formation Logic
```csharp
// Create test
var repository = new InMemoryDataRepository();
repository.InitializeDummyData();
var bundleService = new NDTBundleService(repository);

// Test: Add NDT cuts
bundleService.ProcessNDTCuts(1, 5); // Add 5 cuts
var bundles = bundleService.GetAllNDTBundles();
Assert.True(bundles.Count > 0);

// Test: Bundle completion
bundleService.ProcessNDTCuts(1, 5); // Add 5 more (total 10)
var completedBundles = bundleService.GetBundlesReadyForPrinting();
Assert.True(completedBundles.Count > 0);
```

### Method 4: Database Testing (If You Have Database)

#### Step 1: Run SQL Script
```sql
-- Execute in your test database
USE [YourTestDatabase];
GO

-- Run the schema script
-- File: Database/NDT_Database_Schema.sql
```

#### Step 2: Insert Test Data
```sql
-- Insert NDT Bundle Formation Chart
INSERT INTO NDT_BundleFormationChart (Mill_ID, PO_Plan_ID, NDT_PcsPerBundle, IsActive)
VALUES (1, NULL, 10, 1); -- 10 pieces per bundle for all POs

-- Insert test PO Plan (if not exists)
INSERT INTO PO_Plan (PO_No, Shop_ID, Pipe_Grade, Pipe_Size, Pipe_Thick, Pipe_Len, Status)
VALUES ('PO_TEST_001', 1, 'X65', '12"', 10.5, 12.0, 1);

-- Insert test Slit
INSERT INTO M1_Slit (PO_Plan_ID, Slit_No, Status, SlitMillStartTime)
VALUES (1, 'SLIT_001', 2, GETDATE());
```

#### Step 3: Test Stored Procedure
```sql
-- Create a test bundle
INSERT INTO M1_NDTBundles (PO_Plan_ID, Slit_ID, Bundle_No, NDT_Pcs, Batch_No, Status, BundleStartTime, BundleEndTime)
VALUES (1, 1, 'PO_TEST_001NDT001', 10, 'NDT_PO_TEST_001001', 2, GETDATE(), GETDATE());

-- Test stored procedure
EXEC SP_SAPData_Mill_NDTBundle @MillId = 1, @BundleNum = 'PO_TEST_001NDT001';
```

### Method 5: API Testing (Using curl or Postman)

#### Test Bundle Formation
```bash
# Add NDT cuts
curl -X POST "http://localhost:5001/api/ndt-cuts?cuts=5"

# Get all bundles
curl "http://localhost:5001/api/bundles"
```

#### Test Printing
```bash
# Get bundle ID first, then print
curl -X POST "http://localhost:5001/api/print/1"
```

#### Test PLC Connection (Simulated)
```bash
# Connect to PLC
curl -X POST "http://localhost:5001/api/plc/connect?ipAddress=192.168.1.100&rack=0&slot=1"

# Check status
curl "http://localhost:5001/api/plc/status"

# Process cuts from PLC
curl -X POST "http://localhost:5001/api/plc/process-cuts/1"
```

## Test Scenarios

### Scenario 1: Basic Bundle Formation
**Goal**: Verify bundles are created when NDT cuts are added

**Steps**:
1. Start with no bundles
2. Add 5 NDT cuts
3. Verify: One bundle created with Status = "Active", NDT_Pcs = 5
4. Add 5 more NDT cuts
5. Verify: Bundle Status = "Completed", NDT_Pcs = 10, IsFullBundle = true
6. Verify: New bundle created automatically

**Expected Result**: Bundles form correctly and complete when threshold reached

### Scenario 2: Batch Number Generation
**Goal**: Verify batch numbers increment in series

**Steps**:
1. Create first bundle for PO_001
2. Verify: Batch_No = "NDT_PO_001001"
3. Complete bundle and create next
4. Verify: Batch_No = "NDT_PO_001002"
5. Complete bundle and create next
6. Verify: Batch_No = "NDT_PO_001003"

**Expected Result**: Batch numbers increment sequentially

### Scenario 3: Print Flow
**Goal**: Verify complete print workflow

**Steps**:
1. Create completed bundle (Status = 2)
2. Select bundle in UI
3. Click "Print Selected Bundle"
4. Verify: Print file created
5. Verify: Excel export created
6. Verify: Bundle Status = 3 (Printed)

**Expected Result**: Print and export complete successfully

### Scenario 4: Multiple Bundle Printing
**Goal**: Verify multiple bundles can be printed at once

**Steps**:
1. Create 3 completed bundles
2. Select all 3 bundles (Ctrl+Click)
3. Click "Print Bundles"
4. Verify: All 3 bundles printed
5. Verify: All 3 Excel exports created

**Expected Result**: All selected bundles print successfully

### Scenario 5: Filtering and Search
**Goal**: Verify filtering works correctly

**Steps**:
1. Create bundles with different dates
2. Set date filter (From/To)
3. Click "Filter"
4. Verify: Only bundles in date range shown
5. Type PO number in search
6. Verify: Only matching bundles shown

**Expected Result**: Filters work correctly

### Scenario 6: Reprint Functionality
**Goal**: Verify reprint works

**Steps**:
1. Print a bundle (Status = 3)
2. Select the printed bundle
3. Click "Reprint Bundle"
4. Verify: New print file created
5. Verify: IsReprint flag set correctly

**Expected Result**: Reprint creates new print file

## Verification Checklist

### Bundle Formation
- [ ] Bundles created when NDT cuts added
- [ ] Bundle status updates correctly (1→2)
- [ ] Bundle completes when threshold reached
- [ ] New bundle created automatically after completion
- [ ] Batch numbers increment correctly
- [ ] Bundle numbers are sequential

### Printing
- [ ] Print button enabled for completed bundles
- [ ] Print creates file output
- [ ] Excel export created
- [ ] Bundle status updates to 3 (Printed)
- [ ] Print data is correct (Bundle No, Batch No, etc.)

### Data Integrity
- [ ] PO_No is correct
- [ ] Pipe details (Grade, Size, Length) are correct
- [ ] NDT_Pcs count is accurate
- [ ] Bundle weight is calculated (if applicable)
- [ ] Start/End times are recorded

### UI Functionality
- [ ] Grid displays all bundles
- [ ] Filters work correctly
- [ ] Search works correctly
- [ ] Selection works (single and multiple)
- [ ] Status bar shows correct counts
- [ ] Excel export downloads correctly

## Debugging Tips

### Check Console Output
When running the application, watch for:
- `✓ Added X NDT cuts` - Bundle formation working
- `✓ Bundle X printed successfully` - Print working
- `✗ Error: ...` - Any errors

### Check File Outputs
- **Print Files**: `Documents/NDT_Bundle_POC_Prints/`
- **Excel Exports**: `Documents/NDT_Bundle_POC_Exports/`

### Check Data
```csharp
// In code, you can inspect:
var bundles = bundleService.GetAllNDTBundles();
foreach (var bundle in bundles)
{
    Console.WriteLine($"Bundle: {bundle.Bundle_No}, Status: {bundle.Status}, Pcs: {bundle.NDT_Pcs}");
}
```

### Common Issues

**Issue**: Bundles not forming
- **Check**: NDT Formation Chart has entry for Mill_ID
- **Check**: Active Slit exists
- **Check**: PO_Plan_ID is valid

**Issue**: Print not working
- **Check**: Bundle Status is 2 (Completed)
- **Check**: Printer service is configured
- **Check**: File output directory exists

**Issue**: Excel export fails
- **Check**: EPPlus package is installed
- **Check**: Export path is writable
- **Check**: File permissions

## Automated Test Script

Create a test script to run all scenarios:

```bash
#!/bin/bash
# test_ndt_flow.sh

echo "Testing NDT Bundle Flow..."

# Start server in background
dotnet run --project NDTBundlePOC.UI.Web --urls "http://localhost:5001" &
SERVER_PID=$!
sleep 5

# Test 1: Add NDT cuts
echo "Test 1: Adding NDT cuts..."
curl -X POST "http://localhost:5001/api/ndt-cuts?cuts=5"

# Test 2: Get bundles
echo "Test 2: Getting bundles..."
curl "http://localhost:5001/api/bundles"

# Test 3: Print bundle (if exists)
echo "Test 3: Printing bundle..."
curl -X POST "http://localhost:5001/api/print/1"

echo "Tests completed!"
kill $SERVER_PID
```

## Next Steps After Testing

1. **If POC tests pass**: Proceed to integrate with production code
2. **If issues found**: Review error messages and fix
3. **Database testing**: Run SQL script in test database
4. **PLC integration**: Follow `PLC/NDT_Integration_Guide.md`
5. **Production deployment**: Test in staging environment first

## Quick Test Commands

```bash
# Quick test - Web UI
dotnet run --project NDTBundlePOC.UI.Web --urls "http://localhost:5001"
# Then open http://localhost:5001 in browser

# Quick test - Console UI
dotnet run --project NDTBundlePOC.UI

# Quick test - API only
dotnet run --project NDTBundlePOC.UI.Web --urls "http://localhost:5001"
# Then use curl commands above
```

