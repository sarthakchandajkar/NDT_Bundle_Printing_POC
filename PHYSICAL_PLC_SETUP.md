# Physical PLC Connection Setup Guide

## Overview
This guide explains how to connect a physical Siemens S7-1200 PLC to automatically receive pipe information and print both OK and NDT bundle tags.

## Architecture

```
Physical PLC (S7-1200)
    ↓ (Ethernet)
Your Application
    ↓ (Reads pipe cuts)
Bundle Formation Logic
    ↓ (Forms bundles)
Automatic Printing
    ↓ (Ethernet/Serial)
Physical Printer (Honeywell PD45S)
```

## Step 1: Enable Real PLC Communication

### Update RealS7PLCService.cs

1. **Uncomment S7netplus using statements** (lines 2-6):
```csharp
using S7netplus;
using S7netplus.Enums;
```

2. **Uncomment PLC connection code** (around line 30-50):
```csharp
_plc = new Plc(CpuType.S71200, ipAddress, rack, slot);
_plc.Open();
_isConnected = _plc.IsConnected;
```

3. **Uncomment all read/write operations** throughout the file

4. **Remove simulation code** (all `// For POC:` sections)

## Step 2: Configure appsettings.json

```json
{
  "PLC": {
    "IPAddress": "192.168.1.100",      // Your PLC's actual IP
    "Rack": 0,                          // Usually 0 for S7-1200
    "Slot": 1,                          // Usually 1 for S7-1200
    "MillId": 1,                        // Mill ID (1, 2, 3, etc.)
    "EnablePolling": true,              // Enable automatic polling
    "PollingIntervalMs": 1000           // Poll every 1 second
  },
  "Printer": {
    "Address": "192.168.1.200",         // Your printer's IP
    "Port": 9100,                        // TCP port for raw printing
    "UseNetwork": true                   // true for Ethernet
  }
}
```

## Step 3: PLC Tag Configuration

### Required PLC Tags (Configure in TIA Portal)

#### OK Pipe Tags
- **DB251.DBW2** → OK cuts count (L1L2_OKCut)
- **DB250.DBX3.4** → OK bundle done signal (L1L2_PipeDone)
- **DB260.DBX3.4** → OK acknowledgment (L2L1_AckPipeDone)

#### NDT Pipe Tags
- **DB251.DBW6** → NDT cuts count (L1L2_NDTCut)
- **DB250.DBX6.0** → NDT bundle done signal (L1L2_NDTBundleDone)
- **DB260.DBX6.0** → NDT acknowledgment (L2L1_AckNDTBundleDone)

#### Additional Tags
- **DB251.DBW8** → PO Plan ID (L1L2_PLC_PO_ID)
- **DB251.DBW10** → Slit ID (L1L2_PLC_Slit_ID)
- **DB251.DBW28** → NDT bundle pieces count (L1L2_NDTBundle_PCs_Count)
- **DB251.DBW30** → Bundle number reference (L1L2_NDTBundle_No)

## Step 4: How It Works

### Automatic Flow (When EnablePolling = true)

1. **Service Starts**:
   - Connects to PLC automatically
   - Starts background polling service
   - Polls every 1 second (configurable)

2. **Reads from PLC**:
   - OK cuts count (DB251.DBW2)
   - NDT cuts count (DB251.DBW6)
   - OK Bundle Done signal (DB250.DBX3.4)
   - NDT Bundle Done signal (DB250.DBX6.0)

3. **Processes Cuts**:
   - Detects when cuts increase
   - Calculates new cuts (current - previous)
   - Processes through bundle formation logic
   - Creates/updates bundles automatically

4. **Prints Bundles**:
   - When bundle completes (Status = 2)
   - Automatically prints tag
   - Exports to Excel
   - Updates status to 3 (Printed)
   - Sends acknowledgment to PLC

5. **PLC Signals**:
   - When PLC sends Done signal
   - Service finds most recent completed bundle
   - Prints immediately
   - Sends acknowledgment (DB260.DBX3.4 or DB260.DBX6.0)

## Step 5: Testing Physical Connection

### Test 1: Verify Network Connectivity
```bash
# Ping PLC
ping 192.168.1.100

# Test port 102 (S7 protocol)
telnet 192.168.1.100 102
```

### Test 2: Connect via Application
```bash
# Start server
dotnet run --project NDTBundlePOC.UI.Web --urls "http://localhost:5001"

# Connect to PLC via API
curl -X POST "http://localhost:5001/api/plc/connect?ipAddress=192.168.1.100&rack=0&slot=1"

# Check status
curl "http://localhost:5001/api/plc/status"
```

