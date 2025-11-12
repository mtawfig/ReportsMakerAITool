# Oracle Database Integration Guide

## Overview

The Web API project has been successfully integrated with Oracle Database. All endpoints now retrieve and store data using Oracle stored procedures from the `P_IRT_REPORTHELPERTOOL` package.

## Changes Summary

### 1. NuGet Package Added
- **Oracle.ManagedDataAccess.Core** (Version 23.5.1) added to `AIReportsTool.API.csproj`

### 2. Configuration Changes

#### appsettings.json
Added Oracle connection string configuration:
```json
{
  "ConnectionStrings": {
    "OracleConnection": "User Id=YOUR_USER;Password=YOUR_PASSWORD;Data Source=YOUR_TNS_NAME"
  }
}
```

**IMPORTANT:** You must update the connection string with your actual Oracle credentials:
- `YOUR_USER`: Your Oracle database username
- `YOUR_PASSWORD`: Your Oracle database password
- `YOUR_TNS_NAME`: Your Oracle TNS name or connection string

Example:
```json
"OracleConnection": "User Id=myuser;Password=mypass123;Data Source=localhost:1521/XEPDB1"
```

### 3. New Database Service

#### AIReportsTool.API/Services/OracleDbService.cs
A new service class that handles all Oracle database operations:

**Key Methods:**
- `ExecuteStoredProcedureAsync()` - Executes procedures with REF CURSOR and returns DataTable
- `ExecuteStoredProcedureWithOutParamsAsync()` - Executes procedures with OUT parameters (for INSERT/UPDATE)
- `CreateRefCursorParameter()` - Helper to create REF CURSOR output parameters
- `CreateInputParameter()` - Helper to create input parameters
- `CreateOutputParameter()` - Helper to create output parameters

**Features:**
- Async/await pattern for better performance
- Comprehensive error logging
- Proper resource disposal with using statements
- Type-safe parameter creation

### 4. Service Registration

#### Program.cs
Registered OracleDbService as a Singleton:
```csharp
using AIReportsTool.API.Services;
builder.Services.AddSingleton<OracleDbService>();
```

### 5. Updated Controllers

#### AppsController.cs
- **Before:** Returned hardcoded list of 5 applications
- **After:** Calls `P_IRT_REPORTHELPERTOOL.SP_GetApplications`
- **Endpoint:** GET /api/Apps
- **Stored Procedure:**
  ```sql
  P_IRT_REPORTHELPERTOOL.SP_GetApplications(p_result OUT SYS_REFCURSOR)
  ```
- **Returns:** List of applications from IRT_APPLICATIONS table
- **Error Handling:** Returns 500 status code with error message on failure

#### RecentsController.cs
- **Before:** Returned hardcoded list of 10 recent requests
- **After:** Calls `P_IRT_REPORTHELPERTOOL.SP_GetRecentRequests`
- **Endpoint:** GET /api/Recents
- **Stored Procedure:**
  ```sql
  P_IRT_REPORTHELPERTOOL.SP_GetRecentRequests(
    p_limit IN NUMBER := 10,
    p_result OUT SYS_REFCURSOR
  )
  ```
- **Returns:** List of 10 most recent requests from IRT_REQUESTS table with application details
- **Error Handling:** Returns 500 status code with error message on failure

#### RequestController.cs
- **Before:** Returned hardcoded success response
- **After:** Calls `P_IRT_REPORTHELPERTOOL.SP_InsertRequest`
- **Endpoint:** POST /api/Request
- **Stored Procedure:**
  ```sql
  P_IRT_REPORTHELPERTOOL.SP_InsertRequest(
    p_app_id IN NUMBER,
    p_username IN VARCHAR2,
    p_subject IN VARCHAR2,
    p_request_text IN CLOB,
    p_response_format IN VARCHAR2 DEFAULT NULL,
    p_token IN VARCHAR2 DEFAULT NULL,
    p_request_id OUT NUMBER,
    p_status OUT NUMBER,
    p_message OUT VARCHAR2
  )
  ```
