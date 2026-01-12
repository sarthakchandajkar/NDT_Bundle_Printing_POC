using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NDTBundlePOC.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration (appsettings.json is automatically loaded in .NET 8)
var plcIpAddress = builder.Configuration["PLC:IPAddress"] ?? "192.168.1.100";
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

// Add services
builder.Services.AddSingleton<IDataRepository, InMemoryDataRepository>();
builder.Services.AddSingleton<INDTBundleService, NDTBundleService>();
builder.Services.AddSingleton<IOKBundleService, OKBundleService>();

// Use RealS7PLCService for physical PLC connection
builder.Services.AddSingleton<IPLCService>(sp => new RealS7PLCService());

// Use Telerik Reporting for printing
builder.Services.AddSingleton<IPrinterService>(sp => 
    new TelerikReportingPrinterService(printerAddress, printerPort, useNetwork, reportTemplatePath));

// Alternative: Use ZPL-based printing (uncomment if needed)
// builder.Services.AddSingleton<IPrinterService>(sp => 
//     new HoneywellPD45SPrinterService(printerAddress, printerPort, useNetwork));

builder.Services.AddSingleton<ExcelExportService>(sp => new ExcelExportService(exportPath));

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

// Auto-connect to PLC if configured
var plcService = app.Services.GetRequiredService<IPLCService>();
if (enablePLCPolling && !string.IsNullOrEmpty(plcIpAddress))
{
    Console.WriteLine($"Connecting to PLC at {plcIpAddress}...");
    bool connected = plcService.Connect(plcIpAddress, plcRack, plcSlot);
    if (connected)
    {
        Console.WriteLine("✓ PLC connected. Polling service will start automatically.");
    }
    else
    {
        Console.WriteLine("⚠ Failed to connect to PLC. Polling service will retry.");
    }
}

// Configure static files
app.UseDefaultFiles();
app.UseStaticFiles();

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

app.Run();
