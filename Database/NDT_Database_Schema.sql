-- =============================================
-- NDT Bundle Tag Printing - Database Schema
-- =============================================
-- Execute this script in your production database
-- (the database specified in your App.config ServerConnectionString)

USE [YourDatabaseName]; -- Replace with your actual database name
GO

-- =============================================
-- 1. Create NDT_BundleFormationChart Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NDT_BundleFormationChart]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[NDT_BundleFormationChart] (
        [NDTBundleFormationChart_ID] INT PRIMARY KEY IDENTITY(1,1),
        [Mill_ID] INT NOT NULL,
        [PO_Plan_ID] INT NULL, -- NULL means applies to all POs
        [NDT_PcsPerBundle] INT NOT NULL, -- Required NDT pieces per bundle
        [IsActive] BIT DEFAULT 1,
        [CreatedDate] DATETIME DEFAULT GETDATE(),
        [ModifiedDate] DATETIME DEFAULT GETDATE()
    );

    CREATE INDEX IX_NDT_BundleFormationChart_Mill_PO 
        ON [dbo].[NDT_BundleFormationChart]([Mill_ID], [PO_Plan_ID]);
    
    PRINT 'NDT_BundleFormationChart table created successfully.';
END
ELSE
BEGIN
    PRINT 'NDT_BundleFormationChart table already exists.';
END
GO

-- =============================================
-- 2. Create M1_NDTBundles Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[M1_NDTBundles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[M1_NDTBundles] (
        [NDTBundle_ID] INT PRIMARY KEY IDENTITY(1,1),
        [PO_Plan_ID] INT NOT NULL,
        [Slit_ID] INT NULL,
        [Bundle_No] NVARCHAR(50) NOT NULL,
        [NDT_Pcs] INT DEFAULT 0, -- Count of NDT pieces in this bundle
        [Bundle_Wt] DECIMAL(18,2) DEFAULT 0,
        [Status] INT DEFAULT 1, -- 1=Active, 2=Completed, 3=Printed, 4=Exported
        [IsFullBundle] BIT DEFAULT 0,
        [BundleStartTime] DATETIME DEFAULT GETDATE(),
        [BundleEndTime] DATETIME NULL,
        [OprDoneTime] DATETIME NULL,
        [LastReprintDttm] DATETIME NULL,
        [Parent_BundleNo] NVARCHAR(50) NULL,
        [Batch_No] NVARCHAR(50) NULL, -- Batch number in series (e.g., "NDT_2410001")
        [CreatedDate] DATETIME DEFAULT GETDATE(),
        
        FOREIGN KEY ([PO_Plan_ID]) REFERENCES [dbo].[PO_Plan]([PO_Plan_ID]),
        FOREIGN KEY ([Slit_ID]) REFERENCES [dbo].[M1_Slit]([Slit_ID])
    );

    CREATE INDEX IX_M1_NDTBundles_PO_Plan_ID ON [dbo].[M1_NDTBundles]([PO_Plan_ID]);
    CREATE INDEX IX_M1_NDTBundles_Status ON [dbo].[M1_NDTBundles]([Status]);
    CREATE INDEX IX_M1_NDTBundles_Bundle_No ON [dbo].[M1_NDTBundles]([Bundle_No]);
    CREATE INDEX IX_M1_NDTBundles_Batch_No ON [dbo].[M1_NDTBundles]([Batch_No]);
    
    PRINT 'M1_NDTBundles table created successfully.';
END
ELSE
BEGIN
    PRINT 'M1_NDTBundles table already exists.';
END
GO

-- =============================================
-- 3. Create Stored Procedure for Excel Export
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SP_SAPData_Mill_NDTBundle]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[SP_SAPData_Mill_NDTBundle];
GO