- **Parameters Sent:**
  - AppId from payload
  - Username (empty for now, until authentication is implemented)
  - Subject from payload
  - RequestText from payload
  - ResponseFormat (NULL - format selection not yet implemented in UI)
  - Token from payload
- **Returns:** Request ID, status, and message from stored procedure
- **Error Handling:** Returns appropriate HTTP status codes based on database response

## Database Schema Required

### Tables
You must create the following Oracle tables and package using the provided SQL scripts:

1. **IRT_APPLICATIONS** - Stores application definitions
   - Columns: APP_ID (PK), APP_NAME_EN, APP_NAME_AR, APP_URL, APP_ICON, CREATED_DATE, IS_ACTIVE

2. **IRT_REQUESTS** - Stores user requests and AI responses
   - Columns: REQUEST_ID (PK), APP_ID (FK), USERNAME, REQUEST_DATETIME, REQUEST_SUBJECT, REQUEST_TEXT, REQUEST_STATUS, RESPONSE_TEXT, RESPONSE_FORMAT, TOKEN, CREATED_DATE, UPDATED_DATE

### Package
The `P_IRT_REPORTHELPERTOOL` package must be created with all stored procedures as specified in the Oracle SQL script provided earlier.

## Setup Instructions

### Step 1: Configure Database Connection
1. Open `AIReportsTool.API/appsettings.json`
2. Update the `OracleConnection` string with your Oracle credentials
3. Test the connection string using SQL Developer or similar tool

### Step 2: Create Database Schema
1. Execute the Oracle SQL scripts to create:
   - Tables (IRT_APPLICATIONS, IRT_REQUESTS)
   - Sequences (SEQ_IRT_APP_ID, SEQ_IRT_REQUEST_ID)
   - Triggers (TRG_IRT_APP_ID, TRG_IRT_REQUEST_ID)
   - Package specification and body (P_IRT_REPORTHELPERTOOL)
2. Insert sample data into IRT_APPLICATIONS table
3. Verify the package compiles without errors

### Step 3: Restore NuGet Packages
```bash
cd AIReportsTool.API
dotnet restore
```

This will download the Oracle.ManagedDataAccess.Core package.

### Step 4: Build the API Project
```bash
cd AIReportsTool.API
dotnet build
```

Verify there are no compilation errors.

### Step 5: Run and Test
```bash
cd AIReportsTool.API
dotnet run
```

The API will start on:
- HTTP: http://localhost:5100
- HTTPS: https://localhost:5101

### Step 6: Test Endpoints
Use Swagger UI at https://localhost:5101/swagger to test:

1. **GET /api/Apps** - Should return applications from database
2. **GET /api/Recents** - Should return recent requests from database
3. **POST /api/Request** - Should insert a new request and return request ID

## Troubleshooting

### Connection Issues
- **Error:** "ORA-12154: TNS:could not resolve the connect identifier"
  - **Solution:** Verify your TNS name in the connection string
  - Check that TNSNAMES.ORA is configured correctly
  - Or use a direct connection string: `localhost:1521/XEPDB1`

- **Error:** "ORA-01017: invalid username/password"
  - **Solution:** Verify your username and password in appsettings.json

### Package Issues
- **Error:** "ORA-00904: invalid identifier"
  - **Solution:** Verify the package and procedures were created successfully
  - Check that column names match exactly (case-sensitive in queries)

### Data Not Showing
- **Issue:** No applications or recents appearing in UI
  - **Solution:** Verify data exists in IRT_APPLICATIONS table
  - Check that the stored procedures return data when called directly in SQL Developer
  - Review API logs for any exceptions

## Data Flow

