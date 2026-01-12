# NDT Bundle Tag Printing - POC Project

## Overview
This is an isolated POC project demonstrating NDT bundle tag printing functionality. 
**No production credentials or dependencies required.**

## Features
- ✅ NDT bundle formation logic (Scenario 1 & 2)
- ✅ Batch number generation in series
- ✅ **Telerik Reporting** for professional label printing
- ✅ Network/Ethernet printer support
- ✅ PLC communication (Siemens S7-1200)
- ✅ Excel export (EPPlus)
- ✅ Web-based UI

## Setup Instructions

1. **Prerequisites**
   - Visual Studio 2019 or later
   - .NET Framework 4.7.2

2. **Build**
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Run**
   ```bash
   dotnet run --project NDTBundlePOC.UI
   ```

## Usage

1. **Add NDT Cuts**: Enter number of NDT cuts and click "Add NDT Cuts"
   - This simulates pipes being processed
   - Bundle formation logic runs automatically

2. **Print Bundle Tag**: 
   - Select a completed bundle from the grid
   - Click "Print Selected Bundle Tag"
   - Tag is "printed" to a file (simulating printer)
   - Excel export is created automatically

## Output Locations

- **Printed Tags**: `Documents/NDT_Bundle_POC_Prints/`
- **Excel Exports**: `Documents/NDT_Bundle_POC_Exports/`

## Dummy Data

The project initializes with:
- 2 PO Plans (PO_001, PO_002)
- 1 Active Slit
- NDT Formation Chart (10 pieces per bundle default)

## Notes

- All data is in-memory (no database required)
- PLC communication: Configure IP address in `appsettings.json`
- Printer: Configure IP address or printer name in `appsettings.json`
- **Telerik Reporting**: See `TELERIK_SETUP.md` for installation (optional - falls back to file output)
- Excel exports saved to configured folder
- Perfect for demos and POC validation

## Configuration

See `CONFIGURATION_GUIDE.md` for detailed information on:
- Siemens S7-1200 PLC connection settings
- Honeywell PD45S printer configuration (Ethernet)
- Data block structure requirements
- Troubleshooting guide

## Telerik Reporting

The project uses **Telerik Reporting** for professional label printing:
- ✅ Programmatic report creation
- ✅ Report template support (.trdp/.trdx)
- ✅ Network and Windows printer support
- ⚠️ Requires Telerik Reporting license (see `TELERIK_SETUP.md`)

**Quick Start**: See `TELERIK_QUICK_START.md` for enabling Telerik Reporting.

