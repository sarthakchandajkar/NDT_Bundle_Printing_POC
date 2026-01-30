-- =============================================
-- M1_NDTBundles Table Migration for Supabase (PostgreSQL)
-- =============================================
-- This script adds missing columns to M1_NDTBundles table
-- Execute this script in your Supabase SQL Editor

-- Add Batch_No column if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'M1_NDTBundles' 
        AND column_name = 'Batch_No'
    ) THEN
        ALTER TABLE "M1_NDTBundles" 
        ADD COLUMN "Batch_No" VARCHAR(50) NULL;
        
        COMMENT ON COLUMN "M1_NDTBundles"."Batch_No" IS 'Batch number in series (e.g., "NDT_2410001")';
        
        RAISE NOTICE 'Added Batch_No column to M1_NDTBundles table';
    ELSE
        RAISE NOTICE 'Batch_No column already exists in M1_NDTBundles table';
    END IF;
END $$;

-- Add Parent_BundleNo column if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'M1_NDTBundles' 
        AND column_name = 'Parent_BundleNo'
    ) THEN
        ALTER TABLE "M1_NDTBundles" 
        ADD COLUMN "Parent_BundleNo" VARCHAR(50) NULL;
        
        COMMENT ON COLUMN "M1_NDTBundles"."Parent_BundleNo" IS 'Parent bundle number for split bundles';
        
        RAISE NOTICE 'Added Parent_BundleNo column to M1_NDTBundles table';
    ELSE
        RAISE NOTICE 'Parent_BundleNo column already exists in M1_NDTBundles table';
    END IF;
END $$;

-- Create index on Batch_No if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE tablename = 'M1_NDTBundles' 
        AND indexname = 'IX_M1_NDTBundles_Batch_No'
    ) THEN
        CREATE INDEX "IX_M1_NDTBundles_Batch_No" 
        ON "M1_NDTBundles"("Batch_No");
        
        RAISE NOTICE 'Created index IX_M1_NDTBundles_Batch_No';
    ELSE
        RAISE NOTICE 'Index IX_M1_NDTBundles_Batch_No already exists';
    END IF;
END $$;

-- Verify the columns exist
SELECT 
    column_name, 
    data_type, 
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'public' 
AND table_name = 'M1_NDTBundles'
AND column_name IN ('Batch_No', 'Parent_BundleNo')
ORDER BY column_name;

