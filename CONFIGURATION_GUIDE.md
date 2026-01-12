# Configuration Guide

## Ethernet Connection Information

For connecting via Ethernet cable, see **ETHERNET_CONNECTION_INFO.md** for detailed information about what you need from your PLC and printer. - PLC and Printer Connection

## Siemens S7-1200 PLC Connection

### Required Information

#### 1. Network Configuration
- **IP Address**: The IP address of your PLC (e.g., `192.168.1.100`)
- **Subnet Mask**: Usually `255.255.255.0` (for configuration reference)
- **Default Gateway**: Router IP if needed
- **Rack Number**: Typically `0` for S7-1200
- **Slot Number**: Typically `1` for S7-1200 (CPU slot)

#### 2. PLC Data Block (DB) Configuration

You need to define the following data blocks in your PLC program:

**DB100 (or DB100 + Mill_ID) - NDT Bundle Data Block**

| Offset | Data Type | Variable Name | Description | Size |
|--------|-----------|---------------|-------------|------|
| DBD0   | DINT/REAL | NDT_Cuts      | Current NDT cuts count | 4 bytes |
| DBD4   | STRING[20] | Bundle_No     | Bundle number | 20 bytes |
| DBD24  | STRING[20] | Batch_No      | Batch number | 20 bytes |
| DBD44  | DINT      | NDT_Pcs       | NDT pieces in bundle | 4 bytes |
| DBD48  | DINT      | PO_Plan_ID    | PO Plan ID | 4 bytes |
| DBD52  | DINT      | Slit_ID       | Slit ID | 4 bytes |
| DBD56  | BOOL      | PO_End_Flag   | PO ended flag | 1 byte |

**Example PLC Data Block Structure:**
```
DB100:
  DBD0  : NDT_Cuts (DINT)
  DBD4  : Bundle_No (STRING[20])
  DBD24 : Batch_No (STRING[20])
  DBD44 : NDT_Pcs (DINT)
  DBD48 : PO_Plan_ID (DINT)
  DBD52 : Slit_ID (DINT)
  DBD56.0 : PO_End_Flag (BOOL)
```

#### 3. PLC Program Requirements

Your PLC program should:
1. **Write NDT cuts count** to `DB100.DBD0` when new cuts are detected
2. **Read bundle information** from the application when bundle is completed:
   - Bundle Number → `DB100.DBD4`
   - Batch Number → `DB100.DBD24`
   - NDT Pieces → `DB100.DBD44`
3. **Provide PO Plan ID** at `DB100.DBD48`
4. **Provide Slit ID** at `DB100.DBD52`
5. **Set PO End Flag** at `DB100.DBD56.0` when PO is completed

#### 4. Network Settings in TIA Portal

1. Open TIA Portal
2. Go to **Device Configuration** → **Network View**
3. Select your S7-1200 CPU
4. Go to **Properties** → **Ethernet Address**
5. Set:
   - **IP Address**: (e.g., 192.168.1.100)
   - **Subnet Mask**: 255.255.255.0
   - **Enable "Permit access with PUT/GET communication from remote partner"**

#### 5. Firewall/Security Settings

- Ensure PLC firewall allows S7 communication (port 102)
- If using a firewall, allow TCP port 102
- Ensure the application server can reach the PLC network

---

## Honeywell PD45S Printer Connection

### Required Information

#### 1. Network Configuration (Ethernet - Recommended for Demo)

- **IP Address**: The IP address of your printer (e.g., `192.168.1.200`)
- **Port**: Typically `9100` for raw TCP/IP printing (standard for ZPL printers)
- **Connection Type**: Ethernet/TCP-IP
- **Protocol**: Raw TCP/IP (port 9100) or LPR (port 515)

#### 2. Serial Port Configuration (Alternative)

- **Port Name**: 
  - Windows: `COM1`, `COM2`, `COM3`, etc.
  - Linux/Mac: `/dev/ttyUSB0`, `/dev/ttyS0`, etc.
- **Baud Rate**: Typically `9600` (check printer manual)
- **Data Bits**: `8`
- **Parity**: `None`
- **Stop Bits**: `1`
- **Flow Control**: `None` (or `XON/XOFF` if required)

#### 3. Physical Connection

- **Ethernet Connection** (For Demo):
  - Connect printer to network via Ethernet cable
  - Configure printer IP address (via printer display or configuration utility)
  - Ensure printer and application server are on same network
- **Serial Connection** (Alternative):
  - USB (most common) - appears as COM port
  - Serial (RS-232) - direct serial connection
- **Cable Type**: Ethernet cable or USB/RS-232 cable

#### 3. Printer Settings

Check printer configuration:
- **Print Mode**: Direct thermal or thermal transfer
- **Label Size**: Width and length (affects ZPL template)
- **Print Density**: Usually 203 DPI (dots per inch)
- **Print Speed**: Adjustable in printer settings

#### 4. ZPL Template Configuration

The current implementation uses a basic ZPL template. You may need to adjust:

**Current Template Settings:**
- Label Size: 4" x 2" (400 dots length at 203 DPI)
- Font Sizes: 20-30 points
- Barcode: Code 128

**To Customize:**
1. Open `HoneywellPD45SPrinterService.cs`
2. Modify the `GenerateZPLCommands()` method
3. Adjust:
   - `^LL400` - Label length (in dots)
   - `^FO` commands - Field origin (X, Y positions)
   - `^A0N` - Font size and orientation
   - `^BY` - Barcode parameters

#### 5. Finding Your Printer IP Address (Ethernet)

1. **Via Printer Display**:
   - Navigate to printer menu → Network Settings → TCP/IP
   - Note the IP address

