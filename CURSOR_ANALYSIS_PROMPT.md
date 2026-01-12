# Cursor AI Prompt for Production Code Analysis

## Prompt to Use in Cursor

Copy and paste this prompt into Cursor's chat to analyze your production codebase:

```
I need to analyze the production source code to find configuration details for:

1. **Siemens S7-1200 PLC Connection:**
   - IP address configuration
   - Rack and slot numbers
   - Data block (DB) numbers and structure
   - Memory addresses/offsets for:
     * NDT cuts count
     * Bundle number
     * Batch number
     * PO Plan ID
     * Slit ID
     * PO end flag
   - Any S7 protocol communication code
   - PLC connection strings or configuration files

2. **Honeywell PD45S Printer Configuration:**
   - COM port name/configuration
   - Baud rate settings
   - Serial port parameters (data bits, parity, stop bits)
   - ZPL template or label format
   - Printer initialization code
   - Print job submission code
   - Any printer-specific settings

Please search for:
- Configuration files (appsettings.json, web.config, app.config, etc.)
- Service classes related to PLC communication
- Service classes related to printer/printing
- Constants or static configuration values
- Database tables or stored procedures that store this configuration
- Any code that references "S7", "PLC", "Siemens", "Honeywell", "PD45S", "printer", "COM", "serial port", "ZPL"
- Connection strings or network settings
- Data block definitions or PLC memory mapping

Provide:
1. Exact configuration values found
2. File paths where configuration is defined
3. Code snippets showing how these are used
4. Any related database schema or stored procedures
5. Environment-specific configurations (dev, staging, prod)
```

---

## Alternative Shorter Prompt

If the above is too long, use this shorter version:

```
Analyze the codebase to find:
1. Siemens S7-1200 PLC connection settings (IP, rack, slot, DB blocks, memory addresses)
2. Honeywell PD45S printer configuration (COM port, baud rate, ZPL template)
3. Where these are configured (config files, code, database)
4. How they're used in the codebase

Search for: PLC, S7, Siemens, Honeywell, PD45S, printer, COM port, serial, ZPL, data block, DB block
```

---

## Follow-up Prompts

After getting initial results, use these follow-up prompts:

### For PLC Details:
```
Show me the exact PLC data block structure:
- What DB block numbers are used?
- What are the memory offsets for each field?
- What data types are used (DINT, STRING, BOOL)?
- How is the PLC connection established?
- What error handling exists?
```

### For Printer Details:
```
Show me the printer implementation:
- What is the exact ZPL template/format?
- How is the serial port opened and configured?
- What label size and format is used?
- Are there any printer-specific commands or settings?
- How are print errors handled?
```

### For Configuration Location:
```
Where is the configuration stored:
- Is it in config files, database, or hardcoded?
- Are there different values for dev/staging/prod?
- How can these be changed without code changes?
- Are there any environment variables used?
```

---

## Code Search Queries

You can also use Cursor's code search with these queries:

### For PLC:
```
PLC connection IP address configuration
S7 protocol communication
Data block DB structure
PLC memory addresses
Siemens S7-1200
```

### For Printer:
```
Honeywell PD45S printer
COM port serial communication
ZPL label printing
Printer service implementation
Serial port configuration
```

### For Configuration:
```
appsettings.json PLC
web.config printer
Configuration.GetSection
Connection strings
```

---

## Expected Code Patterns to Look For

### PLC Code Patterns:
```csharp
// Look for these patterns:
- new Plc(...)
- S7netplus or S7Net library usage
- DB100, DB101, etc. (data block references)
- DBD0, DBD4, etc. (data block offsets)
- Read() or Write() methods with DB addresses
- IP address strings
- Rack/Slot parameters
```

### Printer Code Patterns:
```csharp
// Look for these patterns:
- new SerialPort(...)
- COM1, COM2, COM3 (port names)
- ZPL commands (^XA, ^FO, ^FD, ^XZ)
- PrintDocument or similar
- Honeywell, PD45S references
- Baud rate settings (9600, 19200, etc.)
```

### Configuration Patterns:
```csharp
// Look for these patterns:
- Configuration["PLC:IPAddress"]
- Configuration["Printer:Port"]
- appsettings.json sections
- Connection strings
- Environment variables
- Database configuration tables
```

---

## Example Analysis Output Format

After running the prompt, you should get information like:

```
PLC Configuration Found:
- IP Address: 192.168.1.50 (in appsettings.Production.json)
- Rack: 0, Slot: 1 (hardcoded in S7PLCService.cs)
- DB Block: 100 (for Mill 1)
- Offsets:
  * NDT_Cuts: DBD0
  * Bundle_No: DBD4 (STRING[20])
  * Batch_No: DBD24 (STRING[20])
  * NDT_Pcs: DBD44 (DINT)
  * PO_Plan_ID: DBD48 (DINT)
  * Slit_ID: DBD52 (DINT)
  * PO_End_Flag: DBD56.0 (BOOL)

Printer Configuration Found:
- Port: COM3 (from database table PrinterConfig)
- Baud Rate: 9600 (hardcoded)
- ZPL Template: Defined in HoneywellPrinterService.cs
- Label Size: 4" x 2"
```

---

## Tips for Better Results

1. **Start Broad**: Use the main prompt first to get an overview
2. **Narrow Down**: Use follow-up prompts for specific details
3. **Check Multiple Locations**: Configuration might be in:
   - Config files (appsettings.json, web.config)
   - Code (constants, service constructors)
   - Database (configuration tables)
   - Environment variables
   - Registry (Windows)
4. **Look for Variants**: Search for different spellings:
   - "PLC" vs "Plc" vs "plc"
   - "Honeywell" vs "HoneyWell"
   - "PD45S" vs "PD-45S" vs "pd45s"
5. **Check Comments**: Sometimes configuration details are in comments
6. **Look for TODO/FIXME**: Might indicate where configuration should be

---

## Quick Copy-Paste Prompt

```
Find all PLC and printer configuration in the codebase:
- Siemens S7-1200: IP, rack, slot, DB blocks, memory addresses
- Honeywell PD45S: COM port, baud rate, ZPL template
- Where configured: files, code, database
- How used: service classes, connection code
```

