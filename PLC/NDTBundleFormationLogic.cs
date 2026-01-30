using System;
using System.Data;
using Npgsql;
using System.Diagnostics;
using System.Configuration;

namespace NDTBundlePOC.PLC
{
    /// <summary>
    /// NDT Bundle Formation Logic - Processes NDT cuts and forms bundles
    /// Integrate this into your PLCThread_TM.cs main loop
    /// </summary>
    public class NDTBundleFormationLogic
    {
        private int _millId;
        private int _previousNDTCut = 0;
        private int _currentNDTPO_Plan_ID = 0;
        private int _currentNDTBundleID = 0;

        public NDTBundleFormationLogic(int millId)
        {
            _millId = millId;
        }

        /// <summary>
        /// Process NDT cuts and form bundles
        /// Call this method in your main PLC reading loop
        /// </summary>
        public void Process_NDTBundleFormation(ref NpgsqlCommand sqlcmd, ushort currentNDTCut)
        {
            try
            {
                if (currentNDTCut > _previousNDTCut)
                {
                    int newNDTPcs = currentNDTCut - _previousNDTCut;
                    _previousNDTCut = currentNDTCut;

                    // Get current active PO and Slit
                    sqlcmd.CommandText = @"SELECT ""PO_Plan_ID"", ""Slit_ID"" 
                                           FROM ""M" + _millId.ToString() + @"_Slit"" 
                                           WHERE ""Status"" = 2 
                                           ORDER BY ""SlitMillStartTime"" DESC
                                           LIMIT 1";

                    using (NpgsqlDataReader rdr = sqlcmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            _currentNDTPO_Plan_ID = Convert.ToInt32(rdr["PO_Plan_ID"]);
                            int currentSlitID = Convert.ToInt32(rdr["Slit_ID"]);
                            rdr.Close();

                            // Get NDT Pcs per bundle from chart
                            sqlcmd.CommandText = @"SELECT COALESCE(
                                (SELECT ""NDT_PcsPerBundle"" 
                                 FROM ""NDT_BundleFormationChart"" 
                                 WHERE ""Mill_ID"" = " + _millId.ToString() + @" 
                                   AND (""PO_Plan_ID"" = " + _currentNDTPO_Plan_ID.ToString() + @" OR ""PO_Plan_ID"" IS NULL)
                                   AND ""IsActive"" = 1
                                 ORDER BY ""PO_Plan_ID"" DESC NULLS LAST
                                 LIMIT 1), 
                                (SELECT ""NDT_PcsPerBundle"" 
                                 FROM ""NDT_BundleFormationChart"" 
                                 WHERE ""Mill_ID"" = " + _millId.ToString() + @" 
                                   AND ""PO_Plan_ID"" IS NULL 
                                   AND ""IsActive"" = 1
                                 LIMIT 1), 10)"; // Default to 10 if no config found

                            int requiredNDTPcs = Convert.ToInt32(sqlcmd.ExecuteScalar());

                            // Get current active NDT bundle
                            sqlcmd.CommandText = @"SELECT ""NDTBundle_ID"", ""Bundle_No"", ""NDT_Pcs"", ""Batch_No""
                                                  FROM ""M" + _millId.ToString() + @"_NDTBundles""
                                                  WHERE ""PO_Plan_ID"" = " + _currentNDTPO_Plan_ID.ToString() + @"
                                                    AND ""Status"" = 1
                                                  ORDER BY ""BundleStartTime"" DESC
                                                  LIMIT 1";

                            using (NpgsqlDataReader bundleRdr = sqlcmd.ExecuteReader())
                            {
                                if (bundleRdr.Read())
                                {
                                    _currentNDTBundleID = Convert.ToInt32(bundleRdr["NDTBundle_ID"]);
                                    string bundleNo = bundleRdr["Bundle_No"].ToString();
                                    int currentNDTPcs = Convert.ToInt32(bundleRdr["NDT_Pcs"]);
                                    string currentBatchNo = bundleRdr["Batch_No"]?.ToString() ?? "";
                                    bundleRdr.Close();

                                    // Update current bundle
                                    int newTotalNDTPcs = currentNDTPcs + newNDTPcs;

                                    sqlcmd.CommandText = @"UPDATE ""M" + _millId.ToString() + @"_NDTBundles""
                                                           SET ""NDT_Pcs"" = " + newTotalNDTPcs.ToString() + @"
                                                           WHERE ""NDTBundle_ID"" = " + _currentNDTBundleID.ToString();
                                    sqlcmd.ExecuteNonQuery();

                                    // Check if bundle is complete
                                    if (newTotalNDTPcs >= requiredNDTPcs)
                                    {
                                        // End current bundle
                                        sqlcmd.CommandText = @"UPDATE ""M" + _millId.ToString() + @"_NDTBundles""
                                                               SET ""Status"" = 2, 
                                                                   ""BundleEndTime"" = CURRENT_TIMESTAMP,
                                                                   ""IsFullBundle"" = 1
                                                               WHERE ""NDTBundle_ID"" = " + _currentNDTBundleID.ToString();
                                        sqlcmd.ExecuteNonQuery();

                                        // Generate new batch number in series
                                        string newBatchNo = GenerateNDTBatchNumber(_currentNDTPO_Plan_ID, currentBatchNo, ref sqlcmd);

                                        // Create new bundle with new batch
                                        CreateNewNDTBundle(_currentNDTPO_Plan_ID, currentSlitID, newBatchNo, ref sqlcmd);
                                    }
                                }
                                else
                                {
                                    bundleRdr.Close();
                                    // No active bundle, create new one
                                    string newBatchNo = GenerateNDTBatchNumber(_currentNDTPO_Plan_ID, "", ref sqlcmd);
                                    CreateNewNDTBundle(_currentNDTPO_Plan_ID, currentSlitID, newBatchNo, ref sqlcmd);
                                }
                            }
                        }
                        else
                        {
                            rdr.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error in Process_NDTBundleFormation: " + ex.ToString());
            }
        }

        private string GenerateNDTBatchNumber(int poPlanId, string previousBatchNo, ref NpgsqlCommand sqlcmd)
        {
            if (string.IsNullOrEmpty(previousBatchNo))
            {
                // First batch for this PO
                sqlcmd.CommandText = "SELECT \"PO_No\" FROM \"PO_Plan\" WHERE \"PO_Plan_ID\" = " + poPlanId.ToString();
                string poNo = sqlcmd.ExecuteScalar()?.ToString() ?? "";
                return "NDT_" + poNo + "001";
            }
            else
            {
                // Increment batch number
                if (previousBatchNo.Contains("_"))
                {
                    string[] parts = previousBatchNo.Split('_');
                    if (parts.Length >= 2)
                    {
                        string numberPart = parts[parts.Length - 1];
                        if (int.TryParse(numberPart, out int batchNum))
                        {
                            return parts[0] + "_" + (batchNum + 1).ToString("D3");
                        }
                    }
                }
                return previousBatchNo + "_001";
            }
        }

        private string CreateNewNDTBundle(int poPlanId, int slitId, string batchNo, ref NpgsqlCommand sqlcmd)
        {
            // Generate bundle number (similar to OK bundles: PO_No + sequential number)
            sqlcmd.CommandText = @"SELECT ""Bundle_No"" 
                                   FROM ""M" + _millId.ToString() + @"_NDTBundles"" 
                                   WHERE ""PO_Plan_ID"" = " + poPlanId.ToString() + @"
                                   ORDER BY ""NDTBundle_ID"" DESC
                                   LIMIT 1";
            var lastBundle = sqlcmd.ExecuteScalar();

            string newBundleNo = "";
            if (lastBundle != null && lastBundle != DBNull.Value)
            {
                string lastBundleStr = lastBundle.ToString();
                if (lastBundleStr.Length >= 3)
                {
                    string lastThree = lastBundleStr.Substring(Math.Max(0, lastBundleStr.Length - 3));
                    if (int.TryParse(lastThree, out int lastNum))
                    {
                        string prefix = lastBundleStr.Substring(0, lastBundleStr.Length - 3);
                        newBundleNo = prefix + (lastNum + 1).ToString("D3");
                    }
                    else
                    {
                        newBundleNo = lastBundleStr + "001";
                    }
                }
                else
                {
                    newBundleNo = lastBundleStr + "001";
                }
            }
            else
            {
                sqlcmd.CommandText = "SELECT \"PO_No\" FROM \"PO_Plan\" WHERE \"PO_Plan_ID\" = " + poPlanId.ToString();
                string poNo = sqlcmd.ExecuteScalar()?.ToString() ?? "";
                newBundleNo = poNo + "NDT001";
            }

            // Insert new bundle
            sqlcmd.CommandText = @"INSERT INTO ""M" + _millId.ToString() + @"_NDTBundles"" 
                                  (""PO_Plan_ID"", ""Slit_ID"", ""Bundle_No"", ""NDT_Pcs"", ""Batch_No"", ""Status"", ""BundleStartTime"")
                                  VALUES (" + poPlanId.ToString() + @", " +
                                  (slitId > 0 ? slitId.ToString() : "NULL") + @", 
                                  '" + newBundleNo.Replace("'", "''") + @"', 0, '" + batchNo.Replace("'", "''") + @"', 1, CURRENT_TIMESTAMP)";
            sqlcmd.ExecuteNonQuery();

            return newBundleNo;
        }

        /// <summary>
        /// Reset the previous NDT cut counter (call when starting new slit/PO)
        /// </summary>
        public void ResetNDTCutCounter()
        {
            _previousNDTCut = 0;
        }
    }
}

