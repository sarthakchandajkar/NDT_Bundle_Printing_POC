-- Test Case 3: Partial bundles for both OK and NDT
-- Expected Output: 3 OK tags (20+20+2 pieces), 1 NDT tag (20 pieces - partial)
-- PLC Counts: OK = 42, NDT = 20

-- Cleanup: Close all existing slits and clear test data
-- Note: OK bundles are stored in-memory only, so they don't need database cleanup
UPDATE "M1_Slit" SET "Status" = 3 WHERE "Slit_No" LIKE 'SLIT_TEST_%';
DELETE FROM "M1_NDTBundles" WHERE "Bundle_No" LIKE '%TEST%' OR "Bundle_No" LIKE 'PO_TEST%' OR "Bundle_No" LIKE '0%';
DELETE FROM "PO_Plan" WHERE "PO_No" LIKE 'PO_TEST%';

-- Create PO_Plan for Test Case 3
INSERT INTO "PO_Plan" (
    "PLC_POID", "PO_No", "Pipe_Type", "Pipe_Size", "PcsPerBundle", 
    "Pipe_Len", "PipeWt_per_mtr", "SAP_Type", "Shop_ID"
) VALUES (
    2003, 'PO_TEST_PARTIAL_BUNDLES', 'X65', '3.0', 20, 
    12.0, 2.5, 'SAP_TEST', 1
) 
ON CONFLICT ("PO_No") DO UPDATE
SET "PcsPerBundle" = 20, "Pipe_Size" = '3.0', "Shop_ID" = 1;

-- Create Active Slit
INSERT INTO "M1_Slit" ("PO_Plan_ID", "Slit_No", "Status", "Slit_NDT", "SlitMillStartTime")
SELECT "PO_Plan_ID", 'SLIT_TEST_PARTIAL_BUNDLES', 2, 0, CURRENT_TIMESTAMP
FROM "PO_Plan" WHERE "PO_No" = 'PO_TEST_PARTIAL_BUNDLES'
ON CONFLICT ("Slit_No") DO UPDATE
SET "PO_Plan_ID" = EXCLUDED."PO_Plan_ID", "Status" = 2, "Slit_NDT" = 0, "SlitMillStartTime" = CURRENT_TIMESTAMP;