2. **Via Configuration Utility**:
   - Use Honeywell Printer Configuration Utility
   - Scan network for printers
   - Note the IP address

3. **Via Network Scanner**:
   - Use network scanning tool (Advanced IP Scanner, Angry IP Scanner)
   - Look for Honeywell device
   - Note the IP address

4. **Via Router Admin**:
   - Check DHCP client list in router
   - Look for Honeywell device
   - Note the assigned IP address

#### 6. Finding Your COM Port (Serial Connection - Alternative)

1. Open **Device Manager**
2. Expand **Ports (COM & LPT)**
3. Look for your printer (e.g., "USB Serial Port (COM3)")
4. Note the COM port number

#### 6. Testing Printer Connection

You can test the printer connection using:
- **ZPL Viewer**: Download ZPL viewer to preview labels
- **Printer Configuration Utility**: Honeywell provides configuration tools
- **Direct ZPL Test**: Send test ZPL commands via serial port

---

## Configuration File Setup

### Option 1: appsettings.json (Recommended)

Create `appsettings.json` in `NDTBundlePOC.UI.Web`:

**For Ethernet Connection (Demo):**
```json
{
  "PLC": {
    "IPAddress": "192.168.1.100",
    "Rack": 0,
    "Slot": 1,
    "DBBaseNumber": 100
  },
  "Printer": {
    "Address": "192.168.1.200",
    "Port": 9100,
    "UseNetwork": true
  },
  "Export": {
    "Path": "C:\\NDT_Exports"
  }
}
```

**For Serial Connection (Alternative):**
```json
{
  "PLC": {
    "IPAddress": "192.168.1.100",
    "Rack": 0,
    "Slot": 1,
    "DBBaseNumber": 100
  },
  "Printer": {
    "Address": "COM1",
    "Port": 9600,
    "UseNetwork": false
  },
  "Export": {
    "Path": "C:\\NDT_Exports"
  }
}
```

### Option 2: Environment Variables

Set environment variables:
- `PLC__IPAddress=192.168.1.100`
- `PLC__Rack=0`
- `PLC__Slot=1`
- `Printer__Port=COM1`
- `Printer__BaudRate=9600`

### Option 3: Hardcode in Program.cs (Current)

Currently configured in `Program.cs`:
```csharp
var plcIpAddress = builder.Configuration["PLC:IPAddress"] ?? "192.168.1.100";
var printerPort = builder.Configuration["Printer:Port"] ?? "COM1";
```

---

## Testing Checklist

### PLC Connection Test

- [ ] PLC is powered on
- [ ] PLC IP address is configured correctly
- [ ] Application server can ping PLC IP
- [ ] Firewall allows port 102 (S7 protocol)
- [ ] DB blocks are created in PLC program
- [ ] DB blocks are accessible (not optimized)
- [ ] Test connection via TIA Portal first

### Printer Connection Test

- [ ] Printer is powered on
- [ ] Printer is connected via USB/Serial
- [ ] COM port is identified in Device Manager
- [ ] No other application is using the COM port
- [ ] Printer has labels loaded
- [ ] Test print from printer configuration utility

---

## Troubleshooting

### PLC Connection Issues

**Problem**: Cannot connect to PLC
- **Solution**: 
  - Check IP address is correct
  - Verify network connectivity (ping PLC)
  - Check firewall settings
  - Ensure "Permit PUT/GET" is enabled in TIA Portal
  - Verify rack and slot numbers

**Problem**: Can connect but cannot read/write data
- **Solution**:
  - Verify DB block numbers match
  - Check data types match (DINT, STRING, etc.)
  - Ensure DB blocks are not optimized
  - Verify offsets are correct

### Printer Connection Issues

**Problem**: Cannot open COM port
- **Solution**:
  - Check COM port number is correct
  - Verify no other application is using the port
  - Check USB cable connection
  - Try different COM port

**Problem**: Printer receives data but doesn't print
- **Solution**:
  - Check ZPL syntax is correct
  - Verify label size matches template
  - Check printer has labels loaded
  - Test with simple ZPL command first

---

## Quick Reference

### PLC Connection String Format
```
IP: 192.168.1.100
Rack: 0
Slot: 1
DB Block: 100 + Mill_ID
```

### Printer Connection String Format

**Ethernet (Network):**
```
Address: 192.168.1.200 (Printer IP)
Port: 9100 (Raw TCP/IP printing)
Protocol: TCP/IP
```

**Serial (Alternative):**
```
Address: COM1 (Windows) or /dev/ttyUSB0 (Linux)
Port: 9600 (Baud Rate)
Data Bits: 8
Parity: None
Stop Bits: 1
```

### Default Values in Code
- **PLC IP**: `192.168.1.100`
- **PLC Rack**: `0`
- **PLC Slot**: `1`
- **Printer Address**: `192.168.1.200` (Ethernet) or `COM1` (Serial)
- **Printer Port**: `9100` (Ethernet) or `9600` (Serial Baud Rate)
- **Use Network**: `true` (Ethernet connection)
- **Export Path**: `Documents/NDT_Bundle_POC_Exports`

---

## Next Steps

1. **Gather PLC Information**:
   - Get PLC IP address from network administrator
   - Verify DB block structure with PLC programmer
   - Test connection using TIA Portal

2. **Gather Printer Information**:
   - Identify COM port in Device Manager
   - Check printer manual for baud rate
   - Test printer with ZPL viewer

3. **Update Configuration**:
   - Modify `Program.cs` or create `appsettings.json`
   - Update DB block numbers if different
   - Adjust ZPL template if needed

4. **Test Connections**:
   - Test PLC connection first
   - Test printer connection separately
   - Then test integrated workflow

