# Ethernet Connection - Quick Reference

## 📋 Information You Need

### PLC (Siemens S7-1200)
1. **IP Address** - e.g., `192.168.1.100`
2. **Rack** - Usually `0`
3. **Slot** - Usually `1`

### Printer (Honeywell PD45S)
1. **IP Address** - e.g., `192.168.1.200`
2. **Port** - Usually `9100`

## 🔍 How to Find

### PLC IP Address
- **TIA Portal**: Device Configuration → Network View → Properties → Ethernet Address
- **PLC Display**: Network Settings menu
- **Network Scanner**: Scan for Siemens devices

### Printer IP Address
- **Printer Menu**: Network Settings → TCP/IP
- **Print Config Page**: Print network settings from printer
- **Network Scanner**: Scan for Honeywell devices

## ⚙️ Configuration

Update `appsettings.json`:
```json
{
  "PLC": {
    "IPAddress": "YOUR_PLC_IP_HERE"
  },
  "Printer": {
    "Address": "YOUR_PRINTER_IP_HERE",
    "Port": 9100,
    "UseNetwork": true
  }
}
```

## ✅ Requirements

- All devices (Computer, PLC, Printer) must be on **same subnet**
- Example: All `192.168.1.x` with subnet mask `255.255.255.0`
- Each device must have **unique IP address**
- Use **standard Ethernet cable** (Cat5e or better)

## 🧪 Test Connection

```bash
# Test PLC
ping YOUR_PLC_IP

# Test Printer
ping YOUR_PRINTER_IP
```

Both should respond successfully.

## 📚 Full Details

See **ETHERNET_CONNECTION_INFO.md** for complete information.

