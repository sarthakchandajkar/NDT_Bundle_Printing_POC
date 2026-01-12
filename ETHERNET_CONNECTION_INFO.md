# Ethernet Connection Information Required

## Overview
To connect your PLC and printer via Ethernet cable, you need specific network configuration information for both devices.

## 🔌 PLC (Siemens S7-1200) - Required Information

### Essential Information

1. **IP Address** (Required)
   - Example: `192.168.1.100`
   - Where to find: TIA Portal → Device Configuration → Network View → Select PLC → Properties → Ethernet Address
   - Or: PLC's HMI/Display → Network Settings

2. **Subnet Mask** (Required)
   - Example: `255.255.255.0`
   - Usually: `255.255.255.0` for local networks
   - Must match your computer's subnet mask

3. **Default Gateway** (Optional, but recommended)
   - Example: `192.168.1.1`
   - Only needed if PLC needs to communicate outside local network

4. **Rack Number** (Required for S7 protocol)
   - Usually: `0` for S7-1200
   - Where to find: TIA Portal → Device Configuration

5. **Slot Number** (Required for S7 protocol)
   - Usually: `1` for S7-1200
   - Where to find: TIA Portal → Device Configuration

### Additional PLC Information (For Configuration)

6. **S7 Protocol Port** (Standard)
   - Default: `102`
   - Usually doesn't need to be changed

7. **CPU Type** (For code)
   - Value: `S71200` (already in code)
   - No configuration needed

### How to Find PLC IP Address

#### Method 1: TIA Portal
1. Open TIA Portal
2. Go to **Device Configuration**
3. Select your PLC device
4. Go to **Properties** → **General** → **Ethernet Address**
5. View or set IP address

#### Method 2: PLC Display/HMI
1. Navigate to network settings on PLC display
2. View current IP configuration

#### Method 3: Network Scanner
1. Use network scanning tool (Advanced IP Scanner, Angry IP Scanner)
2. Look for Siemens devices
3. Identify your PLC by MAC address or device name

#### Method 4: Ask Network Administrator
- If PLC is on company network, IT department may have the IP

## 🖨️ Printer (Honeywell PD45S) - Required Information

### Essential Information

1. **IP Address** (Required)
   - Example: `192.168.1.200`
   - Where to find: Printer menu → Network Settings → TCP/IP
   - Or: Print network configuration page from printer

2. **TCP Port** (Required)
   - Default: `9100` (standard for raw TCP/IP printing)
   - Alternative: `515` (LPR/LPD protocol)
   - Usually: `9100` for ZPL/raw printing

3. **Subnet Mask** (Required for network setup)
   - Example: `255.255.255.0`
   - Must match your computer's subnet mask

4. **Default Gateway** (Optional)
   - Example: `192.168.1.1`
   - Only needed if printer needs internet access

### Additional Printer Information

5. **Printer Protocol** (For configuration)
   - Options: `Raw TCP/IP` (port 9100) or `LPR` (port 515)
   - Recommended: `Raw TCP/IP` on port `9100`

6. **Printer Model** (For reference)
   - Model: `Honeywell PD45S`
   - Already configured in code

### How to Find Printer IP Address

#### Method 1: Printer Menu
1. Press **Menu** button on printer
2. Navigate to **Network Settings** or **TCP/IP Settings**
3. View **IP Address**

#### Method 2: Print Configuration Page
1. Press and hold specific button combination (check printer manual)
2. Printer prints network configuration page
3. Look for "IP Address" on the page

#### Method 3: Printer Web Interface
1. If printer has web interface, access via browser
2. Default URL might be: `http://[printer-ip]` or `http://printer.local`
3. View network settings

#### Method 4: Network Scanner
1. Use network scanning tool
2. Look for Honeywell devices
3. Identify by MAC address (usually starts with Honeywell OUI)

#### Method 5: Router/Network Admin
- Check router's connected devices list
- Look for Honeywell or printer device

## 📋 Configuration Checklist

### Before Connecting

- [ ] **PLC IP Address**: `_________________`
- [ ] **PLC Subnet Mask**: `_________________` (usually 255.255.255.0)
- [ ] **PLC Rack**: `0` (standard for S7-1200)
- [ ] **PLC Slot**: `1` (standard for S7-1200)
- [ ] **Printer IP Address**: `_________________`
- [ ] **Printer Port**: `9100` (standard)
- [ ] **Printer Subnet Mask**: `_________________` (usually 255.255.255.0)

