using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NDTBundlePOC.Core.Services;
using NDTBundlePOC.UI.Web.Hubs;
using NDTBundlePOC.UI.Web.Services;

// ============================================
// NDT BUNDLE PRINTING POC - HARDCODED CONFIG
// ============================================
// All printer and PLC values are hardcoded in this file.
// Update the values below to match your hardware:
//   - PLC IP Address (Siemens S7-1200)
//   - Printer IP Address (Honeywell PD45S)
//   - Printer Port (default: 9100 for ZPL)
//
// The application will:
//   1. Connect to PLC at the hardcoded IP address
//   2. Poll for NDT pipe counts every 1 second
//   3. Create bundles when 5 NDT pipes are counted
//   4. Print tags directly to Honeywell printer via ZPL
// ============================================

var builder = WebApplication.CreateBuilder(args);

// ============================================
// HARDCODED CONFIGURATION VALUES
// ============================================
// TODO: Update these values with your actual hardware addresses

// PLC Configuration (Siemens S7-300)
var plcIpAddress = "192.168.0.13";  // Your PLC IP address
var plcRack = 0;                     // Rack number
var plcSlot = 2;                     // Slot number for S7-300

// Honeywell PD45S Printer Configuration
var printerAddress = "192.168.0.125";  // Your printer IP address
var printerPort = 9100;                 // Standard port for raw printing (ZPL)
var useNetwork = true;                  // true = Ethernet, false = Serial

// Service Configuration
var enablePLCPolling = true;            // Enable automatic PLC polling for NDT cuts
var pollingIntervalMs = 1000;          // Poll PLC every 1 second
var millId = 1;                        // Mill ID (1 = Mill 1)
var heartbeatPollingIntervalMs = 750;  // Heartbeat check every 750ms
var enableHeartbeatMonitoring = true;   // Enable heartbeat monitoring

// Export/Output Path
var exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NDT_Bundle_Exports");

// Add services
builder.Services.AddSingleton<IDataRepository, InMemoryDataRepository>();
builder.Services.AddSingleton<INDTBundleService, NDTBundleService>();
builder.Services.AddSingleton<IOKBundleService, OKBundleService>();

// Use RealS7PLCService for physical PLC connection
// Hardcoded values: IP=192.168.0.74, Rack=0, Slot=1
builder.Services.AddSingleton<IPLCService>(sp => new RealS7PLCService());

// Use ZPL-based printing for Honeywell PD45S printer (sends ZPL commands directly to printer)
// Hardcoded values: IP=192.168.0.125, Port=9100, Network=true
builder.Services.AddSingleton<IPrinterService>(sp => 
    new HoneywellPD45SPrinterService(
        printerAddress: printerAddress,  // Hardcoded: 192.168.0.125
        printerPort: printerPort,        // Hardcoded: 9100
        useNetwork: useNetwork           // Hardcoded: true (Ethernet)
    ));

// Alternative: Use Telerik Reporting for printing (generates images - use for Windows printers or if you have Telerik Reporting installed)
// builder.Services.AddSingleton<IPrinterService>(sp => 
//     new TelerikReportingPrinterService(printerAddress, printerPort, useNetwork, reportTemplatePath));

builder.Services.AddSingleton<ExcelExportService>(sp => new ExcelExportService(exportPath));

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Add Heartbeat Notifier (SignalR implementation)
builder.Services.AddSingleton<IHeartbeatNotifier, HeartbeatNotifier>();

// Add Pipe Counting Activity Service (register both interfaces)
builder.Services.AddSingleton<PipeCountingActivityService>();
builder.Services.AddSingleton<IPipeCountingActivityService>(sp => sp.GetRequiredService<PipeCountingActivityService>());
builder.Services.AddSingleton<IPipeCountingActivityServiceExtended>(sp => sp.GetRequiredService<PipeCountingActivityService>());

// Add PLC Heartbeat Monitor Service as background service (if enabled)
if (enableHeartbeatMonitoring)
{
    builder.Services.AddHostedService<PLCHeartbeatMonitorService>(sp =>
    {
        var plcService = sp.GetRequiredService<IPLCService>();
        var notifier = sp.GetRequiredService<IHeartbeatNotifier>();
        var logger = sp.GetRequiredService<ILogger<PLCHeartbeatMonitorService>>();
        
        return new PLCHeartbeatMonitorService(
            plcService,
            logger,
            notifier,
            plcIpAddress,
            heartbeatPollingIntervalMs
        );
    });
}

