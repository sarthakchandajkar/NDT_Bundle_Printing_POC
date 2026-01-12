# Physical PLC Connection - Quick Start

## ✅ What's Been Implemented

1. **RealS7PLCService** - Real PLC communication service (ready for S7netplus)
2. **OKBundleService** - Handles OK pipe bundle formation and printing
3. **PLCPollingService** - Background service that continuously reads from PLC
4. **Automatic Printing** - Prints both OK and NDT bundles automatically

## 🚀 Quick Setup (3 Steps)

### Step 1: Enable Real PLC Code
Edit `NDTBundlePOC.Core/Services/RealS7PLCService.cs`:
- Uncomment `using S7netplus;` and `using S7netplus.Enums;`
- Uncomment all PLC read/write code
- Remove simulation code

### Step 2: Configure Settings
Edit `NDTBundlePOC.UI.Web/appsettings.json`:
```json
{
  "PLC": {
    "IPAddress": "YOUR_PLC_IP",        // e.g., "192.168.1.100"
    "EnablePolling": true               // Enable automatic polling
  },
  "Printer": {
    "Address": "YOUR_PRINTER_IP",       // e.g., "192.168.1.200"
    "Port": 9100,
    "UseNetwork": true
  }
}
```

### Step 3: Start the Service
```bash
dotnet run --project NDTBundlePOC.UI.Web --urls "http://localhost:5001"
```

## 📋 What Happens Automatically

1. **Connects to PLC** on startup
2. **Polls every second** for:
   - OK cuts (DB251.DBW2)
   - NDT cuts (DB251.DBW6)
   - OK Bundle Done signal (DB250.DBX3.4)
   - NDT Bundle Done signal (DB250.DBX6.0)

3. **Processes cuts** automatically:
   - Forms OK bundles
   - Forms NDT bundles
   - Completes bundles when threshold reached

4. **Prints automatically**:
   - When bundle completes
   - When PLC sends Done signal
   - Exports to Excel
   - Sends acknowledgment to PLC

## 🔧 PLC Tag Mapping

| PLC Address | Tag Name | Purpose |
|------------|----------|---------|
| DB251.DBW2 | L1L2_OKCut | OK cuts count |
| DB251.DBW6 | L1L2_NDTCut | NDT cuts count |
| DB250.DBX3.4 | L1L2_PipeDone | OK bundle done signal |
| DB250.DBX6.0 | L1L2_NDTBundleDone | NDT bundle done signal |
| DB260.DBX3.4 | L2L1_AckPipeDone | OK acknowledgment |
| DB260.DBX6.0 | L2L1_AckNDTBundleDone | NDT acknowledgment |

## 📝 Testing

### Test Connection
```bash
# Connect via API
curl -X POST "http://localhost:5001/api/plc/connect?ipAddress=192.168.1.100"

# Check status
curl "http://localhost:5001/api/plc/status"
```

### Monitor Output
Watch console for:
- `✓ Connected to Siemens S7-1200 PLC at...`
- `Detected X new OK cuts`
- `Detected X new NDT cuts`
- `Printing OK bundle: ...`
- `Printing NDT bundle: ...`

## 📚 Full Documentation

- **PLC_CONNECTION_GUIDE.md** - Complete connection guide
- **PHYSICAL_PLC_SETUP.md** - Detailed setup instructions
- **TESTING_GUIDE.md** - Testing procedures

## ⚠️ Important Notes

1. **S7netplus**: Uncomment real PLC code in `RealS7PLCService.cs`
2. **Network**: Ensure PLC and computer are on same network
3. **Firewall**: Allow port 102 (S7 protocol)
4. **Printer**: Configure printer IP before testing
5. **Testing**: Test in development first!

## 🎯 Expected Behavior

When PLC sends pipe information:
1. Service detects new cuts
2. Forms bundles automatically
3. Prints tags when bundles complete
4. Exports to Excel
5. Sends acknowledgment to PLC

All happens automatically in the background! 🚀

