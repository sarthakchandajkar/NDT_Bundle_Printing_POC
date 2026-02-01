using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NDTBundlePOC.Core.Models;
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

// Configure JSON serialization to preserve property names
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null; // Preserve original property names
});

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
var enablePLCPolling = false;           // Enable automatic PLC polling for NDT cuts (OFF by default - must be started via button)
var pollingIntervalMs = 1000;          // Poll PLC every 1 second
var millId = 1;                        // Mill ID (1 = Mill 1)
var heartbeatPollingIntervalMs = 750;  // Heartbeat check every 750ms
var enableHeartbeatMonitoring = true;   // Enable heartbeat monitoring

// Printing Configuration
var useTestMode = true;                 // Set to true to log tags instead of printing (for testing)
                                        // Set to false to enable actual physical printing

// OK Bundle Printing Configuration
var bypassOKBundlePLCConditions = true; // Set to true to bypass PLC signal requirements for OK bundles (for testing)
                                        // Set to false for production (requires L1L2_PipeDone signal and packing station)

// Export/Output Path
var exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NDT_Bundle_Exports");

// Add services
builder.Services.AddSingleton<IDataRepository, SupabaseDataRepository>();

// Register PLC service first (needed by bundle services)
builder.Services.AddSingleton<IPLCService>(sp => new RealS7PLCService());

// Register bundle services with PLC service dependency
builder.Services.AddSingleton<INDTBundleService>(sp => 
    new NDTBundleService(sp.GetRequiredService<IDataRepository>(), sp.GetRequiredService<IPLCService>()));
builder.Services.AddSingleton<IOKBundleService>(sp => 
    new OKBundleService(sp.GetRequiredService<IDataRepository>(), sp.GetRequiredService<IPLCService>()));

// PLC service is registered above with bundle services

// Printer Service Configuration
// TEST MODE: Use LoggingPrinterService to log tags instead of printing (for testing/verification)
// PRODUCTION MODE: Use HoneywellPD45SPrinterService for actual physical printing
if (useTestMode)
{
    // Test Mode: Log all print attempts to file (no physical printing)
    builder.Services.AddSingleton<IPrinterService>(sp => 
        new LoggingPrinterService());
    Console.WriteLine("⚠ TEST MODE ENABLED: Tags will be logged to file instead of printing");
    Console.WriteLine($"  → Log file location: {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NDT_Bundle_POC_PrintLogs")}");
}
else
{
    // Production Mode: Use actual printer
    // Use ZPL-based printing for Honeywell PD45S printer (sends ZPL commands directly to printer)
    // Hardcoded values: IP=192.168.0.125, Port=9100, Network=true
    builder.Services.AddSingleton<IPrinterService>(sp => 
        new HoneywellPD45SPrinterService(
            printerAddress: printerAddress,  // Hardcoded: 192.168.0.125
            printerPort: printerPort,        // Hardcoded: 9100
            useNetwork: useNetwork           // Hardcoded: true (Ethernet)
        ));
    Console.WriteLine("✓ PRODUCTION MODE: Physical printing enabled");
    Console.WriteLine($"  → Printer: {printerAddress}:{printerPort}");
}

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

// Register Controllable PLC Polling Service (can be started/stopped via API)
// Note: This is NOT registered as a hosted service - it must be started manually via button
builder.Services.AddSingleton<IControllablePLCPollingService>(sp =>
{
    var plcService = sp.GetRequiredService<IPLCService>();
    var ndtBundleService = sp.GetRequiredService<INDTBundleService>();
    var okBundleService = sp.GetRequiredService<IOKBundleService>();
    var printerService = sp.GetRequiredService<IPrinterService>();
    var excelService = sp.GetRequiredService<ExcelExportService>();
    var activityService = sp.GetRequiredService<IPipeCountingActivityService>();
    var logger = sp.GetRequiredService<ILogger<ControllablePLCPollingService>>();
    
    return new ControllablePLCPollingService(
        plcService,
        ndtBundleService,
        okBundleService,
        printerService,
        excelService,
        logger,
        activityService,
        millId,
        pollingIntervalMs,
        bypassOKBundlePLCConditions
    );
});

// Legacy: Only register PLCPollingService as hosted service if enablePLCPolling is true (for backward compatibility)
// This should remain false by default - use ControllablePLCPollingService instead
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

