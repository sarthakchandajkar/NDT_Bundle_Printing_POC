using System;
using System.Collections.Generic;
using System.Linq;
using NDTBundlePOC.Core.Models;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace NDTBundlePOC.Core.Services
{
    public class SupabaseDataRepository : IDataRepository
    {
        private readonly string _connectionString;

        public SupabaseDataRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ServerConnectionString") 
                ?? throw new InvalidOperationException(
                    "ServerConnectionString not found in configuration. " +
                    "Please add it to appsettings.json: " +
                    "\"ConnectionStrings\": { \"ServerConnectionString\": \"Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require;\" }");
            
            // Validate connection string format
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException("ServerConnectionString is empty. Please configure it in appsettings.json");
            }
            
            // Log connection string (without password) for debugging
            var host = ExtractHostFromConnectionString();
            if (host != "Unknown")
            {
                Console.WriteLine($"→ Database connection configured for host: {host}");
                
                // Test DNS resolution
                try
                {
                    var hostEntry = System.Net.Dns.GetHostEntry(host);
                    if (hostEntry.AddressList.Length > 0)
                    {
                        var ipv4Addresses = hostEntry.AddressList
                            .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            .Select(a => a.ToString())
                            .ToList();
                        var ipv6Addresses = hostEntry.AddressList
                            .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                            .Select(a => a.ToString())
                            .ToList();
                        
                        Console.WriteLine($"✓ DNS resolution successful.");
                        if (ipv4Addresses.Any())
                        {
                            Console.WriteLine($"  → IPv4 addresses: {string.Join(", ", ipv4Addresses)}");
                        }
                        if (ipv6Addresses.Any())
                        {
                            Console.WriteLine($"  → IPv6 addresses: {string.Join(", ", ipv6Addresses)}");
                        }
                        
                        if (!ipv4Addresses.Any() && ipv6Addresses.Any())
                        {
                            Console.WriteLine($"⚠ Warning: Only IPv6 address found. If connection fails, try:");
                            Console.WriteLine($"    1. Use Supabase Connection Pooling (pooler.supabase.com)");
                            Console.WriteLine($"    2. Check if your network/firewall supports IPv6");
                            Console.WriteLine($"    3. Verify IPv6 connectivity: ping6 {ipv6Addresses.First()}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"⚠ DNS resolution returned no IP addresses for host: {host}");
                    }
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    Console.WriteLine($"✗ DNS resolution failed for host: {host}");
                    Console.WriteLine($"  → Error: {ex.Message}");
                    Console.WriteLine($"  → Possible causes:");
                    Console.WriteLine($"    1. Hostname is incorrect - verify in Supabase dashboard");
                    Console.WriteLine($"    2. Supabase project may be paused or deleted");
                    Console.WriteLine($"    3. Network/DNS server cannot resolve the hostname");
                    Console.WriteLine($"    4. Firewall blocking DNS queries");
                    Console.WriteLine($"  → To fix:");
                    Console.WriteLine($"    - Go to Supabase Dashboard → Settings → Database");
                    Console.WriteLine($"    - Copy the exact connection string from 'Connection string' section");
                    Console.WriteLine($"    - Verify the hostname matches exactly");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Could not test DNS resolution: {ex.Message}");
                }
            }
        }

        private NpgsqlConnection GetConnection()
        {
            // Npgsql should handle IPv6 automatically, but we can add connection string options if needed
            // If IPv6 connection fails, try adding: "Include Error Detail=true;" to see more details
            var connectionString = _connectionString;
            
            // Ensure we have proper SSL and connection timeout settings
            if (!connectionString.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
            {
                connectionString += "Timeout=30;";
            }
            
            return new NpgsqlConnection(connectionString);
        }

        /// <summary>
        /// Wrapper method to safely execute database operations with error handling
        /// </summary>
        private T SafeExecute<T>(Func<T> operation, T defaultValue, string operationName)
        {
            try
            {
                return operation();
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                Console.WriteLine($"✗ Network error in {operationName}: {ex.Message}");
                Console.WriteLine($"  → Check your internet connection");
                Console.WriteLine($"  → Verify Supabase hostname is correct: {ExtractHostFromConnectionString()}");
                return defaultValue;
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"✗ Database error in {operationName}: {ex.Message}");
                Console.WriteLine($"  → Check your connection string in appsettings.json");
                return defaultValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error in {operationName}: {ex.Message}");
                return defaultValue;
            }
        }

        // NDT Bundles
        public List<NDTBundle> GetNDTBundles()
        {
            var bundles = new List<NDTBundle>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(@"SELECT ""NDTBundle_ID"", ""PO_Plan_ID"", ""Slit_ID"", ""Bundle_No"", ""NDT_Pcs"", ""Bundle_Wt"", ""Status"", ""IsFullBundle"", ""BundleStartTime"", ""BundleEndTime"", ""OprDoneTime"", ""Batch_No""
                                                        FROM ""M1_NDTBundles""
                                                        ORDER BY ""NDTBundle_ID"" DESC", conn))
                    {
                        using (var rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                bundles.Add(new NDTBundle
                                {
                                    NDTBundle_ID = Convert.ToInt32(rdr["NDTBundle_ID"]),
                                    PO_Plan_ID = Convert.ToInt32(rdr["PO_Plan_ID"]),
                                    Slit_ID = rdr["Slit_ID"] == DBNull.Value ? null : Convert.ToInt32(rdr["Slit_ID"]),
                                    Bundle_No = rdr["Bundle_No"]?.ToString() ?? "",
                                    NDT_Pcs = Convert.ToInt32(rdr["NDT_Pcs"]),
                                    Bundle_Wt = Convert.ToDecimal(rdr["Bundle_Wt"]),
                                    Status = Convert.ToInt32(rdr["Status"]),
                                    IsFullBundle = Convert.ToBoolean(rdr["IsFullBundle"]),
                                    BundleStartTime = Convert.ToDateTime(rdr["BundleStartTime"]),
                                    BundleEndTime = rdr["BundleEndTime"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(rdr["BundleEndTime"]),
                                    OprDoneTime = rdr["OprDoneTime"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(rdr["OprDoneTime"]),
                                    Batch_No = rdr["Batch_No"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"✗ Database connection error in GetNDTBundles: {ex.Message}");
                Console.WriteLine($"  → Check your connection string in appsettings.json");
                Console.WriteLine($"  → Verify Supabase hostname is correct and accessible");
                // Return empty list instead of throwing - allows application to continue
                return new List<NDTBundle>();
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                Console.WriteLine($"✗ Network error connecting to database: {ex.Message}");
                Console.WriteLine($"  → Check your internet connection");
                Console.WriteLine($"  → Verify Supabase hostname is correct: {ExtractHostFromConnectionString()}");
                // Return empty list instead of throwing - allows application to continue
                return new List<NDTBundle>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error in GetNDTBundles: {ex.Message}");
                Console.WriteLine($"  → Stack trace: {ex.StackTrace}");
                // Return empty list instead of throwing - allows application to continue
                return new List<NDTBundle>();
            }
            return bundles;
        }

        private string ExtractHostFromConnectionString()
        {
            try
            {
                if (string.IsNullOrEmpty(_connectionString))
                    return "Unknown";
                
                var parts = _connectionString.Split(';');
                foreach (var part in parts)
                {
                    if (part.Trim().StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
                    {
                        return part.Split('=')[1].Trim();
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        public NDTBundle GetNDTBundle(int bundleId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"SELECT ""NDTBundle_ID"", ""PO_Plan_ID"", ""Slit_ID"", ""Bundle_No"", ""NDT_Pcs"", ""Bundle_Wt"", ""Status"", ""IsFullBundle"", ""BundleStartTime"", ""BundleEndTime"", ""OprDoneTime"", ""Batch_No""
                                                    FROM ""M1_NDTBundles""
                                                    WHERE ""NDTBundle_ID"" = @bundleId", conn))
                {
                    cmd.Parameters.AddWithValue("@bundleId", bundleId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            return new NDTBundle
                            {
                                NDTBundle_ID = Convert.ToInt32(rdr["NDTBundle_ID"]),
                                PO_Plan_ID = Convert.ToInt32(rdr["PO_Plan_ID"]),
                                Slit_ID = rdr["Slit_ID"] == DBNull.Value ? null : Convert.ToInt32(rdr["Slit_ID"]),
                                Bundle_No = rdr["Bundle_No"]?.ToString() ?? "",
                                NDT_Pcs = Convert.ToInt32(rdr["NDT_Pcs"]),
                                Bundle_Wt = Convert.ToDecimal(rdr["Bundle_Wt"]),
                                Status = Convert.ToInt32(rdr["Status"]),
                                IsFullBundle = Convert.ToBoolean(rdr["IsFullBundle"]),
                                BundleStartTime = Convert.ToDateTime(rdr["BundleStartTime"]),
                                BundleEndTime = rdr["BundleEndTime"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(rdr["BundleEndTime"]),
                                OprDoneTime = rdr["OprDoneTime"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(rdr["OprDoneTime"]),
                                Batch_No = rdr["Batch_No"]?.ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }

        public NDTBundle GetActiveNDTBundle(int poPlanId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"SELECT ""NDTBundle_ID"", ""PO_Plan_ID"", ""Slit_ID"", ""Bundle_No"", ""NDT_Pcs"", ""Bundle_Wt"", ""Status"", ""IsFullBundle"", ""BundleStartTime"", ""BundleEndTime"", ""OprDoneTime"", ""Batch_No""
                                                    FROM ""M1_NDTBundles""
                                                    WHERE ""PO_Plan_ID"" = @poPlanId AND ""Status"" = 1
                                                    ORDER BY ""BundleStartTime"" DESC
                                                    LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@poPlanId", poPlanId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            return new NDTBundle
                            {
                                NDTBundle_ID = Convert.ToInt32(rdr["NDTBundle_ID"]),
                                PO_Plan_ID = Convert.ToInt32(rdr["PO_Plan_ID"]),
                                Slit_ID = rdr["Slit_ID"] == DBNull.Value ? null : Convert.ToInt32(rdr["Slit_ID"]),
                                Bundle_No = rdr["Bundle_No"]?.ToString() ?? "",
                                NDT_Pcs = Convert.ToInt32(rdr["NDT_Pcs"]),
                                Bundle_Wt = Convert.ToDecimal(rdr["Bundle_Wt"]),
                                Status = Convert.ToInt32(rdr["Status"]),
                                IsFullBundle = Convert.ToBoolean(rdr["IsFullBundle"]),
                                BundleStartTime = Convert.ToDateTime(rdr["BundleStartTime"]),
                                BundleEndTime = rdr["BundleEndTime"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(rdr["BundleEndTime"]),
                                OprDoneTime = rdr["OprDoneTime"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(rdr["OprDoneTime"]),
                                Batch_No = rdr["Batch_No"]?.ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }

        public void AddNDTBundle(NDTBundle bundle)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"INSERT INTO ""M1_NDTBundles"" (""PO_Plan_ID"", ""Slit_ID"", ""Bundle_No"", ""NDT_Pcs"", ""Bundle_Wt"", ""Status"", ""IsFullBundle"", ""BundleStartTime"", ""BundleEndTime"", ""OprDoneTime"", ""Batch_No"")
                                                    VALUES (@poPlanId, @slitId, @bundleNo, @ndtPcs, @bundleWt, @status, @isFullBundle, @bundleStartTime, @bundleEndTime, @oprDoneTime, @batchNo)
                                                    RETURNING ""NDTBundle_ID""", conn))
                {
                    cmd.Parameters.AddWithValue("@poPlanId", bundle.PO_Plan_ID);
                    cmd.Parameters.AddWithValue("@slitId", (object)bundle.Slit_ID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@bundleNo", bundle.Bundle_No ?? "");
                    cmd.Parameters.AddWithValue("@ndtPcs", bundle.NDT_Pcs);
                    cmd.Parameters.AddWithValue("@bundleWt", bundle.Bundle_Wt);
                    cmd.Parameters.AddWithValue("@status", bundle.Status);
                    cmd.Parameters.AddWithValue("@isFullBundle", bundle.IsFullBundle);
                    cmd.Parameters.AddWithValue("@bundleStartTime", bundle.BundleStartTime);
                    cmd.Parameters.AddWithValue("@bundleEndTime", (object)bundle.BundleEndTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@oprDoneTime", (object)bundle.OprDoneTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@batchNo", (object)bundle.Batch_No ?? DBNull.Value);
                    bundle.NDTBundle_ID = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void UpdateNDTBundle(NDTBundle bundle)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"UPDATE ""M1_NDTBundles""
                                                    SET ""PO_Plan_ID"" = @poPlanId, ""Slit_ID"" = @slitId, ""Bundle_No"" = @bundleNo, ""NDT_Pcs"" = @ndtPcs, ""Bundle_Wt"" = @bundleWt, ""Status"" = @status, ""IsFullBundle"" = @isFullBundle, ""BundleStartTime"" = @bundleStartTime, ""BundleEndTime"" = @bundleEndTime, ""OprDoneTime"" = @oprDoneTime, ""Batch_No"" = @batchNo
                                                    WHERE ""NDTBundle_ID"" = @bundleId", conn))
                {
                    cmd.Parameters.AddWithValue("@bundleId", bundle.NDTBundle_ID);
                    cmd.Parameters.AddWithValue("@poPlanId", bundle.PO_Plan_ID);
                    cmd.Parameters.AddWithValue("@slitId", (object)bundle.Slit_ID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@bundleNo", bundle.Bundle_No ?? "");
                    cmd.Parameters.AddWithValue("@ndtPcs", bundle.NDT_Pcs);
                    cmd.Parameters.AddWithValue("@bundleWt", bundle.Bundle_Wt);
                    cmd.Parameters.AddWithValue("@status", bundle.Status);
                    cmd.Parameters.AddWithValue("@isFullBundle", bundle.IsFullBundle);
                    cmd.Parameters.AddWithValue("@bundleStartTime", bundle.BundleStartTime);
                    cmd.Parameters.AddWithValue("@bundleEndTime", (object)bundle.BundleEndTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@oprDoneTime", (object)bundle.OprDoneTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@batchNo", (object)bundle.Batch_No ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Formation Chart - Get by Size (for NDT bundles)
        public NDTBundleFormationChart GetNDTFormationChart(int millId, decimal? pipeSize)
        {
            return SafeExecute(() =>
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT ""NDTBundleFormationChart_ID"", ""Mill_ID"", ""Pipe_Size"", ""NDT_PcsPerBundle"", ""IsActive""
                                    FROM ""NDT_BundleFormationChart""
                                    WHERE ""Mill_ID"" = @millId AND ""IsActive"" = true";
                    
                    if (pipeSize.HasValue)
                    {
                        // First try to get size-specific configuration
                        // Use ROUND to handle potential precision differences (e.g., 8.0 vs 8.00)
                        query = @"SELECT ""NDTBundleFormationChart_ID"", ""Mill_ID"", ""Pipe_Size"", ""NDT_PcsPerBundle"", ""IsActive""
                                 FROM ""NDT_BundleFormationChart""
                                 WHERE ""Mill_ID"" = @millId 
                                   AND ""IsActive"" = true 
                                   AND ROUND(""Pipe_Size""::numeric, 2) = ROUND(@pipeSize::numeric, 2)
                                 ORDER BY ""Pipe_Size"" DESC
                                 LIMIT 1";
                    }
                    else
                    {
                        // Get default configuration (Pipe_Size IS NULL)
                        query += @" AND ""Pipe_Size"" IS NULL
                                   LIMIT 1";
                    }

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@millId", millId);
                        if (pipeSize.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@pipeSize", pipeSize.Value);
                        }
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                var chart = new NDTBundleFormationChart
                                {
                                    NDTBundleFormationChart_ID = Convert.ToInt32(rdr["NDTBundleFormationChart_ID"]),
                                    Mill_ID = Convert.ToInt32(rdr["Mill_ID"]),
                                    Pipe_Size = rdr["Pipe_Size"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(rdr["Pipe_Size"]),
                                    NDT_PcsPerBundle = Convert.ToInt32(rdr["NDT_PcsPerBundle"]),
                                    IsActive = Convert.ToBoolean(rdr["IsActive"])
                                };
                                
                                Console.WriteLine($"🔍 Database Query Result: Found chart entry - Pipe_Size: {chart.Pipe_Size?.ToString() ?? "NULL"}, NDT_PcsPerBundle: {chart.NDT_PcsPerBundle}");
                                return chart;
                            }
                        }
                    }
                    
                    // If size-specific not found and pipeSize was provided, fall back to default
                    if (pipeSize.HasValue)
                    {
                        Console.WriteLine($"⚠️  Size-specific chart not found for Pipe_Size={pipeSize}. Falling back to default (NULL).");
                        
                        query = @"SELECT ""NDTBundleFormationChart_ID"", ""Mill_ID"", ""Pipe_Size"", ""NDT_PcsPerBundle"", ""IsActive""
                                 FROM ""NDT_BundleFormationChart""
                                 WHERE ""Mill_ID"" = @millId AND ""IsActive"" = true AND ""Pipe_Size"" IS NULL
                                 LIMIT 1";
                        
                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@millId", millId);
                            using (var rdr = cmd.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    var chart = new NDTBundleFormationChart
                                    {
                                        NDTBundleFormationChart_ID = Convert.ToInt32(rdr["NDTBundleFormationChart_ID"]),
                                        Mill_ID = Convert.ToInt32(rdr["Mill_ID"]),
                                        Pipe_Size = null,
                                        NDT_PcsPerBundle = Convert.ToInt32(rdr["NDT_PcsPerBundle"]),
                                        IsActive = Convert.ToBoolean(rdr["IsActive"])
                                    };
                                    
                                    Console.WriteLine($"✅ Fallback to default: NDT_PcsPerBundle = {chart.NDT_PcsPerBundle}");
                                    return chart;
                                }
                            }
                        }
                    }
                }
                return null;
            }, null, "GetNDTFormationChart");
        }

        // PO Plans
        public POPlan GetPOPlan(int poPlanId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"SELECT ""PO_Plan_ID"", ""PLC_POID"", ""PO_No"", ""Pipe_Type"", ""Pipe_Size"", ""PcsPerBundle"", ""Pipe_Len"", ""PipeWt_per_mtr"", ""SAP_Type"", ""Shop_ID""
                                                    FROM ""PO_Plan""
                                                    WHERE ""PO_Plan_ID"" = @poPlanId", conn))
                {
                    cmd.Parameters.AddWithValue("@poPlanId", poPlanId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            return new POPlan
                            {
                                PO_Plan_ID = Convert.ToInt32(rdr["PO_Plan_ID"]),
                                PLC_POID = rdr["PLC_POID"] == DBNull.Value ? null : (int?)Convert.ToInt32(rdr["PLC_POID"]),
                                PO_No = rdr["PO_No"]?.ToString() ?? "",
                                Pipe_Type = rdr["Pipe_Type"]?.ToString(),
                                Pipe_Size = rdr["Pipe_Size"]?.ToString(),
                                PcsPerBundle = Convert.ToInt32(rdr["PcsPerBundle"]),
                                Pipe_Len = Convert.ToDecimal(rdr["Pipe_Len"]),
                                PipeWt_per_mtr = Convert.ToDecimal(rdr["PipeWt_per_mtr"]),
                                SAP_Type = rdr["SAP_Type"]?.ToString(),
                                Shop_ID = rdr["Shop_ID"] == DBNull.Value ? null : (int?)Convert.ToInt32(rdr["Shop_ID"])
                            };
                        }
                    }
                }
            }
            return null;
        }

        public List<POPlan> GetPOPlans()
        {
            return SafeExecute(() =>
            {
                var poPlans = new List<POPlan>();
                using (var conn = GetConnection())
                {
                    conn.Open();
                using (var cmd = new NpgsqlCommand(@"SELECT ""PO_Plan_ID"", ""PLC_POID"", ""PO_No"", ""Pipe_Type"", ""Pipe_Size"", ""PcsPerBundle"", ""Pipe_Len"", ""PipeWt_per_mtr"", ""SAP_Type"", ""Shop_ID""
                                                    FROM ""PO_Plan""
                                                    ORDER BY ""PO_Plan_ID"" DESC", conn))
                {
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            poPlans.Add(new POPlan
                            {
                                PO_Plan_ID = Convert.ToInt32(rdr["PO_Plan_ID"]),
                                PLC_POID = rdr["PLC_POID"] == DBNull.Value ? null : (int?)Convert.ToInt32(rdr["PLC_POID"]),
                                PO_No = rdr["PO_No"]?.ToString() ?? "",
                                Pipe_Type = rdr["Pipe_Type"]?.ToString(),
                                Pipe_Size = rdr["Pipe_Size"]?.ToString(),
                                PcsPerBundle = Convert.ToInt32(rdr["PcsPerBundle"]),
                                Pipe_Len = Convert.ToDecimal(rdr["Pipe_Len"]),
                                PipeWt_per_mtr = Convert.ToDecimal(rdr["PipeWt_per_mtr"]),
                                SAP_Type = rdr["SAP_Type"]?.ToString(),
                                Shop_ID = rdr["Shop_ID"] == DBNull.Value ? null : (int?)Convert.ToInt32(rdr["Shop_ID"])
                            });
                        }
                    }
                }
            }
            return poPlans;
            }, new List<POPlan>(), "GetPOPlans");
        }

        public void AddPOPlan(POPlan poPlan)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"INSERT INTO ""PO_Plan"" (""PLC_POID"", ""PO_No"", ""Pipe_Type"", ""Pipe_Size"", ""PcsPerBundle"", ""Pipe_Len"", ""PipeWt_per_mtr"", ""SAP_Type"", ""Shop_ID"")
                                                    VALUES (@plcPoid, @poNo, @pipeType, @pipeSize, @pcsPerBundle, @pipeLen, @pipeWtPerMtr, @sapType, @shopId)
                                                    RETURNING ""PO_Plan_ID""", conn))
                {
                    cmd.Parameters.AddWithValue("@plcPoid", (object)poPlan.PLC_POID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@poNo", poPlan.PO_No ?? "");
                    cmd.Parameters.AddWithValue("@pipeType", (object)poPlan.Pipe_Type ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pipeSize", (object)poPlan.Pipe_Size ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pcsPerBundle", poPlan.PcsPerBundle);
                    cmd.Parameters.AddWithValue("@pipeLen", poPlan.Pipe_Len);
                    cmd.Parameters.AddWithValue("@pipeWtPerMtr", poPlan.PipeWt_per_mtr);
                    cmd.Parameters.AddWithValue("@sapType", (object)poPlan.SAP_Type ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@shopId", (object)poPlan.Shop_ID ?? DBNull.Value);
                    poPlan.PO_Plan_ID = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void UpdatePOPlan(POPlan poPlan)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"UPDATE ""PO_Plan""
                                                    SET ""PLC_POID"" = @plcPoid, ""PO_No"" = @poNo, ""Pipe_Type"" = @pipeType, ""Pipe_Size"" = @pipeSize, ""PcsPerBundle"" = @pcsPerBundle, ""Pipe_Len"" = @pipeLen, ""PipeWt_per_mtr"" = @pipeWtPerMtr, ""SAP_Type"" = @sapType, ""Shop_ID"" = @shopId
                                                    WHERE ""PO_Plan_ID"" = @poPlanId", conn))
                {
                    cmd.Parameters.AddWithValue("@poPlanId", poPlan.PO_Plan_ID);
                    cmd.Parameters.AddWithValue("@plcPoid", (object)poPlan.PLC_POID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@poNo", poPlan.PO_No ?? "");
                    cmd.Parameters.AddWithValue("@pipeType", (object)poPlan.Pipe_Type ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pipeSize", (object)poPlan.Pipe_Size ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pcsPerBundle", poPlan.PcsPerBundle);
                    cmd.Parameters.AddWithValue("@pipeLen", poPlan.Pipe_Len);
                    cmd.Parameters.AddWithValue("@pipeWtPerMtr", poPlan.PipeWt_per_mtr);
                    cmd.Parameters.AddWithValue("@sapType", (object)poPlan.SAP_Type ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@shopId", (object)poPlan.Shop_ID ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeletePOPlan(int poPlanId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"DELETE FROM ""PO_Plan""
                                                    WHERE ""PO_Plan_ID"" = @poPlanId", conn))
                {
                    cmd.Parameters.AddWithValue("@poPlanId", poPlanId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Slits
        public Slit GetActiveSlit(int poPlanId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"SELECT ""Slit_ID"", ""PO_Plan_ID"", ""Slit_No"", ""Status"", ""Slit_NDT"", ""SlitMillStartTime""
                                FROM ""M1_Slit""
                                WHERE ""Status"" = 2";
                
                if (poPlanId > 0)
                {
                    query += @" AND ""PO_Plan_ID"" = @poPlanId";
                }
                
                query += @" ORDER BY ""SlitMillStartTime"" DESC
                           LIMIT 1";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    if (poPlanId > 0)
                    {
                        cmd.Parameters.AddWithValue("@poPlanId", poPlanId);
                    }
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            return new Slit
                            {
                                Slit_ID = Convert.ToInt32(rdr["Slit_ID"]),
                                PO_Plan_ID = Convert.ToInt32(rdr["PO_Plan_ID"]),
                                Slit_No = rdr["Slit_No"]?.ToString() ?? "",
                                Status = Convert.ToInt32(rdr["Status"]),
                                Slit_NDT = Convert.ToInt32(rdr["Slit_NDT"]),
                                SlitMillStartTime = Convert.ToDateTime(rdr["SlitMillStartTime"])
                            };
                        }
                    }
                }
            }
            return null;
        }

        public void UpdateSlit(Slit slit)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"UPDATE ""M1_Slit""
                                                    SET ""PO_Plan_ID"" = @poPlanId, ""Slit_No"" = @slitNo, ""Status"" = @status, ""Slit_NDT"" = @slitNdt, ""SlitMillStartTime"" = @slitMillStartTime
                                                    WHERE ""Slit_ID"" = @slitId", conn))
                {
                    cmd.Parameters.AddWithValue("@slitId", slit.Slit_ID);
                    cmd.Parameters.AddWithValue("@poPlanId", slit.PO_Plan_ID);
                    cmd.Parameters.AddWithValue("@slitNo", slit.Slit_No ?? "");
                    cmd.Parameters.AddWithValue("@status", slit.Status);
                    cmd.Parameters.AddWithValue("@slitNdt", slit.Slit_NDT);
                    cmd.Parameters.AddWithValue("@slitMillStartTime", slit.SlitMillStartTime);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void InitializeDummyData()
        {
            // Not needed for Supabase repository - data comes from database
        }
    }
}

