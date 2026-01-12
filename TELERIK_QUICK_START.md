# Telerik Reporting Quick Start

## Current Status

✅ **Telerik Reporting service is implemented** but **disabled** until Telerik Reporting is installed.

The code uses conditional compilation (`#if TELERIK_REPORTING`) so it will compile without Telerik, but will fall back to file output.

## To Enable Telerik Reporting

### Step 1: Install Telerik Reporting

Choose one method:

**Option A: NuGet Feed (Recommended)**
1. Get your Telerik NuGet feed URL from: https://www.telerik.com/account
2. Edit `NuGet.config` and uncomment/add your feed URL
3. Uncomment the PackageReference in `NDTBundlePOC.Core.csproj`
4. Run: `dotnet restore`

**Option B: Local Assemblies**
1. Copy Telerik DLLs to `NDTBundlePOC.Core/lib/`
2. Uncomment the Reference section in `NDTBundlePOC.Core.csproj`
3. Run: `dotnet restore`

### Step 2: Enable Conditional Compilation

Edit `NDTBundlePOC.Core.csproj`:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);TELERIK_REPORTING</DefineConstants>
</PropertyGroup>
```

### Step 3: Uncomment Using Statements

Edit `TelerikReportingPrinterService.cs`:

```csharp
// Change from:
// using Telerik.Reporting;

// To:
using Telerik.Reporting;
```

### Step 4: Rebuild

```bash
dotnet build
```

## Features

✅ **Programmatic Report Creation**: Creates NDT bundle tag report dynamically
✅ **Report Template Support**: Can load `.trdp` or `.trdx` files
✅ **Network Printing**: Sends rendered report to network printer
✅ **Windows Printer Support**: Can print to Windows printers
✅ **Image Rendering**: Renders report as image (PNG) for printing

## Configuration

In `appsettings.json`:

```json
{
  "Printer": {
    "Address": "192.168.1.200",      // Printer IP or Windows printer name
    "Port": 9100,                     // TCP port (network) or 0 (Windows)
    "UseNetwork": true,               // true for network, false for Windows
    "ReportTemplatePath": ""          // Path to .trdp/.trdx file (optional)
  }
}
```

## Report Template (Optional)

You can create a report template using Telerik Report Designer:

1. Open Telerik Report Designer
2. Create new report (4" x 2" label size)
3. Add fields:
   - BundleNo
   - BatchNo
   - PO_No
   - NDT_Pcs
   - Pipe_Grade
   - Pipe_Size
   - Pipe_Len
   - BundleStartTime
   - BundleEndTime
4. Save as `.trdp` or `.trdx`
5. Set path in `appsettings.json`

## Current Behavior (Without Telerik)

- ✅ Code compiles successfully
- ✅ Prints to file (text format)
- ⚠️ Telerik Reporting features disabled
- ⚠️ Network/Windows printing disabled (falls back to file)

## After Installing Telerik

- ✅ Full Telerik Reporting functionality
- ✅ Network printing to Ethernet printer
- ✅ Windows printer support
- ✅ Professional label formatting
- ✅ Report template support

See `TELERIK_SETUP.md` for detailed installation instructions.