### Test 3: Monitor Console Output
Watch for:
- `✓ Connected to Siemens S7-1200 PLC at 192.168.1.100`
- `Detected X new OK cuts`
- `Detected X new NDT cuts`
- `Printing OK bundle: PO_001OK001`
- `Printing NDT bundle: PO_001NDT001`

## Step 6: Enable Automatic Polling

### Option A: Via appsettings.json (Recommended)
```json
{
  "PLC": {
    "EnablePolling": true
  }
}
```

### Option B: Via Environment Variable
```bash
export PLC__EnablePolling=true
dotnet run --project NDTBundlePOC.UI.Web
```

## Step 7: Printer Setup

### Ethernet Printer
1. Connect printer to network
2. Configure printer IP (e.g., 192.168.1.200)
3. Update `appsettings.json`:
```json
{
  "Printer": {
    "Address": "192.168.1.200",
    "Port": 9100,
    "UseNetwork": true
  }
}
```

### Test Printer Connection
```bash
# Test printer port
telnet 192.168.1.200 9100
```

## Complete Workflow Example

### Scenario: PLC sends 5 OK cuts and 3 NDT cuts

1. **PLC Updates**:
   - DB251.DBW2 = 5 (OK cuts)
   - DB251.DBW6 = 3 (NDT cuts)

2. **Service Detects** (after 1 second):
   - Previous OK cuts = 0, Current = 5 → New = 5
   - Previous NDT cuts = 0, Current = 3 → New = 3

3. **Service Processes**:
   - Creates/updates OK bundle with 5 pieces
   - Creates/updates NDT bundle with 3 pieces

4. **PLC Sends Signal** (when bundle complete):
   - DB250.DBX3.4 = true (OK Bundle Done)
   - DB250.DBX6.0 = true (NDT Bundle Done)

5. **Service Responds**:
   - Finds completed bundle
   - Prints tag
   - Exports to Excel
   - Sets DB260.DBX3.4 = true (Acknowledge OK)
   - Sets DB260.DBX6.0 = true (Acknowledge NDT)

6. **PLC Resets**:
   - DB250.DBX3.4 = false
   - DB250.DBX6.0 = false

## Troubleshooting

### PLC Connection Issues
- **Cannot connect**: Check IP address, network, firewall
- **Connection drops**: Check network stability, PLC status
- **Timeout errors**: Increase timeout in S7netplus settings

### No Cuts Detected
- **Check PLC tags**: Verify DB251.DBW2 and DB251.DBW6 are being written
- **Check polling**: Ensure EnablePolling = true
- **Check logs**: Look for error messages

### Bundles Not Printing
- **Check bundle status**: Must be Status = 2 (Completed)
- **Check printer**: Verify printer IP and connection
- **Check Telerik**: If using Telerik, ensure it's configured

### Acknowledgment Not Working
- **Check PLC tags**: Verify DB260 addresses are correct
- **Check write permissions**: Ensure application can write to DB260
- **Check signal reset**: PLC should reset Done signal after acknowledgment

## Production Deployment Checklist

- [ ] S7netplus package installed and configured
- [ ] RealS7PLCService.cs updated (real code uncommented)
- [ ] PLC IP address configured correctly
- [ ] PLC tags mapped in TIA Portal
- [ ] Printer IP/port configured
- [ ] EnablePolling set to true
- [ ] Network connectivity verified
- [ ] Test connection successful
- [ ] Test cut detection working
- [ ] Test bundle formation working
- [ ] Test printing working
- [ ] Test acknowledgment working
- [ ] Monitor logs for errors
- [ ] Backup database before deployment

## Safety and Best Practices

1. **Test First**: Always test in development environment
2. **Monitor Closely**: Watch first few bundles carefully
3. **Error Handling**: All operations have try-catch blocks
4. **Logging**: Check logs regularly for issues
5. **Backup**: Backup database before production deployment
6. **Network**: Ensure stable network connection
7. **PLC Safety**: Verify PLC program handles signals correctly

## Quick Start Command

```bash
# 1. Update appsettings.json with your PLC IP
# 2. Set EnablePolling = true
# 3. Start server
dotnet run --project NDTBundlePOC.UI.Web --urls "http://localhost:5001"

# The service will:
# - Connect to PLC automatically
# - Poll every second
# - Process cuts automatically
# - Print bundles automatically
```

## Monitoring

Watch console output for:
- Connection status
- Cut detection
- Bundle formation
- Print operations
- Error messages

All operations are logged and can be monitored in real-time.

