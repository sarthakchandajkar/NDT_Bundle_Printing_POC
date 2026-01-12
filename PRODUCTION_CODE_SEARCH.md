# Production Code Search Checklist

Use this checklist to systematically find PLC and printer configuration in your production codebase.

## Files to Check

### Configuration Files
- [ ] `appsettings.json`
- [ ] `appsettings.Production.json`
- [ ] `appsettings.Development.json`
- [ ] `web.config`
- [ ] `app.config`
- [ ] `appsettings.{Environment}.json`
- [ ] `.env` files
- [ ] `docker-compose.yml`
- [ ] `kubernetes` config files

### Code Files to Search
- [ ] `*PLC*.cs` - Any file with PLC in the name
- [ ] `*Printer*.cs` - Any file with Printer in the name
- [ ] `*S7*.cs` - S7 protocol related files
- [ ] `*Honeywell*.cs` - Honeywell related files
- [ ] `*Service*.cs` - Service classes
- [ ] `*Config*.cs` - Configuration classes
- [ ] `Program.cs` or `Startup.cs` - Application startup
- [ ] `*Controller*.cs` - API controllers

### Database
- [ ] Configuration tables
- [ ] Stored procedures with PLC/printer references
- [ ] Connection string configurations

## Search Terms

### PLC Related
```
S7
S7-1200
Siemens
PLC
Plc
plc
S7netplus
S7Net
DataBlock
DB100
DB101
DBD0
DBD4
rack
slot
IPAddress
```

### Printer Related
```
Honeywell
PD45S
PD-45S
printer
Printer
COM
SerialPort
COM1
COM2
COM3
baud
BaudRate
ZPL
^XA
^FO
^FD
^XZ
```

### Configuration Related
```
Configuration
config
appsettings
ConnectionString
GetSection
GetValue
```

## Code Patterns to Find

### PLC Connection Pattern
```csharp
// Look for:
new Plc(...)
Plc.Open()
Plc.Read("DB...")
Plc.Write("DB...")
Connect(ipAddress, rack, slot)
S7netplus
S7Net
```

### Printer Connection Pattern
```csharp
// Look for:
new SerialPort(...)
SerialPort.Open()
PrintDocument
ZPL
^XA
Honeywell
PD45S
```

### Configuration Reading Pattern
```csharp
// Look for:
Configuration["PLC:..."]
Configuration["Printer:..."]
GetSection("PLC")
GetSection("Printer")
_configuration.GetValue<string>("...")
```

## Database Queries to Run

If configuration is in database:

```sql
-- Find PLC configuration
SELECT * FROM Configuration WHERE Key LIKE '%PLC%' OR Key LIKE '%S7%' OR Key LIKE '%Siemens%'

-- Find Printer configuration
SELECT * FROM Configuration WHERE Key LIKE '%Printer%' OR Key LIKE '%Honeywell%' OR Key LIKE '%PD45%' OR Key LIKE '%COM%'

-- Find all configuration
SELECT * FROM Configuration WHERE Category IN ('PLC', 'Printer', 'Hardware')
```

## Environment-Specific Values

Check for:
- [ ] Development environment values
- [ ] Staging environment values
- [ ] Production environment values
- [ ] Environment variables
- [ ] Docker/Kubernetes secrets

## Quick Search Commands

### Using grep/ripgrep
```bash
# Search for PLC references
grep -r "PLC\|S7\|Siemens" --include="*.cs" --include="*.json" --include="*.config"

# Search for Printer references
grep -r "Honeywell\|PD45\|Printer\|COM" --include="*.cs" --include="*.json" --include="*.config"

# Search for configuration
grep -r "appsettings\|Configuration\[" --include="*.cs"
```

### Using Cursor Search
1. Press `Ctrl+Shift+F` (or `Cmd+Shift+F` on Mac)
2. Enter search terms
3. Filter by file type if needed
4. Check "Case Sensitive" if needed

## Expected Configuration Structure

### PLC Configuration
```json
{
  "PLC": {
    "IPAddress": "...",
    "Rack": 0,
    "Slot": 1,
    "DBBaseNumber": 100,
    "Timeout": 5000
  }
}
```

### Printer Configuration
```json
{
  "Printer": {
    "Port": "COM1",
    "BaudRate": 9600,
    "DataBits": 8,
    "Parity": "None",
    "StopBits": "One",
    "Timeout": 1000
  }
}
```

## What to Document

When you find the information, document:

1. **PLC Settings:**
   - IP Address: `___`
   - Rack: `___`
   - Slot: `___`
   - DB Block Number: `___`
   - Memory Offsets: `___`

2. **Printer Settings:**
   - COM Port: `___`
   - Baud Rate: `___`
   - Other Serial Settings: `___`
   - ZPL Template Location: `___`

3. **Configuration Location:**
   - File Path: `___`
   - Database Table: `___`
   - Code File: `___`

4. **Environment Differences:**
   - Dev: `___`
   - Staging: `___`
   - Prod: `___`