CREATE PROCEDURE [dbo].[SP_SAPData_Mill_NDTBundle]
    @MillId INT,
    @BundleNum NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        pp.PO_No AS "PO Number",
        @MillId AS "Mill",
        DATENAME(month, DATEADD(month, pp.PO_PlanDate, -1)) AS "PO_Mth",
        pp.Pipe_Grade AS "Pipe Grade",
        pp.Pipe_Size AS "Pipe Size",
        pp.Pipe_Thick AS "Pipe Thickness",
        pp.Pipe_Len AS "Pipe Length",
        pp.PipeWt_per_mtr AS "Pipe Wt/mtr",
        pp.Pipe_Type AS "Pipe Type",
        ISNULL(nbfc.NDT_PcsPerBundle, 10) AS "NDT Pcs. per Bundle",
        mb.Bundle_No AS "Bundle No",
        mb.Batch_No AS "Batch No",
        CASE WHEN mb.IsFullBundle = 1 THEN 'Full' ELSE 'Partial' END AS "Full or Partial",
        mb.NDT_Pcs AS "Act NDT Pcs. per Bundle",
        mb.Bundle_Wt AS "Bundle Wt.",
        mb.BundleStartTime AS "Bundle Start",
        mb.BundleEndTime AS "Bundle End",
        ISNULL(mb.OprDoneTime, GETDATE()) AS "Operator Done",
        s.Slit_No AS "Slit No",
        s.Slit_Thick AS "Slit Thickness",
        s.Slit_Width AS "Slit Width",
        CASE WHEN s.Is_NSS = 1 THEN 'Yes' ELSE 'No' END AS "Non Standard Slit",
        pp.PO_No + '_' + mb.Bundle_No + '_' + FORMAT(GETDATE(), 'yyyyMMddHHmmss') AS "FileName"
    FROM [dbo].[M1_NDTBundles] mb
    INNER JOIN [dbo].[PO_Plan] pp ON pp.PO_Plan_ID = mb.PO_Plan_ID
    LEFT JOIN [dbo].[M1_Slit] s ON s.Slit_ID = mb.Slit_ID
    LEFT JOIN [dbo].[NDT_BundleFormationChart] nbfc ON nbfc.Mill_ID = @MillId 
        AND (nbfc.PO_Plan_ID = mb.PO_Plan_ID OR nbfc.PO_Plan_ID IS NULL)
        AND nbfc.IsActive = 1
    WHERE mb.Bundle_No = @BundleNum
    ORDER BY mb.NDTBundle_ID;

    -- Update bundle status and OprDoneTime
    UPDATE [dbo].[M1_NDTBundles] 
    SET [Status] = 4, 
        [OprDoneTime] = GETDATE()
    WHERE [Bundle_No] = @BundleNum
      AND [Status] = 3;
END
GO

PRINT 'SP_SAPData_Mill_NDTBundle stored procedure created successfully.';
GO

-- =============================================
-- 4. Add Printer Configuration
-- =============================================
-- Add NDT printer entry to PlantDevice table
-- Update the IP address and port with your actual printer settings
IF NOT EXISTS (SELECT * FROM [dbo].[PlantDevice] WHERE [DeviceAbbr] = 'M1NDTPrinter')
BEGIN
    INSERT INTO [dbo].[PlantDevice] ([DeviceName], [DeviceAbbr], [DeviceType], [IPAddress], [Port], [IsActive], [ShopID])
    VALUES ('Honeywell_PD45S_NDT', 'M1NDTPrinter', 'Printer', '192.168.1.200', 9100, 1, 1);
    
    PRINT 'NDT Printer configuration added to PlantDevice table.';
    PRINT 'IMPORTANT: Update the IPAddress in PlantDevice table with your actual printer IP address.';
END
ELSE
BEGIN
    PRINT 'NDT Printer configuration already exists in PlantDevice table.';
END
GO

-- =============================================
-- 5. Sample Data (Optional - for testing)
-- =============================================
-- Uncomment to insert sample NDT Bundle Formation Chart data
/*
INSERT INTO [dbo].[NDT_BundleFormationChart] ([Mill_ID], [PO_Plan_ID], [NDT_PcsPerBundle], [IsActive])
VALUES 
    (1, NULL, 10, 1),  -- Default: 10 pieces per bundle for all POs in Mill 1
    (1, 1, 15, 1);      -- Specific: 15 pieces per bundle for PO_Plan_ID = 1 in Mill 1
*/

PRINT 'NDT Database Schema setup completed successfully!';
PRINT 'Next steps:';
PRINT '1. Update PlantDevice table with your actual printer IP address';
PRINT '2. Insert NDT_BundleFormationChart data for your mills';
PRINT '3. Ensure PO_Plan and M1_Slit tables exist and have data';
GO

