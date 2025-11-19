-- ============================================================================
-- Fixed Stored Procedure: SP_DeleteRequest
-- ============================================================================
-- This is the corrected version of the SP_DeleteRequest stored procedure.
--
-- ISSUE FIXED:
-- Removed RAISE_APPLICATION_ERROR that was preventing OUT parameters from being
-- properly returned to the C# application. The issue occurred when a request ID
-- was not found - the exception would prevent the application from receiving
-- the error status and message through the OUT parameters.
--
-- CHANGES:
-- 1. Removed RAISE_APPLICATION_ERROR from the main logic
-- 2. Handle "not found" case gracefully by setting p_status=0 and appropriate message
-- 3. Only use EXCEPTION block for unexpected errors
-- 4. Now properly returns error status via OUT parameters instead of throwing exceptions
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
        -- Request not found - return error status gracefully
        -- DO NOT use RAISE_APPLICATION_ERROR as it prevents OUT parameters from being returned
        ROLLBACK;
        p_status := 0;
        p_message := 'Request ID not found: ' || p_request_id;
    ELSE
        -- Delete successful - commit and return success status
        COMMIT;
        p_status := 1;
        p_message := 'Deleted';
    END IF;

EXCEPTION
    WHEN OTHERS THEN
        -- Handle unexpected errors
        ROLLBACK;
        p_status := 0;
        p_message := 'Failed to delete: ' || SQLERRM;
        -- Do NOT re-raise - let the procedure complete and return error via OUT parameters
END SP_DeleteRequest;
/

-- Show compilation errors if any
SHOW ERRORS PROCEDURE P_IRT_REPORTHELPERTOOL.SP_DeleteRequest;
