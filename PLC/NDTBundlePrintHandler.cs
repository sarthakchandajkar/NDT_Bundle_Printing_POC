using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Configuration;
using System.Threading;
using Telerik.Reporting;
using Telerik.Reporting.Processing;
using System.Drawing.Printing;

namespace NDTBundlePOC.PLC
{
    /// <summary>
    /// NDT Bundle Print Handler - Handles printing of NDT bundle tags
    /// Integrate this into your PLCThread_TM.cs main loop
    /// </summary>
    public class NDTBundlePrintHandler
    {
        private int _millId;

        public NDTBundlePrintHandler(int millId)
        {
            _millId = millId;
        }

        /// <summary>
        /// Process NDT bundle print trigger from PLC
        /// Call this when L1L2_NDTBundleDone is true and L2L1_AckNDTBundleDone is false
        /// </summary>
        public void Process_NDTBundlePrint(ref SqlCommand sqlcmd, ushort slitIdFromPLC)
        {
            string slitIdStr = slitIdFromPLC.ToString();

            sqlcmd.CommandText = "SELECT Slit_ID FROM M" + _millId.ToString() +
                                "_Slit WHERE [Status] = 2 AND PLC_SlitID = " + slitIdStr;
            var Slit_ID = sqlcmd.ExecuteScalar();

            if (Slit_ID == null || Slit_ID == DBNull.Value)
            {
                Trace.WriteLine("No Slit found for NDT bundle print with PLC_SlitID: " + slitIdStr);
                return;
            }

            // Get completed NDT bundle (Status = 2)
            sqlcmd.CommandText = @"SELECT TOP 1 
                                    NDTBundle_ID, Bundle_No, PO_Plan_ID
                                   FROM M" + _millId.ToString() + @"_NDTBundles
                                   WHERE Slit_ID = " + Slit_ID.ToString() + @"
                                     AND [Status] = 2
                                   ORDER BY BundleEndTime DESC";

            using (SqlDataReader rdr = sqlcmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    int bundleId = Convert.ToInt32(rdr["NDTBundle_ID"]);
                    string bundleNo = rdr["Bundle_No"].ToString();
                    int poPlanId = Convert.ToInt32(rdr["PO_Plan_ID"]);
                    rdr.Close();

                    // Print NDT bundle tag
                    ThreadPool.QueueUserWorkItem(new WaitCallback(_ReportNDTPrint),
                        new NDTPrintData
                        {
                            BundleNo = bundleNo,
                            BundleID = bundleId,
                            MillNo = _millId,
                            POPlanID = poPlanId,
                            Reprint = false
                        });

                    // Update bundle status to 3 (Printed)
                    sqlcmd.CommandText = "UPDATE M" + _millId.ToString() +
                        "_NDTBundles SET [Status] = 3, OprDoneTime = GETDATE() WHERE Bundle_No = '" + bundleNo.Replace("'", "''") + "'";
                    sqlcmd.ExecuteNonQuery();

                    Trace.WriteLine("NDT Bundle print triggered: " + bundleNo);
                }
                else
                {
                    rdr.Close();
                    Trace.WriteLine("No NDT bundle found for print (Slit_ID: " + Slit_ID.ToString() + ")");
                }
            }
        }

