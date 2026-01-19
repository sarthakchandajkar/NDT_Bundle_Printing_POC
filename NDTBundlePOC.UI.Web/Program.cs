using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NDTBundlePOC.Core.Services;
using NDTBundlePOC.UI.Web.Hubs;
using NDTBundlePOC.UI.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration (appsettings.json is automatically loaded in .NET 10)
var plcIpAddress = builder.Configuration["PLC:IPAddress"] ?? "192.168.0.74";
var plcRack = int.Parse(builder.Configuration["PLC:Rack"] ?? "0");
var plcSlot = int.Parse(builder.Configuration["PLC:Slot"] ?? "1");
var printerAddress = builder.Configuration["Printer:Address"] ?? "192.168.1.200";
var printerPort = int.Parse(builder.Configuration["Printer:Port"] ?? "9100");
var useNetwork = bool.Parse(builder.Configuration["Printer:UseNetwork"] ?? "true");
var reportTemplatePath = builder.Configuration["Printer:ReportTemplatePath"];
var exportPath = builder.Configuration["Export:Path"];
var enablePLCPolling = bool.Parse(builder.Configuration["PLC:EnablePolling"] ?? "false");
var pollingIntervalMs = int.Parse(builder.Configuration["PLC:PollingIntervalMs"] ?? "1000");
var millId = int.Parse(builder.Configuration["PLC:MillId"] ?? "1");
var heartbeatPollingIntervalMs = int.Parse(builder.Configuration["PLC:HeartbeatPollingIntervalMs"] ?? "750");
var enableHeartbeatMonitoring = bool.Parse(builder.Configuration["PLC:EnableHeartbeatMonitoring"] ?? "true");

// Add services
builder.Services.AddSingleton<IDataRepository, InMemoryDataRepository>();
builder.Services.AddSingleton<INDTBundleService, NDTBundleService>();
builder.Services.AddSingleton<IOKBundleService, OKBundleService>();

// Use RealS7PLCService for physical PLC connection
builder.Services.AddSingleton<IPLCService>(sp => new RealS7PLCService());

// Use ZPL-based printing for Honeywell PD45S printer (sends ZPL commands, not images)
builder.Services.AddSingleton<IPrinterService>(sp => 
    new HoneywellPD45SPrinterService(printerAddress, printerPort, useNetwork));

// Alternative: Use Telerik Reporting for printing (generates images - use for Windows printers or if you have Telerik Reporting installed)
// builder.Services.AddSingleton<IPrinterService>(sp => 
//     new TelerikReportingPrinterService(printerAddress, printerPort, useNetwork, reportTemplatePath));

builder.Services.AddSingleton<ExcelExportService>(sp => new ExcelExportService(exportPath));

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Add Heartbeat Notifier (SignalR implementation)
builder.Services.AddSingleton<IHeartbeatNotifier, HeartbeatNotifier>();

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
        var logger = sp.GetRequiredService<ILogger<PLCPollingService>>();
        
        return new PLCPollingService(
            plcService,
            ndtBundleService,
            okBundleService,
            printerService,
            excelService,
            logger,
            millId,
            pollingIntervalMs
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
    Console.WriteLine($"Connecting to PLC at {plcIpAddress}...");
    bool connected = plcService.Connect(plcIpAddress, plcRack, plcSlot);
    if (connected)
    {
        Console.WriteLine("✓ PLC connected.");
        if (enablePLCPolling)
        {
            Console.WriteLine("  → Polling service will start automatically.");
        }
        if (enableHeartbeatMonitoring)
        {
            Console.WriteLine("  → Heartbeat monitoring service will start automatically.");
        }
    }
    else
    {
        Console.WriteLine("⚠ Failed to connect to PLC. Services will retry when connection is available.");
    }
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
    var bundles = okBundleService.GetBundlesReadyForPrinting();
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
        // Value -1 indicates OFFLINE (not connected)
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