// Add PLC Polling Service as background service (if enabled)
if (enablePLCPolling)
{
    builder.Services.AddHostedService<PLCPollingService>(sp =>
    {
        var plcService = sp.GetRequiredService<IPLCService>();
        var ndtBundleService = sp.GetRequiredService<INDTBundleService>();
        var okBundleService = sp.GetRequiredService<IOKBundleService>();
        var printerService = sp.GetRequiredService<IPrinterService>();
        var excelService = sp.GetRequiredService<ExcelExportService>();
        var activityService = sp.GetRequiredService<IPipeCountingActivityService>();
        var logger = sp.GetRequiredService<ILogger<PLCPollingService>>();
        
        return new PLCPollingService(
            plcService,
            ndtBundleService,
            okBundleService,
            printerService,
            excelService,
            logger,
            millId,
            pollingIntervalMs,
            activityService
        );
    });
}

var app = builder.Build();

// Initialize dummy data
var repository = app.Services.GetRequiredService<IDataRepository>();
((InMemoryDataRepository)repository).InitializeDummyData();

// Auto-connect to PLC if configured (for polling or heartbeat monitoring)
var plcService = app.Services.GetRequiredService<IPLCService>();
if ((enablePLCPolling || enableHeartbeatMonitoring) && !string.IsNullOrEmpty(plcIpAddress))
{
    Console.WriteLine("==========================================");
    Console.WriteLine("  HARDCODED CONFIGURATION");
    Console.WriteLine("==========================================");
    Console.WriteLine($"PLC IP Address:    {plcIpAddress}");
    Console.WriteLine($"PLC Rack:         {plcRack}");
    Console.WriteLine($"PLC Slot:         {plcSlot}");
    Console.WriteLine($"Printer IP:       {printerAddress}");
    Console.WriteLine($"Printer Port:     {printerPort}");
    Console.WriteLine($"Printer Mode:     {(useNetwork ? "Network (Ethernet)" : "Serial")}");
    Console.WriteLine($"Mill ID:          {millId}");
    Console.WriteLine($"PLC Polling:      {(enablePLCPolling ? "ENABLED" : "DISABLED")}");
    Console.WriteLine($"Heartbeat Monitor: {(enableHeartbeatMonitoring ? "ENABLED" : "DISABLED")}");
    Console.WriteLine("==========================================");
    Console.WriteLine($"");
    Console.WriteLine($"Connecting to PLC at {plcIpAddress} (Rack: {plcRack}, Slot: {plcSlot})...");
    bool connected = plcService.Connect(plcIpAddress, plcRack, plcSlot);
    if (connected)
    {
        Console.WriteLine("✓ PLC connected successfully.");
        if (enablePLCPolling)
        {
            Console.WriteLine("  → NDT bundle polling service: ENABLED");
            Console.WriteLine($"  → Polling interval: {pollingIntervalMs}ms");
            Console.WriteLine($"  → Will print tag for every 5 NDT pipes counted");
        }
        if (enableHeartbeatMonitoring)
        {
            Console.WriteLine("  → Heartbeat monitoring service: ENABLED");
        }
    }
    else
    {
        Console.WriteLine("⚠ Failed to connect to PLC. Services will retry when connection is available.");
        Console.WriteLine($"  → Verify PLC is powered on and IP address is correct: {plcIpAddress}");
    }
    Console.WriteLine($"");
}

// Configure static files
app.UseDefaultFiles();
app.UseStaticFiles();

// Map SignalR hub
app.MapHub<HeartbeatHub>("/heartbeathub");

// API endpoints
app.MapGet("/api/bundles", (INDTBundleService bundleService) =>
{
    // Return all bundles for UI display (not just ready for printing)
    var bundles = bundleService.GetAllNDTBundles();
    return Results.Ok(bundles);
});

app.MapGet("/api/ok-bundles", (IOKBundleService okBundleService) =>
{
    // Return all OK bundles for UI display (not just ready for printing)
    var bundles = okBundleService.GetAllOKBundles();
    return Results.Ok(bundles);
});

