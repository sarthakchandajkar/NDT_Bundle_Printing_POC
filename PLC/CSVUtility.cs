using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NDTBundlePOC.PLC
{
    /// <summary>
    /// CSV Export Utility for NDT Bundles
    /// Add this method to your existing CSVUtility class
    /// </summary>
    public static class CSVUtility
    {
        /// <summary>
        /// Create CSV file for NDT Bundle export to SAP
        /// </summary>
        public static void CreateNDTBundleCSV(string millId, string bundleNo, SqlConnection scon)
        {
            try
            {
                string fileName = "";
                using (SqlCommand sql_cmd = new SqlCommand("SP_SAPData_Mill_NDTBundle", scon))
                {
                    sql_cmd.CommandType = CommandType.StoredProcedure;
                    sql_cmd.Parameters.Add(new SqlParameter("@MillId", Convert.ToInt32(millId)));
                    sql_cmd.Parameters.Add(new SqlParameter("@BundleNum", bundleNo));
                    using (SqlDataReader rdr = sql_cmd.ExecuteReader())
                    {
                        using (DataTable dt = new DataTable())
                        {
                            dt.Load(rdr, LoadOption.OverwriteChanges);
                            if (dt.Rows.Count > 0)
                            {
                                fileName = dt.Rows[0]["FileName"].ToString();
                                dt.Columns.Remove("FileName");
                                
                                // Update the path to match your project structure
                                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                                string csvPath = baseDir.Replace("FoxPasMill_" + millId, "PSR") + 
                                    "PAS-SAP\\To SAP\\TM\\NDT Bundle\\";
                                
                                // Ensure directory exists
                                if (!Directory.Exists(csvPath))
                                {
                                    Directory.CreateDirectory(csvPath);
                                }
                                
                                string fullPath = Path.Combine(csvPath, fileName + ".csv");
                                dt.ToCSV(fullPath);
                                dt.Clear();
                                
                                Trace.WriteLine("NDT Bundle CSV file created: " + fullPath);
                            }
                        }
                        rdr.Close();
                    }
                    sql_cmd.Dispose();
                }

                // Status = 4 and OprDoneTime = GETDATE() from M1_NDTBundles done in SP
                Trace.WriteLine("NDT Bundle CSV file created: " + bundleNo);
            }
            catch (Exception fex)
            {
                Trace.WriteLine("Error creating NDT Bundle CSV: " + fex.ToString());
            }
        }

        /// <summary>
        /// Extension method to convert DataTable to CSV
        /// Add this to your existing extension methods or create a new extension class
        /// </summary>
        public static void ToCSV(this DataTable dt, string filePath)
        {
            using (StreamWriter sw = new StreamWriter(filePath, false))
            {
                // Write headers
                sw.WriteLine(string.Join(",", dt.Columns.Cast<DataColumn>().Select(c => "\"" + c.ColumnName + "\"")));

                // Write data rows
                foreach (DataRow row in dt.Rows)
                {
                    sw.WriteLine(string.Join(",", row.ItemArray.Select(field => "\"" + field.ToString().Replace("\"", "\"\"") + "\"")));
                }
            }
        }
    }
}

