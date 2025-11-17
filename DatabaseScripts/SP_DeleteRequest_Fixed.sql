-- ============================================================================
-- Fixed Stored Procedure: SP_DeleteRequest
-- ============================================================================
-- This is the corrected version of the SP_DeleteRequest stored procedure.
--
-- ISSUE FIXED:
-- Removed the RAISE statement from the EXCEPTION block. The original version
-- was re-throwing exceptions after setting output parameters, which prevented
-- the calling application from receiving proper error status and messages.
--
-- CHANGES:
-- 1. Removed "RAISE;" from the EXCEPTION handler
-- 2. Now properly returns p_status=0 and p_message on errors
-- 3. The calling application can now handle errors gracefully
--
-- DEPLOYMENT INSTRUCTIONS:
-- Run this script as the database owner to replace the existing procedure:
--   SQL> @SP_DeleteRequest_Fixed.sql
-- ============================================================================

CREATE OR REPLACE PROCEDURE P_IRT_REPORTHELPERTOOL.SP_DeleteRequest (
    p_request_id IN NUMBER,
    p_status OUT NUMBER,
    p_message OUT NVARCHAR2
)
AS
BEGIN
    -- Delete the request from IRT_Requests table
    DELETE FROM IRT_Requests
    WHERE RequestId = p_request_id;

    -- Check if any rows were deleted
    IF SQL%ROWCOUNT = 0 THEN
        RAISE_APPLICATION_ERROR(-20001, 'Request ID not found: ' || p_request_id);
    END IF;

    -- Commit the transaction
    COMMIT;

    -- Set success status
    p_status := 1;
    p_message := 'Deleted';

EXCEPTION
    WHEN OTHERS THEN
        -- Rollback the transaction on error
        ROLLBACK;

        -- Set error status and message
        p_status := 0;
        p_message := 'Failed to delete: ' || SQLERRM;

        -- IMPORTANT: Do NOT re-raise the exception here
        -- The output parameters will communicate the error to the calling application
        -- Removing RAISE allows the procedure to complete successfully
        -- and return the error status via output parameters

        -- REMOVED: RAISE;
END SP_DeleteRequest;
/

-- Show compilation errors if any
SHOW ERRORS PROCEDURE P_IRT_REPORTHELPERTOOL.SP_DeleteRequest;
