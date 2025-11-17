# Database Scripts

This folder contains SQL scripts for database schema and stored procedure updates.

## Delete Request Fix

### Issue

The `SP_DeleteRequest` stored procedure had a bug in its exception handling that prevented proper error communication with the application.

**Problem:** The procedure's `EXCEPTION` block ended with a `RAISE;` statement that re-threw exceptions after setting output parameters. This caused:

1. Exceptions to propagate to the C# application layer
2. Output parameters (`p_status`, `p_message`) not being properly returned
3. The API returning generic 500 errors instead of meaningful error messages
4. Poor user experience when delete operations failed

**Original Code (Problematic):**
```sql
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        p_status := 0;
        p_message := 'Failed to delete';
        RAISE;  -- ❌ This re-throws the exception!
END SP_DeleteRequest;
```

**Fixed Code:**
```sql
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        p_status := 0;
        p_message := 'Failed to delete: ' || SQLERRM;
        -- ✅ RAISE removed - procedure completes successfully and returns error status
END SP_DeleteRequest;
```

### Deployment

To deploy the fixed stored procedure:

1. Connect to your Oracle database as the schema owner
2. Run the fixed stored procedure script:

```bash
sqlplus username/password@database
SQL> @SP_DeleteRequest_Fixed.sql
```

Or use SQL Developer / SQL*Plus to execute the `SP_DeleteRequest_Fixed.sql` script.

### Verification

After deployment, verify the fix works:

1. Try deleting a request from the "Recent Requests" list in the application
2. The delete should work properly and show appropriate success/error messages
3. Check application logs - you should no longer see stored procedure exceptions for delete operations

### Technical Details

The C# application calls this stored procedure via the `ExecuteStoredProcedureWithOutParamsAsync` method in `OracleDbService.cs`. The method expects output parameters to be properly populated regardless of whether the operation succeeded or failed. The `RAISE;` statement prevented this from happening.

With the fix:
- Successful deletes: `p_status = 1`, `p_message = 'Deleted'`
- Failed deletes: `p_status = 0`, `p_message = 'Failed to delete: [error details]'`
- Application properly handles both cases without exceptions
