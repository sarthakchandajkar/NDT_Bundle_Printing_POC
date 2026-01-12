# Physical PLC Connection Guide

## Overview
This guide explains how to connect a physical Siemens S7-1200 PLC to receive pipe information and automatically print both OK and NDT bundle tags.

## Prerequisites

1. **S7netplus Package**: Install the S7netplus NuGet package for PLC communication
2. **Network Connection**: PLC and computer must be on the same network
3. **PLC Configuration**: PLC must be configured with correct IP address
4. **Printer**: Physical printer must be connected (Ethernet or Serial)

## Step 1: Install S7netplus Package

The package is already referenced in `NDTBundlePOC.Core.csproj`. To enable real PLC communication:

1. Uncomment the S7netplus using statements in `RealS7PLCService.cs`
2. Uncomment the actual PLC connection code
3. Remove the simulation code

## Step 2: Configure PLC Settings

Edit `appsettings.json`:

```json
{
  "PLC": {
    "IPAddress": "192.168.1.100",      // Your PLC's IP address
    "Rack": 0,                          // Usually 0 for S7-1200
    "Slot": 1,                          // Usually 1 for S7-1200
    "MillId": 1,                        // Mill ID (1, 2, 3, etc.)
    "EnablePolling": true,              // Enable automatic polling
    "PollingIntervalMs": 1000           // Poll every 1 second
  },
  "Printer": {
    "Address": "192.168.1.200",         // Your printer's IP address
    "Port": 9100,                        // TCP port (9100 for raw printing)
    "UseNetwork": true                   // true for Ethernet, false for Serial
  }
}
```

## Step 3: PLC Tag Mapping

### OK Pipe Cuts
- **DB251.DBW2** → `L1L2_OKCut` (OK cuts count)
- **DB250.DBX3.4** → `L1L2_PipeDone` (OK bundle done signal)
- **DB260.DBX3.4** → `L2L1_AckPipeDone` (Acknowledgment)

### NDT Pipe Cuts
- **DB251.DBW6** → `L1L2_NDTCut` (NDT cuts count)
- **DB250.DBX6.0** → `L1L2_NDTBundleDone` (NDT bundle done signal)
- **DB260.DBX6.0** → `L2L1_AckNDTBundleDone` (Acknowledgment)

### Other Tags
- **DB251.DBW8** → `L1L2_PLC_PO_ID` (PO Plan ID)
- **DB251.DBW10** → `L1L2_PLC_Slit_ID` (Slit ID)
- **DB251.DBW28** → `L1L2_NDTBundle_PCs_Count` (NDT pieces in bundle)
- **DB251.DBW30** → `L1L2_NDTBundle_No` (Bundle number reference)

## Step 4: Enable PLC Polling

### Option A: Automatic (Recommended)
Set `EnablePolling: true` in `appsettings.json`. The service will:
- Connect to PLC on startup
- Poll every second (configurable)
- Automatically process OK and NDT cuts
- Form bundles automatically
- Print bundles when they complete
- Send acknowledgments to PLC

### Option B: Manual via API
Keep `EnablePolling: false` and use API endpoints:
```bash
# Connect to PLC
curl -X POST "http://localhost:5001/api/plc/connect?ipAddress=192.168.1.100&rack=0&slot=1"

# Process cuts manually
curl -X POST "http://localhost:5001/api/plc/process-cuts/1"
```

## Step 5: How It Works

### Automatic Flow (When Polling Enabled)

1. **Service Starts**: PLC Polling Service connects to PLC
2. **Continuous Polling**: Every second, service reads:
   - OK cuts count (DB251.DBW2)
   - NDT cuts count (DB251.DBW6)
   - OK Bundle Done signal (DB250.DBX3.4)
   - NDT Bundle Done signal (DB250.DBX6.0)

3. **Process Cuts**: When cuts increase:
   - Calculate new cuts (current - previous)
   - Process through bundle formation logic
   - Create/update bundles automatically

4. **Print Bundles**: When bundle completes:
   - Detect completed bundle (Status = 2)
   - Print tag via printer
   - Export to Excel
   - Update bundle status to 3 (Printed)
   - Send acknowledgment to PLC

5. **PLC Signals**: When PLC sends Done signal:
   - Find most recent completed bundle
   - Print immediately
   - Send acknowledgment back to PLC

## Step 6: Testing Physical Connection

