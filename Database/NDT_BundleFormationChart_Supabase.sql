-- =============================================
-- NDT Bundle Formation Chart Table for Supabase (PostgreSQL)
-- =============================================
-- This table stores the NDT pieces per bundle configuration based on pipe size
-- Execute this script in your Supabase SQL Editor

-- Drop table if exists (for re-creation)
DROP TABLE IF EXISTS "NDT_BundleFormationChart" CASCADE;

-- Create NDT_BundleFormationChart table
CREATE TABLE "NDT_BundleFormationChart" (
    "NDTBundleFormationChart_ID" SERIAL PRIMARY KEY,
    "Mill_ID" INTEGER NOT NULL,
    "Pipe_Size" DECIMAL(10,2) NULL, -- NULL means default for all sizes
    "NDT_PcsPerBundle" INTEGER NOT NULL,
    "IsActive" BOOLEAN DEFAULT true,
    "CreatedDate" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "ModifiedDate" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for better query performance
CREATE INDEX "IX_NDT_BundleFormationChart_Mill_Size" 
    ON "NDT_BundleFormationChart"("Mill_ID", "Pipe_Size");

CREATE INDEX "IX_NDT_BundleFormationChart_IsActive" 
    ON "NDT_BundleFormationChart"("IsActive");

-- Insert default and size-specific configurations
-- Default configuration (Pipe_Size = NULL means applies to all sizes not specifically defined)
INSERT INTO "NDT_BundleFormationChart" ("Mill_ID", "Pipe_Size", "NDT_PcsPerBundle", "IsActive")
VALUES 
    (1, NULL, 20, true); -- Default: 20 pieces per bundle for all sizes

-- Size-specific configurations
INSERT INTO "NDT_BundleFormationChart" ("Mill_ID", "Pipe_Size", "NDT_PcsPerBundle", "IsActive")
VALUES 
    (1, 0.5, 250, true),
    (1, 0.75, 180, true),
    (1, 1.0, 150, true),
    (1, 1.25, 140, true),
    (1, 1.5, 120, true),
    (1, 2.0, 80, true),
    (1, 2.4, 60, true),
    (1, 2.5, 65, true),
    (1, 3.0, 45, true),
    (1, 3.5, 40, true),
    (1, 4.0, 35, true),
    (1, 5.0, 25, true),
    (1, 6.0, 20, true),
    (1, 8.0, 13, true);

-- Add comment to table
COMMENT ON TABLE "NDT_BundleFormationChart" IS 'Stores NDT pieces per bundle configuration based on pipe size. NULL Pipe_Size means default for all sizes.';

-- Add comments to columns
COMMENT ON COLUMN "NDT_BundleFormationChart"."Pipe_Size" IS 'Pipe size in inches. NULL means default configuration for all sizes not specifically defined.';
COMMENT ON COLUMN "NDT_BundleFormationChart"."NDT_PcsPerBundle" IS 'Number of NDT pieces required per bundle for this size';
COMMENT ON COLUMN "NDT_BundleFormationChart"."Mill_ID" IS 'Mill identifier (1, 2, 3, etc.)';

-- Create a function to automatically update ModifiedDate
CREATE OR REPLACE FUNCTION update_modified_date()
RETURNS TRIGGER AS $$
BEGIN
    NEW."ModifiedDate" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger to update ModifiedDate on update
CREATE TRIGGER "trg_NDT_BundleFormationChart_UpdateModifiedDate"
    BEFORE UPDATE ON "NDT_BundleFormationChart"
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_date();

-- Verify the data
SELECT 
    "NDTBundleFormationChart_ID",
    "Mill_ID",
    "Pipe_Size",
    "NDT_PcsPerBundle",
    "IsActive"
FROM "NDT_BundleFormationChart"
ORDER BY "Pipe_Size" NULLS LAST;

PRINT 'NDT_BundleFormationChart table created successfully with size-based configuration!';