app.MapPost("/api/ndt-cuts", (INDTBundleService bundleService, int cuts) =>
{
    try
    {
        bundleService.ProcessNDTCuts(1, cuts);
        return Results.Ok(new { success = true, message = $"Added {cuts} NDT cuts" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

app.MapPost("/api/ok-cuts", (IOKBundleService okBundleService, int cuts) =>
{
    try
    {
        okBundleService.ProcessOKCuts(1, cuts);
        return Results.Ok(new { success = true, message = $"Added {cuts} OK cuts" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

app.MapPost("/api/print/{bundleId}", (INDTBundleService bundleService, IPrinterService printerService, ExcelExportService excelService, IPLCService plcService, int bundleId) =>
{
    try
    {
        bool printed = bundleService.PrintBundleTag(bundleId, printerService, excelService, plcService);
        
        if (printed)
        {
            var printData = bundleService.GetBundlePrintData(bundleId);
            return Results.Ok(new { success = true, message = $"Bundle {printData.BundleNo} printed successfully" });
        }

        return Results.BadRequest(new { success = false, message = "Print failed" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

app.MapPost("/api/print-ok/{bundleId}", (IOKBundleService okBundleService, IPrinterService printerService, ExcelExportService excelService, IPLCService plcService, int bundleId) =>
{
    try
    {
        bool printed = okBundleService.PrintBundleTag(bundleId, printerService, excelService, plcService);
        
        if (printed)
        {
            var printData = okBundleService.GetBundlePrintData(bundleId);
            return Results.Ok(new { success = true, message = $"OK Bundle {printData.BundleNo} printed successfully" });
        }

        return Results.BadRequest(new { success = false, message = "Print failed" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

// PLC connection endpoints
app.MapPost("/api/plc/connect", (IPLCService plcService, string ipAddress, int? rack, int? slot) =>
{
    try
    {
        bool connected = plcService.Connect(ipAddress, rack ?? 0, slot ?? 1);
        return Results.Ok(new { success = connected, message = connected ? "PLC connected successfully" : "Failed to connect to PLC" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

app.MapPost("/api/plc/disconnect", (IPLCService plcService) =>
{
    try
    {
        plcService.Disconnect();
        return Results.Ok(new { success = true, message = "PLC disconnected" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

app.MapGet("/api/plc/status", (IPLCService plcService) =>
{
    return Results.Ok(new { connected = plcService.IsConnected });
});

// PLC Heartbeat endpoint
app.MapGet("/api/plc/heartbeat", (IPLCService plcService) =>
{
    try
    {
        if (!plcService.IsConnected)
        {
            return Results.Ok(new 
            { 
                heartbeatValue = -1, 
                plcStatus = "OFFLINE",
                plcIp = plcIpAddress,
                lastUpdateTime = DateTime.UtcNow
            });
        }

        int heartbeatValue = plcService.ReadHeartbeat();
        // All values 1-127 indicate PLC is ONLINE (continuous counter)
        // Value -1 indicates OFFLINE (not connected or object doesn't exist)
        string plcStatus = (heartbeatValue >= 1 && heartbeatValue <= 127) ? "ONLINE" : "OFFLINE";

        return Results.Ok(new 
        { 
            heartbeatValue = heartbeatValue, 
            plcStatus = plcStatus,
            plcIp = plcIpAddress,
            lastUpdateTime = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        // Handle gracefully - if heartbeat object doesn't exist, return offline status
        string errorMsg = ex.Message?.ToLower() ?? "";
        if (errorMsg.Contains("object does not exist") || 
            errorMsg.Contains("does not exist") ||
            errorMsg.Contains("not found"))
        {
            return Results.Ok(new 
            { 
                heartbeatValue = -1, 
                plcStatus = "OFFLINE",
                plcIp = plcIpAddress,
                lastUpdateTime = DateTime.UtcNow
            });
        }
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

// Process cuts from PLC
app.MapPost("/api/plc/process-cuts/{millId}", (INDTBundleService ndtBundleService, IOKBundleService okBundleService, IPLCService plcService, int millId) =>
{
    try
    {
        if (!plcService.IsConnected)
        {
            return Results.BadRequest(new { success = false, message = "PLC not connected" });
        }

        // Read OK cuts
        if (plcService is RealS7PLCService realPlc)
        {
            int okCuts = realPlc.ReadOKCuts(millId);
            if (okCuts > 0)
            {
                okBundleService.ProcessOKCuts(millId, okCuts);
            }
        }

        // Read NDT cuts
        int ndtCuts = plcService.ReadNDTCuts(millId);
        if (ndtCuts > 0)
        {
            ndtBundleService.ProcessNDTCuts(millId, ndtCuts);
            return Results.Ok(new { success = true, message = $"Processed {ndtCuts} NDT cuts and OK cuts from PLC" });
        }

        return Results.Ok(new { success = true, message = "No new cuts from PLC" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

// System status endpoint
app.MapGet("/api/system-status", (IPLCService plcService, INDTBundleService ndtBundleService, IOKBundleService okBundleService, IPipeCountingActivityServiceExtended activityService) =>
{
    try
    {
        var ndtBundles = ndtBundleService.GetAllNDTBundles();
        var ndtReadyBundles = ndtBundleService.GetBundlesReadyForPrinting();
        var okBundles = okBundleService.GetAllOKBundles();
        var okReadyBundles = okBundleService.GetBundlesReadyForPrinting();
        var (okCuts, ndtCuts) = activityService?.GetCurrentCounts() ?? (0, 0);
        
        return Results.Ok(new
        {
            plc = new
            {
                connected = plcService.IsConnected,
                ipAddress = "192.168.0.13",
                rack = 0,
                slot = 2
            },
            printer = new
            {
                ipAddress = "192.168.0.125",
                port = 9100,
                status = "ready" // Will be updated based on test print
            },
            pipeCounts = new
            {
                currentOKCuts = okCuts,
                currentNDTCuts = ndtCuts
            },
            ndtCounts = new
            {
                totalPipes = ndtBundles.Sum(b => b.NDT_Pcs),
                bundlesCreated = ndtBundles.Count,
                tagsPrinted = ndtBundles.Count(b => b.Status == 3)
            },
            okCounts = new
            {
                totalPipes = okBundles.Sum(b => b.OK_Pcs),
                bundlesCreated = okBundles.Count,
                tagsPrinted = okBundles.Count(b => b.Status == 3)
            },
            bundleStatus = new
            {
                active = ndtBundles.Count(b => b.Status == 1) + okBundles.Count(b => b.Status == 1),
                ready = ndtReadyBundles.Count + okReadyBundles.Count,
                printed = ndtBundles.Count(b => b.Status == 3) + okBundles.Count(b => b.Status == 3)
            }
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

// Pipe counting activity endpoint
app.MapGet("/api/pipe-counting-activity", (IPipeCountingActivityServiceExtended activityService) =>
{
    try
    {
        var activities = activityService?.GetRecentActivity(100) ?? new List<PipeCountingActivity>();
        var (okCuts, ndtCuts) = activityService?.GetCurrentCounts() ?? (0, 0);
        
        return Results.Ok(new
        {
            activities = activities.Select(a => new
            {
                timestamp = a.Timestamp,
                pipeType = a.PipeType,
                count = a.Count,
                totalOKCuts = a.TotalOKCuts,
                totalNDTCuts = a.TotalNDTCuts,
                source = a.Source
            }),
            currentCounts = new
            {
                okCuts = okCuts,
                ndtCuts = ndtCuts
            }
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

// Test print endpoint with dummy data using mock Rpt_MillLabel design
app.MapPost("/api/test-print", (IPrinterService printerService) =>
{
    try
    {
        // Create dummy print data matching Rpt_MillLabel structure
        var dummyPrintData = new NDTBundlePrintData
        {
            BundleNo = "TEST-001",
            BatchNo = "BATCH-2024-001",
            PO_No = "PO-12345",
            NDT_Pcs = 10,
            Pipe_Grade = "API 5L X42",
            Pipe_Size = "4''",
            Pipe_Len = 12.5m,
            BundleStartTime = DateTime.Now.AddHours(-2),
            BundleEndTime = DateTime.Now
        };

        // Print using the printer service (will use mock Rpt_MillLabel design)
        bool printed = printerService.PrintNDTBundleTag(dummyPrintData);
        
        if (printed)
        {
            return Results.Ok(new 
            { 
                success = true, 
                message = $"Test label printed successfully to printer at {printerAddress}:{printerPort}",
                printData = new
                {
                    dummyPrintData.BundleNo,
                    dummyPrintData.BatchNo,
                    dummyPrintData.PO_No,
                    dummyPrintData.NDT_Pcs,
                    dummyPrintData.Pipe_Grade,
                    dummyPrintData.Pipe_Size,
                    dummyPrintData.Pipe_Len
                }
            });
        }

        return Results.BadRequest(new { success = false, message = "Print failed - check printer connection and console logs" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
    }
});

app.Run();