### Test 1: Connection Test
```bash
# Start server
dotnet run --project NDTBundlePOC.UI.Web --urls "http://localhost:5001"

# Connect to PLC via API
curl -X POST "http://localhost:5001/api/plc/connect?ipAddress=YOUR_PLC_IP&rack=0&slot=1"

# Check status
curl "http://localhost:5001/api/plc/status"
```

### Test 2: Read Cuts
```bash
# Process cuts from PLC
curl -X POST "http://localhost:5001/api/plc/process-cuts/1"
```

### Test 3: Monitor Console
Watch the console output for:
- `✓ Connected to Siemens S7-1200 PLC at...`
- `Detected X new OK cuts`
- `Detected X new NDT cuts`
- `Printing OK bundle: ...`
- `Printing NDT bundle: ...`

## Step 7: Enable Real PLC Communication

### Update RealS7PLCService.cs

1. **Uncomment S7netplus using statements**:
```csharp
using S7netplus;
using S7netplus.Enums;
```

2. **Uncomment PLC connection code**:
```csharp
_plc = new Plc(CpuType.S71200, ipAddress, rack, slot);
_plc.Open();
_isConnected = _plc.IsConnected;
```

3. **Uncomment read/write operations**:
```csharp
// Read NDT cuts
var value = _plc.Read("DB251.DBW6");
if (value != null && value is ushort)
    return (int)(ushort)value;
```

4. **Remove simulation code** (the `// For POC:` sections)

## Step 8: Printer Configuration

### Ethernet Printer
```json
{
  "Printer": {
    "Address": "192.168.1.200",
    "Port": 9100,
    "UseNetwork": true
  }
}
```

### Serial Printer
```json
{
  "Printer": {
    "Address": "COM1",
    "Port": 9600,
    "UseNetwork": false
  }
}
```

## Troubleshooting

### PLC Connection Fails
- **Check IP Address**: Ping the PLC IP address
- **Check Network**: Ensure same subnet
- **Check Firewall**: Allow port 102 (S7 protocol)
- **Check PLC**: Ensure PLC is in RUN mode
- **Check Rack/Slot**: Verify in TIA Portal (usually 0,1 for S7-1200)

### No Cuts Detected
- **Check PLC Tags**: Verify DB251.DBW2 and DB251.DBW6 are being written by PLC
- **Check Polling**: Ensure `EnablePolling: true`
- **Check Logs**: Look for error messages in console

### Bundles Not Printing
- **Check Bundle Status**: Bundle must be Status = 2 (Completed)
- **Check Printer**: Verify printer IP/port is correct
- **Check Printer Connection**: Test printer separately
- **Check Telerik**: If using Telerik, ensure it's installed and configured

### Acknowledgment Not Working
- **Check PLC Tags**: Verify DB260 addresses are correct
- **Check Write Permissions**: Ensure L2 can write to DB260
- **Check Signal Reset**: PLC should reset Done signal after acknowledgment

## Production Deployment

1. **Update appsettings.json** with production PLC IP
2. **Enable polling**: Set `EnablePolling: true`
3. **Configure printer**: Set correct printer IP/port
4. **Test connection**: Verify PLC connection works
5. **Monitor logs**: Watch for errors
6. **Verify printing**: Test with actual bundles

## Safety Notes

- ⚠️ **Test First**: Always test in development before production
- ⚠️ **Backup Data**: Backup database before deployment
- ⚠️ **Monitor Closely**: Watch first few bundles to ensure correct operation
- ⚠️ **Error Handling**: All operations have try-catch blocks
- ⚠️ **PLC Safety**: Ensure PLC program handles acknowledgments correctly

## Quick Start Checklist

- [ ] S7netplus package installed
- [ ] PLC IP address configured in appsettings.json
- [ ] Printer IP/port configured
- [ ] EnablePolling set to true
- [ ] RealS7PLCService.cs updated (uncommented real code)
- [ ] PLC tags mapped correctly in TIA Portal
- [ ] Network connectivity verified (ping PLC)
- [ ] Test connection via API
- [ ] Monitor console for cut detection
- [ ] Verify bundles form correctly
- [ ] Verify printing works
- [ ] Verify Excel export works

## Example Workflow

1. **PLC sends**: OK cuts = 5, NDT cuts = 3
2. **Service detects**: New OK cuts = 5, New NDT cuts = 3
3. **Service processes**: Creates/updates bundles
4. **PLC sends**: OK Bundle Done signal
5. **Service detects**: Signal change
6. **Service prints**: OK bundle tag
7. **Service sends**: Acknowledgment to PLC
8. **PLC resets**: Done signal

This cycle repeats continuously while the service is running.