### Network Requirements

- [ ] **Same Subnet**: PLC, Printer, and Computer must be on same subnet
- [ ] **IP Range**: All devices in same IP range (e.g., 192.168.1.x)
- [ ] **No IP Conflicts**: Each device has unique IP address
- [ ] **Ethernet Cable**: Use standard Ethernet cable (Cat5e or better)

## 🔧 Network Configuration Example

### Scenario: Direct Connection (PLC ↔ Computer ↔ Printer)

If connecting directly via Ethernet:

```
Computer:  192.168.1.10  (Subnet: 255.255.255.0)
PLC:       192.168.1.100 (Subnet: 255.255.255.0)
Printer:   192.168.1.200 (Subnet: 255.255.255.0)
```

### Scenario: Network Switch (All devices on network)

```
Network:   192.168.1.0/24
Gateway:   192.168.1.1

Computer:  192.168.1.10
PLC:       192.168.1.100
Printer:   192.168.1.200
```

## 📝 Configuration in appsettings.json

Once you have the information, update `appsettings.json`:

```json
{
  "PLC": {
    "IPAddress": "192.168.1.100",    // ← Your PLC IP
    "Rack": 0,                        // ← Usually 0
    "Slot": 1,                        // ← Usually 1
    "MillId": 1,
    "EnablePolling": true,
    "PollingIntervalMs": 1000
  },
  "Printer": {
    "Address": "192.168.1.200",       // ← Your Printer IP
    "Port": 9100,                     // ← Usually 9100
    "UseNetwork": true                // ← true for Ethernet
  }
}
```

## 🧪 Testing Connectivity

### Test 1: Ping PLC
```bash
ping 192.168.1.100
```
**Expected**: Should receive replies

### Test 2: Ping Printer
```bash
ping 192.168.1.200
```
**Expected**: Should receive replies

### Test 3: Test PLC Port (S7 Protocol)
```bash
telnet 192.168.1.100 102
```
**Expected**: Connection attempt (may timeout, that's OK - means port is open)

### Test 4: Test Printer Port
```bash
telnet 192.168.1.200 9100
```
**Expected**: Connection opens (press Ctrl+] then type 'quit' to exit)

## ⚠️ Common Issues and Solutions

### Issue: Cannot ping PLC
**Solutions**:
- Check Ethernet cable is connected
- Verify PLC IP address is correct
- Check PLC is powered on and in RUN mode
- Verify subnet mask matches
- Check firewall settings

### Issue: Cannot ping Printer
**Solutions**:
- Check Ethernet cable is connected
- Verify printer IP address is correct
- Check printer is powered on
- Verify subnet mask matches
- Try printing network config page from printer

### Issue: IP Address Conflict
**Solutions**:
- Ensure each device has unique IP
- Check router's DHCP range
- Manually assign static IPs outside DHCP range

### Issue: Wrong Subnet
**Solutions**:
- All devices must have same subnet mask
- All devices must be in same IP range
- Example: All 192.168.1.x with mask 255.255.255.0

## 📞 Where to Get This Information

### For PLC:
1. **TIA Portal** (Best method)
   - Device Configuration → Network View
   - Properties → Ethernet Address

2. **PLC Programmer/Engineer**
   - They should have the IP address documented

3. **Network Administrator**
   - If on company network

4. **PLC Documentation**
   - Check project documentation

### For Printer:
1. **Printer Menu** (Best method)
   - Network Settings → TCP/IP

2. **Printer Configuration Page**
   - Print network settings

3. **Printer Manual**
   - Default IP or setup instructions

4. **IT Department**
   - If printer is on company network

## 🎯 Quick Reference

### Minimum Required Information

**PLC**:
- IP Address: `_____________`
- Rack: `0` (standard)
- Slot: `1` (standard)

**Printer**:
- IP Address: `_____________`
- Port: `9100` (standard)

### Once You Have This:
1. Update `appsettings.json` with the IP addresses
2. Ensure all devices are on same network
3. Test connectivity (ping both devices)
4. Start the application
5. Connect via API or enable automatic polling

## 📚 Next Steps

After gathering this information:
1. See `PHYSICAL_PLC_SETUP.md` for connection setup
2. See `PLC_CONNECTION_GUIDE.md` for detailed integration
3. Test connection before enabling automatic polling

