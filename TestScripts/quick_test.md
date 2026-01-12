# Quick Test Guide

## Fastest Way to Test

### Option 1: Web UI (Easiest)
```bash
# Terminal 1: Start server
dotnet run --project NDTBundlePOC.UI.Web --urls "http://localhost:5001"

# Browser: Open http://localhost:5001
# Then:
# 1. Click "Add NDT Cuts" (default 5)
# 2. Click "Add NDT Cuts" again (5 more)
# 3. Select completed bundle
# 4. Click "Print Selected Bundle"
# 5. Check files in Documents/NDT_Bundle_POC_Prints/
```

### Option 2: Automated Script
```bash
# Terminal 1: Start server
dotnet run --project NDTBundlePOC.UI.Web --urls "http://localhost:5001"

# Terminal 2: Run test script
./TestScripts/test_ndt_flow.sh
```

### Option 3: Manual API Calls
```bash
# Start server first, then:

# Add NDT cuts
curl -X POST "http://localhost:5001/api/ndt-cuts?cuts=5"

# View bundles
curl "http://localhost:5001/api/bundles"

# Print bundle (replace 1 with actual bundle ID)
curl -X POST "http://localhost:5001/api/print/1"
```

## What to Verify

✅ Bundles appear in grid after adding cuts
✅ Bundle status changes to "Completed" when threshold reached
✅ Print creates file in Documents/NDT_Bundle_POC_Prints/
✅ Excel export creates file in Documents/NDT_Bundle_POC_Exports/
✅ Bundle status updates to "Printed" after printing

