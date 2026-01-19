namespace IIOTReport
{
    using System;
    using System.ComponentModel;
    using System.Configuration;
    using System.Data.SqlClient;
    using System.Drawing;
    using System.Windows.Forms;
    using Telerik.Reporting;
    using Telerik.Reporting.Drawing;

    /// <summary>
    /// Summary description for Rpt_Sticker.
    /// </summary>
    public partial class Rpt_MillLabel : Telerik.Reporting.Report
    {
        public Rpt_MillLabel()
        {
            InitializeComponent();
        }

        private void Rpt_MillLabel_NeedDataSource(object sender, EventArgs e)
        {
            Telerik.Reporting.Processing.Report objReport = (Telerik.Reporting.Processing.Report)sender;
            string Mill_Line = objReport.Parameters["MillLine"].Value.ToString();
            Int32 PO_Plan_Id = Int32.Parse(objReport.Parameters["MillPOId"].Value.ToString());
            Int32 BundleID = Int32.Parse(objReport.Parameters["MillBundleID"].Value.ToString());
            bool isReprint = Convert.ToBoolean(objReport.Parameters["isReprint"].Value.ToString());

            var connectionString = "";
            if (ConfigurationManager.AppSettings["DefaultConnection"] != null)
                connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            else
                connectionString = objReport.Parameters["ConnectionString"].Value.ToString();

            SqlConnection sqlcon = new SqlConnection(connectionString);
            SqlCommand sqlcmd = new SqlCommand
            {
                Connection = sqlcon
            };
            sqlcon.Open();
            
            string bundleNo = "";

            //Get PO_Pickling details:
            sqlcmd.CommandText = @"select pop.PO_No as PONo, mb.Bundle_No as BundleNo, isnull(pop.POSpecification,'') as POSpecification,                            
                                isnull(pop.Pipe_Type,'') as PipeType, pop.Pipe_Size as PipeSize , pop.PcsPerBundle as PcsPerBundle,
                                iif(Parent_BundleNo <> Bundle_No, isnull(mb.LenPerPipe, 0), pop.Pipe_Len) as PipeLen
                                from PO_Plan pop
                                left outer join M" + Mill_Line + "_Bundles mb on mb.PO_Plan_ID = pop.PO_Plan_ID " +
                                "where pop.PO_Plan_ID = " + PO_Plan_Id.ToString() + " and mb.Bundle_ID = " + BundleID.ToString(); //,isnull(mb.HeatNumber, '') HeatNumber, pop.Pipe_Len as PipeLen,

            using (SqlDataReader rdr = sqlcmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    bundleNo = rdr["BundleNo"].ToString();
                    this.textSpecification.Value = rdr["POSpecification"].ToString();
                    this.textType.Value = rdr["PipeType"].ToString();
                    this.textSize.Value = rdr["PipeSize"].ToString() + "''";
                    this.textLen.Value = rdr["PipeLen"].ToString() + "'";
                   // this.textPcsBund.Value = rdr["PcsPerBundle"].ToString();
                    //this.textBox3.Value = rdr["HeatNumber"].ToString();
                }
                rdr.Close();
            }

            sqlcmd.CommandText = "select top 1 ms.Slit_No from M" + Mill_Line + "_Slit ms join M" + Mill_Line + "_Bundles mb on mb.Slit_ID = ms.Slit_ID where mb.Bundle_No = '" + bundleNo + "' order by mb.Bundle_ID";
            this.textBox3.Value = sqlcmd.ExecuteScalar().ToString();

            sqlcmd.CommandText = "select isnull((select sum(OK) from M" + Mill_Line + "_Bundles where Bundle_No = '" + bundleNo + "' ), 0) as PcsBundle";
            this.textPcsBund.Value = sqlcmd.ExecuteScalar().ToString();

            this.textBundleNo.Value = bundleNo;
            this.barcode1.Value = bundleNo;
            this.barcode2.Value = bundleNo;

            if (isReprint)
            {
                //sqlcmd.CommandText = "UPDATE M" + Mill_Line + "_Bundles SET LastReprintDttm = GETDATE() where Bundle_ID = '" + ((object[])((object[])BundleId)[j])[0].ToString() + "'";
                sqlcmd.CommandText = "UPDATE M" + Mill_Line + "_Bundles SET LastReprintDttm = GETDATE() WHERE Bundle_No = '" + bundleNo + "'";
                sqlcmd.ExecuteNonQuery();
                this.reprintInd.Value = "R";
            }
            else
                this.reprintInd.Value = "";

            sqlcmd.CommandText = "update M" + Mill_Line + @"_Bundles set [Status] = iif([Status] < 3, 3, [Status]) where Bundle_No = '" + bundleNo + "'";
            sqlcmd.ExecuteNonQuery();

            sqlcon.Close();
            sqlcon.Dispose();

        }
    }
}