### Application Loading
```
User loads page → JavaScript calls /Applications/GetApps (MVC)
→ MVC calls /api/Apps (API)
→ API calls SP_GetApplications
→ Oracle returns SYS_REFCURSOR
→ Service converts to DataTable
→ Controller maps to AppModel list
→ Returns JSON to MVC
→ MVC returns JSON to JavaScript
→ JavaScript renders in sidebar
```

### Recent Requests Loading
```
User loads page → JavaScript calls /Recents/GetRecents (MVC)
→ MVC calls /api/Recents (API)
→ API calls SP_GetRecentRequests(10)
→ Oracle returns SYS_REFCURSOR
→ Service converts to DataTable
→ Controller maps to RecentRequestModel list
→ Returns JSON to MVC
→ MVC returns JSON to JavaScript
→ JavaScript renders in sidebar
```

### Request Submission
```
User submits form → JavaScript calls /Request/Submit (MVC)
→ MVC calls /api/Request (API)
→ API calls SP_InsertRequest with all parameters
→ Oracle procedure inserts record and returns OUT parameters
→ Service extracts OUT parameters (request_id, status, message)
→ Controller returns response with request ID
→ Returns JSON to MVC
→ MVC returns JSON to JavaScript
→ JavaScript displays success message
```

## Next Steps (Future Enhancements)

1. **AI Integration:** Implement actual AI report generation instead of mock responses
2. **Authentication:** Add user authentication and pass real username to stored procedures
3. **Format Selection:** Capture selected format from UI and pass to SP_InsertRequest
4. **Response Update:** Implement background job to update REQUEST_STATUS and RESPONSE_TEXT after AI processing
5. **Error Handling:** Add more specific error codes and messages
6. **Logging:** Enhance logging for better debugging and monitoring
7. **Connection Pooling:** Configure Oracle connection pooling for better performance
8. **Retry Logic:** Add retry logic for transient database errors

## Security Considerations

1. **Connection String Security:**
   - Never commit actual credentials to source control
   - Use environment variables or Azure Key Vault in production
   - Use User Secrets for local development

2. **SQL Injection Protection:**
   - All database calls use parameterized stored procedures
   - No dynamic SQL construction

3. **Input Validation:**
   - All controllers validate input before calling stored procedures
   - Required field validation is in place

4. **Error Information:**
   - Detailed errors are logged but not exposed to clients
   - Generic error messages returned to prevent information disclosure

## File Reference

### Modified Files
- `AIReportsTool.API/AIReportsTool.API.csproj` - Added Oracle package reference
- `AIReportsTool.API/appsettings.json` - Added connection string
- `AIReportsTool.API/Program.cs` - Registered OracleDbService
- `AIReportsTool.API/Controllers/AppsController.cs` - Database integration
- `AIReportsTool.API/Controllers/RecentsController.cs` - Database integration
- `AIReportsTool.API/Controllers/RequestController.cs` - Database integration

### New Files
- `AIReportsTool.API/Services/OracleDbService.cs` - Database service layer

### Database Files (Created Previously)
- Oracle SQL scripts for tables, sequences, triggers, and package

## Validation Checklist

Before running the application, ensure:
- [ ] Oracle database is running and accessible
- [ ] IRT_APPLICATIONS table has data (at least 5 applications)
- [ ] P_IRT_REPORTHELPERTOOL package is compiled without errors
- [ ] Connection string is updated in appsettings.json
- [ ] NuGet packages are restored (`dotnet restore`)
- [ ] Project builds successfully (`dotnet build`)
- [ ] All three stored procedures (SP_GetApplications, SP_GetRecentRequests, SP_InsertRequest) work when tested in SQL Developer

## Support

If you encounter any issues:
1. Check the API logs for detailed error messages
2. Test stored procedures directly in SQL Developer
3. Verify connection string format
4. Ensure Oracle client libraries are installed
5. Check firewall rules if connecting to remote Oracle database

---

**Document Version:** 1.0
**Last Updated:** 2025-11-12
**ASP.NET Core Version:** 9.0
**Oracle Client Version:** 23.5.1