// Note: SupabaseDataRepository connects directly to the database, no dummy data initialization needed

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
    Console.WriteLine($"Printing Mode:    {(useTestMode ? "TEST MODE (Logging Only)" : "PRODUCTION (Physical Printing)")}");
    Console.WriteLine($"OK Bundle PLC Bypass: {(bypassOKBundlePLCConditions ? "ENABLED (Test Mode)" : "DISABLED (Production)")}");
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
    try
    {
        // Return all bundles for UI display (not just ready for printing)
        var bundles = bundleService.GetAllNDTBundles();
        if (bundles == null)
        {
            Console.WriteLine("⚠ GetAllNDTBundles returned null - returning empty list");
            return Results.Ok(new List<NDTBundle>());
        }
        return Results.Ok(bundles);
    }
    catch (System.Net.Sockets.SocketException ex)
    {
        Console.WriteLine($"✗ Network error in /api/bundles endpoint: {ex.Message}");
        Console.WriteLine($"  → Database connection failed - returning empty list");
        return Results.Ok(new List<NDTBundle>());
    }
    catch (Npgsql.NpgsqlException ex)
    {
        Console.WriteLine($"✗ Database error in /api/bundles endpoint: {ex.Message}");
        Console.WriteLine($"  → Database connection failed - returning empty list");
        return Results.Ok(new List<NDTBundle>());
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Error in /api/bundles endpoint: {ex.Message}");
        Console.WriteLine($"  → Stack trace: {ex.StackTrace}");
        // Return empty list instead of error to allow UI to continue
        return Results.Ok(new List<NDTBundle>());
    }
});