        /// <summary>
        /// Process NDT bundle reprint trigger from PLC
        /// Call this when L1L2_NDTBundleReprint is true and L2L1_AckNDTBundleReprint is false
        /// </summary>
        public void Process_NDTBundleReprint(ref SqlCommand sqlcmd, ushort slitIdFromPLC)
        {
            string slitIdStr = slitIdFromPLC.ToString();

            sqlcmd.CommandText = "SELECT Slit_ID FROM M" + _millId.ToString() +
                                "_Slit WHERE [Status] = 2 AND PLC_SlitID = " + slitIdStr;
            var Slit_ID = sqlcmd.ExecuteScalar();

            if (Slit_ID == null || Slit_ID == DBNull.Value)
            {
                Trace.WriteLine("No Slit found for NDT bundle reprint with PLC_SlitID: " + slitIdStr);
                return;
            }

            // Get last printed NDT bundle
            sqlcmd.CommandText = @"SELECT TOP 1 
                                    NDTBundle_ID, Bundle_No, PO_Plan_ID
                                   FROM M" + _millId.ToString() + @"_NDTBundles
                                   WHERE Slit_ID = " + Slit_ID.ToString() + @"
                                     AND [Status] >= 3
                                   ORDER BY OprDoneTime DESC";

            using (SqlDataReader rdr = sqlcmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    int bundleId = Convert.ToInt32(rdr["NDTBundle_ID"]);
                    string bundleNo = rdr["Bundle_No"].ToString();
                    int poPlanId = Convert.ToInt32(rdr["PO_Plan_ID"]);
                    rdr.Close();

                    // Reprint NDT bundle tag
                    ThreadPool.QueueUserWorkItem(new WaitCallback(_ReportNDTPrint),
                        new NDTPrintData
                        {
                            BundleNo = bundleNo,
                            BundleID = bundleId,
                            MillNo = _millId,
                            POPlanID = poPlanId,
                            Reprint = true
                        });

                    // Update last reprint time
                    sqlcmd.CommandText = "UPDATE M" + _millId.ToString() +
                        "_NDTBundles SET LastReprintDttm = GETDATE() WHERE Bundle_No = '" + bundleNo.Replace("'", "''") + "'";
                    sqlcmd.ExecuteNonQuery();

                    Trace.WriteLine("NDT Bundle reprint triggered: " + bundleNo);
                }
                else
                {
                    rdr.Close();
                    Trace.WriteLine("No NDT bundle found for reprint (Slit_ID: " + Slit_ID.ToString() + ")");
                }
            }
        }

        private void _ReportNDTPrint(object data)
        {
            NDTPrintData pd = (NDTPrintData)data;
            string PrinterName = "";
            int BundleId = pd.BundleID;
            int POId = pd.POPlanID;

            try
            {
                using (SqlConnection sqlcon = new SqlConnection(
                    ConfigurationManager.ConnectionStrings["ServerConnectionString"].ConnectionString))
                {
                    sqlcon.Open();
                    using (SqlCommand sqlcmd = new SqlCommand("", sqlcon))
                    {
                        // Get NDT printer name
                        sqlcmd.CommandText = "SELECT DeviceName FROM PlantDevice WHERE DeviceAbbr = 'M" +
                            pd.MillNo.ToString() + "NDTPrinter'";
                        PrinterName = sqlcmd.ExecuteScalar()?.ToString() ?? "Honeywell_PD45S_NDT";
                    }
                    sqlcon.Close();
                }

                // Obtain printer settings
                PrinterSettings printerSettings = new PrinterSettings();
                StandardPrintController standardPrintController = new StandardPrintController();
                ReportProcessor reportProcessor = new ReportProcessor();
                reportProcessor.PrintController = standardPrintController;

                // Use NDT-specific report (create Rpt_NDTLabel report similar to Rpt_MillLabel)
                TypeReportSource typeReportSource = new TypeReportSource()
                {
                    TypeName = typeof(IIOTReport.Rpt_NDTLabel).AssemblyQualifiedName
                };

                typeReportSource.Parameters.Add(new Parameter("MillLine", pd.MillNo));
                typeReportSource.Parameters.Add(new Parameter("MillPOId", POId));
                typeReportSource.Parameters.Add(new Parameter("NDTBundleID", BundleId));
                typeReportSource.Parameters.Add(new Parameter("isReprint", pd.Reprint));
                typeReportSource.Parameters.Add(new Parameter("ConnectionString",
                    ConfigurationManager.ConnectionStrings["ServerConnectionString"].ConnectionString));

                printerSettings.PrinterName = PrinterName;
                printerSettings.MinimumPage = 1;

                reportProcessor.PrintReport(typeReportSource, printerSettings);

                Trace.WriteLine("NDT Bundle tag printed: " + pd.BundleNo);

                // Export to CSV after successful print
                using (SqlConnection sqlcon = new SqlConnection(
                    ConfigurationManager.ConnectionStrings["ServerConnectionString"].ConnectionString))
                {
                    sqlcon.Open();
                    CSVUtility.CreateNDTBundleCSV(pd.MillNo.ToString(), pd.BundleNo, sqlcon);
                    sqlcon.Close();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error printing NDT bundle tag: " + ex.ToString());
            }
        }
    }

    /// <summary>
    /// Data class for NDT print operations
    /// </summary>
    public class NDTPrintData
    {
        public string BundleNo { get; set; }
        public int BundleID { get; set; }
        public int MillNo { get; set; }
        public int POPlanID { get; set; }
        public bool Reprint { get; set; }
    }
}

