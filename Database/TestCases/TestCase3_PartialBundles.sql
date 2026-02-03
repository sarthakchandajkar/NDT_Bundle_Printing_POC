-- Test Case 3: Partial bundles for both OK and NDT
-- Expected Output: 3 OK tags (20+20+2 pieces), 1 NDT tag (20 pieces - partial)
-- PLC Counts: OK = 42, NDT = 20

-- Cleanup: Close all existing slits and clear test data
-- Note: OK bundles are stored in-memory only, so they don't need database cleanup
UPDATE "M1_Slit" SET "Status" = 3 WHERE "Slit_No" LIKE 'SLIT_TEST_%';
DELETE FROM "M1_NDTBundles" WHERE "Bundle_No" LIKE '%TEST%' OR "Bundle_No" LIKE 'PO_TEST%' OR "Bundle_No" LIKE '0%';
DELETE FROM "PO_Plan" WHERE "PO_No" LIKE 'PO_TEST%';

-- Create PO_Plan for Test Case 3 (using DO block to handle insert/update)
DO $$
DECLARE
    v_po_plan_id INTEGER;
BEGIN
    SELECT "PO_Plan_ID" INTO v_po_plan_id
    FROM "PO_Plan"
    WHERE "PO_No" = 'PO_TEST_PARTIAL_BUNDLES';
    
    IF v_po_plan_id IS NULL THEN
        INSERT INTO "PO_Plan" (
            "PLC_POID", "PO_No", "Pipe_Type", "Pipe_Size", "PcsPerBundle", 
            "Pipe_Len", "PipeWt_per_mtr", "SAP_Type", "Shop_ID"
        ) VALUES (
            2003, 'PO_TEST_PARTIAL_BUNDLES', 'X65', '3.0', 20, 
            12.0, 2.5, 'SAP_TEST', 1
        ) RETURNING "PO_Plan_ID" INTO v_po_plan_id;
    ELSE
        UPDATE "PO_Plan"
        SET "PcsPerBundle" = 20, "Pipe_Size" = '3.0', "Shop_ID" = 1
        WHERE "PO_Plan_ID" = v_po_plan_id;
    END IF;
END $$;

-- Create Active Slit
DO $$
DECLARE
    v_po_plan_id INTEGER;
    v_slit_id INTEGER;
BEGIN
    SELECT "PO_Plan_ID" INTO v_po_plan_id
    FROM "PO_Plan"
    WHERE "PO_No" = 'PO_TEST_PARTIAL_BUNDLES';
    
    IF v_po_plan_id IS NOT NULL THEN
        SELECT "Slit_ID" INTO v_slit_id
        FROM "M1_Slit"
        WHERE "Slit_No" = 'SLIT_TEST_PARTIAL_BUNDLES';
        
        IF v_slit_id IS NULL THEN
            INSERT INTO "M1_Slit" ("PO_Plan_ID", "Slit_No", "Status", "Slit_NDT", "SlitMillStartTime")
            VALUES (v_po_plan_id, 'SLIT_TEST_PARTIAL_BUNDLES', 2, 0, CURRENT_TIMESTAMP);
        ELSE
            UPDATE "M1_Slit"
            SET "PO_Plan_ID" = v_po_plan_id, "Status" = 2, "Slit_NDT" = 0, "SlitMillStartTime" = CURRENT_TIMESTAMP
            WHERE "Slit_ID" = v_slit_id;
        END IF;
    END IF;
END $$;