app.MapGet("/api/ok-bundles", (IOKBundleService okBundleService) =>
{
    try
    {
        // Return all OK bundles for UI display (not just ready for printing)
        var bundles = okBundleService.GetAllOKBundles();
        return Results.Ok(bundles);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Error in /api/ok-bundles endpoint: {ex.Message}");
        // Return empty list instead of error to allow UI to continue
        return Results.Ok(new List<OKBundle>());
    }
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
        // Get bundles with null-safe handling
        var ndtBundles = ndtBundleService?.GetAllNDTBundles() ?? new List<NDTBundle>();
        var ndtReadyBundles = ndtBundleService?.GetBundlesReadyForPrinting() ?? new List<NDTBundle>();
        var okBundles = okBundleService?.GetAllOKBundles() ?? new List<OKBundle>();
        var okReadyBundles = okBundleService?.GetBundlesReadyForPrinting() ?? new List<OKBundle>();
        var (okCuts, ndtCuts) = activityService?.GetCurrentCounts() ?? (0, 0);
        
        // Ensure bundles are not null before using LINQ operations
        if (ndtBundles == null) ndtBundles = new List<NDTBundle>();
        if (okBundles == null) okBundles = new List<OKBundle>();
        if (ndtReadyBundles == null) ndtReadyBundles = new List<NDTBundle>();
        if (okReadyBundles == null) okReadyBundles = new List<OKBundle>();
        
        return Results.Ok(new
        {
            plc = new
            {
                connected = plcService?.IsConnected ?? false,
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
                totalPipes = ndtBundles.Sum(b => b?.NDT_Pcs ?? 0),
                bundlesCreated = ndtBundles.Count,
                tagsPrinted = ndtBundles.Count(b => b?.Status == 3)
            },
            okCounts = new
            {
                totalPipes = okBundles.Sum(b => b?.OK_Pcs ?? 0),
                bundlesCreated = okBundles.Count,
                tagsPrinted = okBundles.Count(b => b?.Status == 3)
            },
            bundleStatus = new
            {
                active = ndtBundles.Count(b => b?.Status == 1) + okBundles.Count(b => b?.Status == 1),
                ready = (ndtReadyBundles?.Count ?? 0) + (okReadyBundles?.Count ?? 0),
                printed = ndtBundles.Count(b => b?.Status == 3) + okBundles.Count(b => b?.Status == 3)
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Error in /api/system-status endpoint: {ex.Message}");
        Console.WriteLine($"  → Stack trace: {ex.StackTrace}");
        
        // Return a valid response structure even on error, so UI doesn't break
        return Results.Ok(new
        {
            plc = new { connected = false, ipAddress = "192.168.0.13", rack = 0, slot = 2 },
            printer = new { ipAddress = "192.168.0.125", port = 9100, status = "error" },
            pipeCounts = new { currentOKCuts = 0, currentNDTCuts = 0 },
            ndtCounts = new { totalPipes = 0, bundlesCreated = 0, tagsPrinted = 0 },
            okCounts = new { totalPipes = 0, bundlesCreated = 0, tagsPrinted = 0 },
            bundleStatus = new { active = 0, ready = 0, printed = 0 },
            error = ex.Message
        });
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

// Test Scenario Management API endpoints
app.MapPost("/api/test-scenarios/activate/{scenarioName}", async (IDataRepository repository, IConfiguration configuration, string scenarioName) =>
{
    try
    {
        // Define test scenarios
        var scenarios = new Dictionary<string, (string poNo, string? pipeSize, int pcsPerBundle)>
        {
            { "CASE1", ("PO_TEST_CASE1", "8.0", 42) },
            { "CASE2", ("PO_TEST_CASE2", "6.0", 21) },
            { "CASE3", ("PO_TEST_CASE3", "5.0", 14) },
            { "CASE4", ("PO_TEST_CASE4", "3.0", 50) },
            { "CASE5", ("PO_TEST_CASE5", null, 42) }
        };

        if (!scenarios.ContainsKey(scenarioName))
        {
            return Results.BadRequest(new { success = false, message = $"Unknown scenario: {scenarioName}" });
        }

        var (poNo, pipeSize, pcsPerBundle) = scenarios[scenarioName];

        // Close all active slits (Status = 2)
        var allPOPlans = repository.GetPOPlans() ?? new List<POPlan>();
        foreach (var po in allPOPlans)
        {
            var activeSlit = repository.GetActiveSlit(po.PO_Plan_ID);
            if (activeSlit != null && activeSlit.Status == 2)
            {
                activeSlit.Status = 3; // Completed
                repository.UpdateSlit(activeSlit);
            }
        }

        // Find or create PO Plan
        var poPlan = allPOPlans.FirstOrDefault(p => p.PO_No == poNo);

        if (poPlan == null)
        {
            // Create new PO Plan
            poPlan = new POPlan
            {
                PLC_POID = 2000 + int.Parse(scenarioName.Replace("CASE", "")),
                PO_No = poNo,
                Pipe_Type = "X65",
                Pipe_Size = pipeSize,
                PcsPerBundle = pcsPerBundle,
                Pipe_Len = 12.0m,
                PipeWt_per_mtr = 2.5m,
                SAP_Type = "SAP_TEST",
                Shop_ID = 1
            };
            repository.AddPOPlan(poPlan);
            
            // Reload to get the ID
            allPOPlans = repository.GetPOPlans() ?? new List<POPlan>();
            poPlan = allPOPlans.FirstOrDefault(p => p.PO_No == poNo);
        }

        if (poPlan == null)
        {
            return Results.BadRequest(new { success = false, message = "Failed to create or find PO Plan" });
        }

        // Create or activate Slit using direct SQL (since AddSlit doesn't exist in interface)
        var connectionString = configuration.GetConnectionString("ServerConnectionString");
        if (string.IsNullOrEmpty(connectionString))
        {
            return Results.BadRequest(new { success = false, message = "Database connection string not configured" });
        }

        using (var conn = new NpgsqlConnection(connectionString))
        {
            conn.Open();
            
            // Check if slit exists
            using (var checkCmd = new NpgsqlCommand(
                @"SELECT ""Slit_ID"", ""Status"" FROM ""M1_Slit"" 
                  WHERE ""PO_Plan_ID"" = @poPlanId AND ""Slit_No"" = @slitNo", conn))
            {
                checkCmd.Parameters.AddWithValue("@poPlanId", poPlan.PO_Plan_ID);
                checkCmd.Parameters.AddWithValue("@slitNo", $"SLIT_{scenarioName}");
                
                var existingSlitId = checkCmd.ExecuteScalar();
                
                if (existingSlitId != null && existingSlitId != DBNull.Value)
                {
                    // Update existing slit
                    using (var updateCmd = new NpgsqlCommand(
                        @"UPDATE ""M1_Slit"" 
                          SET ""Status"" = 2, ""Slit_NDT"" = 0, ""SlitMillStartTime"" = CURRENT_TIMESTAMP
                          WHERE ""Slit_ID"" = @slitId", conn))
                    {
                        updateCmd.Parameters.AddWithValue("@slitId", existingSlitId);
                        updateCmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    // Create new slit
                    using (var insertCmd = new NpgsqlCommand(
                        @"INSERT INTO ""M1_Slit"" (""PO_Plan_ID"", ""Slit_No"", ""Status"", ""Slit_NDT"", ""SlitMillStartTime"")
                          VALUES (@poPlanId, @slitNo, 2, 0, CURRENT_TIMESTAMP)", conn))
                    {
                        insertCmd.Parameters.AddWithValue("@poPlanId", poPlan.PO_Plan_ID);
                        insertCmd.Parameters.AddWithValue("@slitNo", $"SLIT_{scenarioName}");
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        return Results.Ok(new 
        { 
            success = true, 
            message = $"Test scenario {scenarioName} activated",
            poPlan = new
            {
                poPlan.PO_Plan_ID,
                poPlan.PO_No,
                poPlan.Pipe_Size,
                poPlan.PcsPerBundle
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Error activating test scenario {scenarioName}: {ex.Message}");
        Console.WriteLine($"  → Stack trace: {ex.StackTrace}");
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

// Get current active scenario
app.MapGet("/api/test-scenarios/active", (IDataRepository repository) =>
{
    try
    {
        var activeSlit = repository.GetActiveSlit(0);
        if (activeSlit == null)
        {
            return Results.Ok(new { activeScenario = (string?)null });
        }

        var poPlan = repository.GetPOPlan(activeSlit.PO_Plan_ID);
        if (poPlan == null)
        {
            return Results.Ok(new { activeScenario = (string?)null });
        }

        // Determine scenario name from PO_No
        string? scenarioName = null;
        if (poPlan.PO_No != null && poPlan.PO_No.StartsWith("PO_TEST_CASE"))
        {
            scenarioName = poPlan.PO_No.Replace("PO_TEST_", "");
        }

        return Results.Ok(new 
        { 
            activeScenario = scenarioName,
            poPlan = new
            {
                poPlan.PO_Plan_ID,
                poPlan.PO_No,
                poPlan.Pipe_Size,
                poPlan.PcsPerBundle
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Error getting active scenario: {ex.Message}");
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

// PO Plan Management API endpoints
app.MapGet("/api/po-plans", (IDataRepository repository) =>
{
    try
    {
        var poPlans = repository.GetPOPlans();
        if (poPlans == null)
        {
            Console.WriteLine("⚠ GetPOPlans returned null - returning empty list");
            return Results.Ok(new List<POPlan>());
        }
        return Results.Ok(poPlans);
    }
    catch (System.Net.Sockets.SocketException ex)
    {
        Console.WriteLine($"✗ Network error in /api/po-plans endpoint: {ex.Message}");
        Console.WriteLine($"  → Database connection failed - returning empty list");
        return Results.Ok(new List<POPlan>());
    }
    catch (Npgsql.NpgsqlException ex)
    {
        Console.WriteLine($"✗ Database error in /api/po-plans endpoint: {ex.Message}");
        Console.WriteLine($"  → Database connection failed - returning empty list");
        return Results.Ok(new List<POPlan>());
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Error in /api/po-plans endpoint: {ex.Message}");
        Console.WriteLine($"  → Stack trace: {ex.StackTrace}");
        // Return empty list instead of error object to allow UI to continue
        return Results.Ok(new List<POPlan>());
    }
});

app.MapGet("/api/po-plans/{id}", (IDataRepository repository, int id) =>
{
    try
    {
        var poPlan = repository.GetPOPlan(id);
        if (poPlan == null)
            return Results.NotFound(new { success = false, message = "PO Plan not found" });
        return Results.Ok(poPlan);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

app.MapPost("/api/po-plans", (IDataRepository repository, POPlan poPlan) =>
{
    try
    {
        repository.AddPOPlan(poPlan);
        return Results.Ok(new { success = true, message = "PO Plan added successfully", data = poPlan });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

app.MapPut("/api/po-plans/{id}", (IDataRepository repository, int id, POPlan poPlan) =>
{
    try
    {
        if (poPlan.PO_Plan_ID != id)
            return Results.BadRequest(new { success = false, message = "ID mismatch" });
        
        repository.UpdatePOPlan(poPlan);
        return Results.Ok(new { success = true, message = "PO Plan updated successfully", data = poPlan });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

app.MapDelete("/api/po-plans/{id}", (IDataRepository repository, int id) =>
{
    try
    {
        repository.DeletePOPlan(id);
        return Results.Ok(new { success = true, message = "PO Plan deleted successfully" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

// PLC Polling Control API endpoints
app.MapGet("/api/plc/polling/status", (IControllablePLCPollingService pollingService) =>
{
    try
    {
        bool isPolling = pollingService.IsPolling;
        return Results.Ok(new { isPolling = isPolling });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

app.MapPost("/api/plc/polling/start", async (IControllablePLCPollingService pollingService, IPLCService plcService) =>
{
    try
    {
        if (pollingService.IsPolling)
        {
            return Results.Ok(new { success = true, message = "PLC polling is already running", isPolling = true });
        }

        if (!plcService.IsConnected)
        {
            return Results.BadRequest(new { success = false, message = "Cannot start polling: PLC is not connected. Please connect to PLC first." });
        }

        bool started = await pollingService.StartAsync();
        if (started)
        {
            return Results.Ok(new { success = true, message = "PLC polling started successfully", isPolling = true });
        }
        else
        {
            return Results.BadRequest(new { success = false, message = "Failed to start PLC polling" });
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

app.MapPost("/api/plc/polling/stop", async (IControllablePLCPollingService pollingService) =>
{
    try
    {
        if (!pollingService.IsPolling)
        {
            return Results.Ok(new { success = true, message = "PLC polling is not running", isPolling = false });
        }

        bool stopped = await pollingService.StopAsync();
        if (stopped)
        {
            return Results.Ok(new { success = true, message = "PLC polling stopped successfully", isPolling = false });
        }
        else
        {
            return Results.BadRequest(new { success = false, message = "Failed to stop PLC polling" });
        }
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
