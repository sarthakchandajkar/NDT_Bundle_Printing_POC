using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using NDTBundlePOC.Core.Models;
using Npgsql;

namespace NDTBundlePOC.Core.Services
{
    public class SupabaseDataRepository : IDataRepository
    {
        private readonly string _connectionString;

        public SupabaseDataRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["ServerConnectionString"]?.ConnectionString 
                ?? throw new InvalidOperationException("ServerConnectionString not found in configuration");
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        // NDT Bundles
        public List<NDTBundle> GetNDTBundles()
        {
            var bundles = new List<NDTBundle>();
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
            return bundles;
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

        // Formation Chart
        public NDTBundleFormationChart GetNDTFormationChart(int millId, int? poPlanId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"SELECT ""NDTBundleFormationChart_ID"", ""Mill_ID"", ""PO_Plan_ID"", ""NDT_PcsPerBundle"", ""IsActive""
                                FROM ""NDT_BundleFormationChart""
                                WHERE ""Mill_ID"" = @millId AND ""IsActive"" = true";
                
                if (poPlanId.HasValue)
                {
                    query += @" AND (""PO_Plan_ID"" = @poPlanId OR ""PO_Plan_ID"" IS NULL)
                               ORDER BY ""PO_Plan_ID"" DESC NULLS LAST
                               LIMIT 1";
                }
                else
                {
                    query += @" AND ""PO_Plan_ID"" IS NULL
                               LIMIT 1";
                }

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@millId", millId);
                    if (poPlanId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@poPlanId", poPlanId.Value);
                    }
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            return new NDTBundleFormationChart
                            {
                                NDTBundleFormationChart_ID = Convert.ToInt32(rdr["NDTBundleFormationChart_ID"]),
                                Mill_ID = Convert.ToInt32(rdr["Mill_ID"]),
                                PO_Plan_ID = rdr["PO_Plan_ID"] == DBNull.Value ? null : (int?)Convert.ToInt32(rdr["PO_Plan_ID"]),
                                NDT_PcsPerBundle = Convert.ToInt32(rdr["NDT_PcsPerBundle"]),
                                IsActive = Convert.ToBoolean(rdr["IsActive"])
                            };
                        }
                    }
                }
            }
            return null;
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

