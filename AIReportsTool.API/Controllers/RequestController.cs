using Microsoft.AspNetCore.Mvc;
using AIReportsTool.API.Models;
using AIReportsTool.API.Services;
using Oracle.ManagedDataAccess.Client;
using System.Text.Json;

namespace AIReportsTool.API.Controllers
{
    /// <summary>
    /// Controller for handling report generation requests
    /// Accepts POST requests with request details and saves to Oracle database
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RequestController : ControllerBase
    {
        private readonly OracleDbService _dbService;
        private readonly GeminiService _geminiService;
        private readonly ApplicationService _applicationService;
        private readonly ILogger<RequestController> _logger;

        public RequestController(
            OracleDbService dbService,
            GeminiService geminiService,
            ApplicationService applicationService,
            ILogger<RequestController> logger)
        {
            _dbService = dbService;
            _geminiService = geminiService;
            _applicationService = applicationService;
            _logger = logger;
        }

        /// <summary>
        /// POST: api/request
        /// Submits a new report generation request
        /// Calls: P_IRT_REPORTHELPERTOOL.SP_InsertRequest
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitRequest([FromBody] RequestPayloadModel payload)
        {
            // TODO: Add authentication/authorization validation here when implementing security
            // TODO: Validate token when implementing security

            // Validate required fields
            if (payload == null)
            {
                return BadRequest(new RequestResponseModel
                {
                    Status = 0,
                    Message = "Invalid payload",
                    ResponseText = string.Empty
                });
            }

            if (payload.AppId <= 0)
            {
                return BadRequest(new RequestResponseModel
                {
                    Status = 0,
                    Message = "Invalid application ID",
                    ResponseText = string.Empty
                });
            }

            if (string.IsNullOrWhiteSpace(payload.Subject))
            {
                return BadRequest(new RequestResponseModel
                {
                    Status = 0,
                    Message = "Subject is required",
                    ResponseText = string.Empty
                });
            }

            if (string.IsNullOrWhiteSpace(payload.RequestText))
            {
                return BadRequest(new RequestResponseModel
                {
                    Status = 0,
                    Message = "Request text is required",
                    ResponseText = string.Empty
                });
            }

            try
            {
                // Call Gemini API to generate response
                _logger.LogInformation("Calling Gemini API for request: {Subject}", payload.Subject);
                string geminiResponse;

                try
                {
                    geminiResponse = await _geminiService.GenerateReportAsync(
                        "IntellegentReportMaker",
                        payload.Subject,
                        payload.RequestText
                    );
                }
                catch (Exception geminiEx)
                {
                    _logger.LogError(geminiEx, "Gemini API call failed");
                    geminiResponse = $"Error generating AI response: {geminiEx.Message}";
                }

                // Create parameters for stored procedure
                var appIdParam = OracleDbService.CreateInputParameter(
                    "p_app_id",
                    OracleDbType.Int32,
                    payload.AppId
                );

                var usernameParam = OracleDbService.CreateInputParameter(
                    "p_username",
                    OracleDbType.NVarchar2,
                    payload.Username ?? string.Empty
                );

                var subjectParam = OracleDbService.CreateInputParameter(
                    "p_subject",
                    OracleDbType.NVarchar2,
                    payload.Subject
                );

                var requestTextParam = OracleDbService.CreateInputParameter(
                    "p_request_text",
                    OracleDbType.NClob,
                    payload.RequestText
                );

                var responseFormatParam = OracleDbService.CreateInputParameter(
                    "p_response_format",
                    OracleDbType.NVarchar2,
                    DBNull.Value // Will be set when format selection is implemented in UI
                );

                var tokenParam = OracleDbService.CreateInputParameter(
                    "p_token",
                    OracleDbType.NVarchar2,
                    string.IsNullOrWhiteSpace(payload.Token) ? DBNull.Value : payload.Token
                );

                // Output parameters
                var requestIdParam = OracleDbService.CreateOutputParameter(
                    "p_request_id",
                    OracleDbType.Int32
                );

                var statusParam = OracleDbService.CreateOutputParameter(
                    "p_status",
                    OracleDbType.Int32
                );

                var messageParam = OracleDbService.CreateOutputParameter(
                    "p_message",
                    OracleDbType.NVarchar2,
                    4000
                );

                // Execute stored procedure to insert request
                var outParams = await _dbService.ExecuteStoredProcedureWithOutParamsAsync(
                    "P_IRT_REPORTHELPERTOOL.SP_InsertRequest",
                    appIdParam,
                    usernameParam,
                    subjectParam,
                    requestTextParam,
                    responseFormatParam,
                    tokenParam,
                    requestIdParam,
                    statusParam,
                    messageParam
                );

                // Extract output parameters
                var dbStatus = Convert.ToInt32(outParams["p_status"]);
                var dbMessage = outParams["p_message"]?.ToString() ?? "Unknown error";
                var requestId = outParams["p_request_id"] != DBNull.Value
                    ? Convert.ToInt32(outParams["p_request_id"])
                    : 0;

                if (dbStatus == 1)
                {
                    // Update the request with Gemini response
                    await UpdateRequestResponseAsync(requestId, geminiResponse);

                    var response = new RequestResponseModel
                    {
                        Status = 1,
                        Message = dbMessage,
                        ResponseText = geminiResponse
                    };

                    return Ok(response);
                }
                else
                {
                    // Database returned an error
                    var response = new RequestResponseModel
                    {
                        Status = 0,
                        Message = dbMessage,
                        ResponseText = string.Empty
                    };

                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting request to database");

                var response = new RequestResponseModel
                {
                    Status = 0,
                    Message = "Failed to submit request",
                    ResponseText = string.Empty
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Updates a request with the Gemini API response
        /// </summary>
        private async Task UpdateRequestResponseAsync(int requestId, string responseText)
        {
            try
            {
                var requestIdParam = OracleDbService.CreateInputParameter(
                    "p_request_id",
                    OracleDbType.Int32,
                    requestId
                );

                var responseTextParam = OracleDbService.CreateInputParameter(
                    "p_response_text",
                    OracleDbType.NClob,
                    responseText
                );

                var statusParam = OracleDbService.CreateInputParameter(
                    "p_status",
                    OracleDbType.Int32,
                    1 // Mark as completed
                );

                // Note: SP_UpdateRequestResponse has no OUT parameters, only IN parameters
                await _dbService.ExecuteStoredProcedureWithOutParamsAsync(
                    "P_IRT_REPORTHELPERTOOL.SP_UpdateRequestResponse",
                    requestIdParam,
                    responseTextParam,
                    statusParam
                );

                _logger.LogInformation("Updated request {RequestId} with Gemini response", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating request {RequestId} with response", requestId);
                // Don't throw - we still want to return the response to the user even if update fails
            }
        }

        /// <summary>
        /// POST: api/request/generate-query
        /// Generates an SQL query based on user request and application database structure
        /// </summary>
        [HttpPost("generate-query")]
        public async Task<IActionResult> GenerateQuery([FromBody] RequestPayloadModel payload)
        {
            // Validate required fields
            if (payload == null)
            {
                return BadRequest(new RequestResponseModel
                {
                    Status = 0,
                    Message = "Invalid payload",
                    ResponseText = string.Empty
                });
            }

            if (payload.AppId <= 0)
            {
                return BadRequest(new RequestResponseModel
                {
                    Status = 0,
                    Message = "Invalid application ID",
                    ResponseText = string.Empty
                });
            }

            if (string.IsNullOrWhiteSpace(payload.Subject))
            {
                return BadRequest(new RequestResponseModel
                {
                    Status = 0,
                    Message = "Subject is required",
                    ResponseText = string.Empty
                });
            }

            if (string.IsNullOrWhiteSpace(payload.RequestText))
            {
                return BadRequest(new RequestResponseModel
                {
                    Status = 0,
                    Message = "Request text is required",
                    ResponseText = string.Empty
                });
            }

            try
            {
                // Get application database details
                _logger.LogInformation("Getting database details for AppId: {AppId}", payload.AppId);
                var appDbDetails = await _applicationService.GetApplicationDatabaseDetails(payload.AppId);

                // Validate that database details were found
                if (appDbDetails.Id == 0)
                {
                    var errorResponse = new RequestResponseModel
                    {
                        Status = 0,
                        Message = "Application database configuration not found or invalid",
                        ResponseText = string.Empty
                    };
                    return BadRequest(errorResponse);
                }

                // Build the specialized prompt for Gemini
                var geminiPrompt = BuildQueryGenerationPrompt(
                    payload.Subject,
                    payload.RequestText,
                    appDbDetails.DbProvider,
                    appDbDetails.DbStructureJson
                );

                // Call Gemini API to generate query
                _logger.LogInformation("Calling Gemini API to generate SQL query for AppId: {AppId}", payload.AppId);

                string geminiResponse;

                try
                {
                    geminiResponse = await _geminiService.GenerateReportAsync(
                        "QueryGenerator",
                        payload.Subject,
                        geminiPrompt
                    );
                }
                catch (Exception geminiEx)
                {
                    _logger.LogError(geminiEx, "Gemini API call failed");
                    return Ok(new RequestResponseModel
                    {
                        Status = 0,
                        Message = "Error generating SQL query",
                        ResponseText = geminiEx.Message
                    });
                }

                // Extract SQL query from Gemini response
                var sqlQuery = ExtractSqlQueryFromResponse(geminiResponse);

                if (string.IsNullOrWhiteSpace(sqlQuery))
                {
                    _logger.LogWarning("Could not extract SQL query from Gemini response");
                    return Ok(new RequestResponseModel
                    {
                        Status = 0,
                        Message = "Failed to extract SQL query from AI response",
                        ResponseText = geminiResponse
                    });
                }

                // Log the generated SQL query
                _logger.LogInformation("Generated SQL Query: {SqlQuery}", sqlQuery);

                // Execute the SQL query based on database provider
                System.Data.DataTable queryResults;
                try
                {
                    queryResults = await ExecuteSqlQuery(
                        sqlQuery,
                        appDbDetails.DbProvider,
                        appDbDetails.DbConnection
                    );
                }
                catch (Exception queryEx)
                {
                    _logger.LogError(queryEx, "Error executing SQL query");
                    return Ok(new RequestResponseModel
                    {
                        Status = 0,
                        Message = $"Error executing query: {queryEx.Message}",
                        ResponseText = $"SQL Query:\n{sqlQuery}\n\nError: {queryEx.Message}"
                    });
                }

                // Convert DataTable to JSON-friendly format
                var resultData = ConvertDataTableToJson(queryResults);

                var response = new RequestResponseModel
                {
                    Status = 1,
                    Message = "Query executed successfully",
                    ResponseText = JsonSerializer.Serialize(new
                    {
                        SqlQuery = sqlQuery,
                        Summary = ExtractSummaryFromResponse(geminiResponse),
                        Results = resultData
                    })
                };

                //insert the request with response into database for logging and recent requests
                try
                {
                    // Create parameters for stored procedure
                    var appIdParam = OracleDbService.CreateInputParameter(
                        "p_app_id",
                        OracleDbType.Int32,
                        payload.AppId
                    );

                    var usernameParam = OracleDbService.CreateInputParameter(
                        "p_username",
                        OracleDbType.NVarchar2,
                        payload.Username ?? string.Empty
                    );

                    var subjectParam = OracleDbService.CreateInputParameter(
                        "p_subject",
                        OracleDbType.NVarchar2,
                        payload.Subject
                    );

                    var requestTextParam = OracleDbService.CreateInputParameter(
                        "p_request_text",
                        OracleDbType.NClob,
                        payload.RequestText
                    );

                    var responseFormatParam = OracleDbService.CreateInputParameter(
                        "p_response_format",
                        OracleDbType.NVarchar2,
                        DBNull.Value // Will be set when format selection is implemented in UI
                    );

                    var tokenParam = OracleDbService.CreateInputParameter(
                        "p_token",
                        OracleDbType.NVarchar2,
                        string.IsNullOrWhiteSpace(payload.Token) ? DBNull.Value : payload.Token
                    );

                    // Output parameters
                    var requestIdParam = OracleDbService.CreateOutputParameter(
                        "p_request_id",
                        OracleDbType.Int32
                    );

                    var statusParam = OracleDbService.CreateOutputParameter(
                        "p_status",
                        OracleDbType.Int32
                    );

                    var messageParam = OracleDbService.CreateOutputParameter(
                        "p_message",
                        OracleDbType.NVarchar2,
                        4000
                    );

                    // Execute stored procedure to insert request
                    var outParams = await _dbService.ExecuteStoredProcedureWithOutParamsAsync(
                        "P_IRT_REPORTHELPERTOOL.SP_InsertRequest",
                        appIdParam,
                        usernameParam,
                        subjectParam,
                        requestTextParam,
                        responseFormatParam,
                        tokenParam,
                        requestIdParam,
                        statusParam,
                        messageParam
                    );

                    // Extract output parameters
                    var dbStatus = Convert.ToInt32(outParams["p_status"]);
                    var dbMessage = outParams["p_message"]?.ToString() ?? "Unknown error";
                    var requestId = outParams["p_request_id"] != DBNull.Value
                        ? Convert.ToInt32(outParams["p_request_id"])
                        : 0;

                    if (dbStatus == 1)
                    {
                        // Update the request with the formatted response (JSON with SqlQuery, Summary, Results)
                        await UpdateRequestResponseAsync(requestId, response.ResponseText);
                    }
                }
                catch (Exception ex)
                {
                    // log and ignore errors here - we still want to return the response to the user
                    _logger.LogError(ex, "Error saving the request to database for AppId: {AppId}", payload.AppId);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating query for AppId: {AppId}", payload.AppId);

                var response = new RequestResponseModel
                {
                    Status = 0,
                    Message = "Failed to generate query",
                    ResponseText = string.Empty
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Builds a specialized prompt for Gemini to generate SQL queries
        /// </summary>
        private string BuildQueryGenerationPrompt(
            string subject,
            string requestText,
            string dbProvider,
            string dbStructureJson)
        {
            var prompt = $@"You are an expert SQL query generator with intelligent matching capabilities. Based on the user's request, generate an appropriate SQL query that matches their requirements using smart, flexible matching.

**User Request:**
{requestText}

**Database Information:**
Database Type: {dbProvider}
Database Structure (JSON):
{dbStructureJson}

**Instructions:**
1. Carefully analyze the user's request to understand what data they want to retrieve or manipulate.
2. Study the provided database structure JSON which contains all tables, columns, and data types.
3. Each table and column in the json of database structure has a ArabicTranslation below the Name to help you understand its meaning when the the user wrote the request in Arabic.
4. Generate a valid SQL query that is compatible with {dbProvider} database syntax and verify the generated SQL query is valid before sending the response.
5. The query should be optimized, efficient, and follow best practices for {dbProvider}.
6. Don't add comments within the SQL query.
6. ALWAYS apply the following intelligent matching rules to ensure flexible and accurate data retrieval.

**CRITICAL - INTELLIGENT MATCHING RULES:**

5. **AUTOMATIC Name Variation Detection**: When searching for ANY condition (person, place, department, section, designation, etc.), AUTOMATICALLY include ALL common variations WITHOUT the user having to specify them.
   - IMPORTANT: try to cover as many common variations as possible for each condition.
   - Examples of automatic variations to include:
     * ""Mostafa"" → MUST include: Mostafa, Mustafa, Moustafa, Mustafah, Mostafah
     * ""Abdalla"" → MUST include: Abdallah, Abdullah, Abd Allah, Abdalla, Abdulla
     * ""Mohamed"" → MUST include: Mohammed, Muhammad, Mohamad, Mohammad
     * ""Ahmed"" → MUST include: Ahmad, Ahmmed, Achmed
     * ""Ali"" → MUST include: Aly, Alee
     * ""Programming"" → MUST include: Programming, Programing, software Development
   - Use multiple OR conditions with LIKE patterns:
     WHERE UPPER(Name) LIKE '%MOSTAFA%' OR UPPER(Name) LIKE '%MUSTAFA%' OR UPPER(Name) LIKE '%MOUSTAFA%'
   - This applies to ALL name searches, not just when user explicitly asks for variations

6. **Fuzzy/Partial Text Matching**: When the user searches for text values, use LIKE patterns with wildcards (%) for flexible matching, NOT exact equality (=).
   - Example: If user asks for ""Programming Section"", search for columns LIKE '%Programming%' to match ""Programming & Applications"", ""Programming Section"", etc.
   - Always use UPPER() or LOWER() functions for case-insensitive searches.

7. **Semantic Understanding**: Interpret user intent, not just literal column names. As examples:
   - If user asks for ""age > 40"" but there's no AGE column, look for DOB, BirthDate, DateOfBirth, or similar date columns, then Calculate age using appropriate date functions for {dbProvider}:
     * Oracle: FLOOR((SYSDATE - DOB) / 365.25)
     * SQL Server: DATEDIFF(year, BirthDate, GETDATE())
     * MySQL: TIMESTAMPDIFF(YEAR, DateOfBirth, CURDATE())

8. **Flexible Column Matching**: Match column names intelligently, not just exactly.
   - ""employee code"" could be: EmpCode, EmployeeCode, EMP_CODE, EMPLOYEE_ID, etc.
   - ""name"" could be: Name, EmpName, EmployeeName, FullName, FIRST_NAME + LAST_NAME, etc.
   - Look for similar or related columns in the database structure.

9. **Case-Insensitive Searches**: ALWAYS use UPPER() or LOWER() functions for text comparisons to ensure case-insensitive matching.

10. **Smart Sorting**: If user mentions ""ascending"" or ""descending"", apply ORDER BY with ASC or DESC respectively.

11. Include appropriate JOINs if multiple tables are needed based on foreign key relationships in the structure.

**Response Format:**
Please provide your response in the following format:

## Summary
[Provide a clear, concise summary explaining what the user's request means and what data they want to retrieve. Write this in 2-3 sentences that a non-technical person can understand.]

## SQL Query
```sql
[Your generated SQL query here - write clean, formatted SQL code with intelligent matching logic and AUTOMATIC name variations]
```

## Explanation
[Brief explanation of what the query does, which tables it uses, and any important details about the intelligent matching logic used, including which name variations were automatically included]

**Important:**
- Do NOT include any introductory or concluding statements like ""I will help you"" or ""I hope this helps""
- Start directly with the Summary section
- Make sure the SQL syntax is compatible with {dbProvider}
- Use proper SQL formatting with appropriate indentation
- Only use tables and columns that exist in the provided database structure
- ALWAYS apply intelligent, flexible matching as described above - never use exact equality for text searches
- CRITICAL: ALWAYS automatically include name variations even if user doesn't ask for them - this is MANDATORY for all name searches";

            return prompt;
        }

        /// <summary>
        /// Extracts SQL query from Gemini response (from ```sql code block)
        /// </summary>
        private string ExtractSqlQueryFromResponse(string response)
        {
            try
            {
                // Look for SQL code block: ```sql ... ```
                var sqlMatch = System.Text.RegularExpressions.Regex.Match(
                    response,
                    @"```sql\s*([\s\S]*?)```",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (sqlMatch.Success && sqlMatch.Groups.Count > 1)
                {
                    var rawSql = sqlMatch.Groups[1].Value.Trim();
                    // Normalize the SQL to remove line breaks and extra whitespace
                    return NormalizeSqlQuery(rawSql);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting SQL query from response");
                return string.Empty;
            }
        }

        /// <summary>
        /// Normalizes SQL query by removing line breaks and extra whitespace
        /// This ensures the query executes properly without formatting issues
        /// </summary>
        private string NormalizeSqlQuery(string sqlQuery)
        {
            if (string.IsNullOrWhiteSpace(sqlQuery))
                return string.Empty;

            try
            {
                // Replace all line breaks (CRLF, LF, CR) with spaces
                var normalized = System.Text.RegularExpressions.Regex.Replace(
                    sqlQuery,
                    @"[\r\n]+",
                    " "
                );

                // Replace tabs with spaces
                normalized = normalized.Replace("\t", " ");

                // Replace multiple consecutive spaces with single space
                normalized = System.Text.RegularExpressions.Regex.Replace(
                    normalized,
                    @"\s+",
                    " "
                );

                // Trim leading and trailing whitespace
                normalized = normalized.Trim();

                // Remove trailing semicolon if present (not needed for ADO.NET commands)
                if (normalized.EndsWith(";"))
                {
                    normalized = normalized.Substring(0, normalized.Length - 1).Trim();
                }

                return normalized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error normalizing SQL query");
                return sqlQuery; // Return original if normalization fails
            }
        }

        /// <summary>
        /// Extracts summary section from Gemini response
        /// </summary>
        private string ExtractSummaryFromResponse(string response)
        {
            try
            {
                // Look for content between ## Summary and ## SQL Query
                var summaryMatch = System.Text.RegularExpressions.Regex.Match(
                    response,
                    @"##\s*Summary\s*([\s\S]*?)##\s*SQL Query",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (summaryMatch.Success && summaryMatch.Groups.Count > 1)
                {
                    return summaryMatch.Groups[1].Value.Trim();
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting summary from response");
                return string.Empty;
            }
        }

        /// <summary>
        /// Executes SQL query based on database provider
        /// </summary>
        private async Task<System.Data.DataTable> ExecuteSqlQuery(
            string sqlQuery,
            string dbProvider,
            string connectionString)
        {
            _logger.LogInformation("Executing SQL query for provider: {DbProvider}", dbProvider);

            switch (dbProvider.ToUpperInvariant())
            {
                case "ORACLE":
                    return await ExecuteOracleQuery(sqlQuery, connectionString);

                case "SQLSERVER":
                    return await ExecuteSqlServerQuery(sqlQuery, connectionString);

                case "MYSQL":
                    return await ExecuteMySqlQuery(sqlQuery, connectionString);

                default:
                    throw new NotSupportedException($"Database provider '{dbProvider}' is not supported");
            }
        }

        /// <summary>
        /// Executes Oracle SQL query
        /// </summary>
        private async Task<System.Data.DataTable> ExecuteOracleQuery(string sqlQuery, string connectionString)
        {
            using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync();

            using var command = new OracleCommand(sqlQuery, connection);
            command.CommandTimeout = 30; // 30 seconds timeout

            using var adapter = new OracleDataAdapter(command);
            var dataTable = new System.Data.DataTable();
            adapter.Fill(dataTable);

            return dataTable;
        }

        /// <summary>
        /// Executes SQL Server query
        /// </summary>
        private async Task<System.Data.DataTable> ExecuteSqlServerQuery(string sqlQuery, string connectionString)
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new Microsoft.Data.SqlClient.SqlCommand(sqlQuery, connection);
            command.CommandTimeout = 30;

            using var adapter = new Microsoft.Data.SqlClient.SqlDataAdapter(command);
            var dataTable = new System.Data.DataTable();
            adapter.Fill(dataTable);

            return dataTable;
        }

        /// <summary>
        /// Executes MySQL query
        /// </summary>
        private async Task<System.Data.DataTable> ExecuteMySqlQuery(string sqlQuery, string connectionString)
        {
            using var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySql.Data.MySqlClient.MySqlCommand(sqlQuery, connection);
            command.CommandTimeout = 30;

            using var adapter = new MySql.Data.MySqlClient.MySqlDataAdapter(command);
            var dataTable = new System.Data.DataTable();
            adapter.Fill(dataTable);

            return dataTable;
        }

        /// <summary>
        /// Converts DataTable to JSON-friendly list of dictionaries
        /// </summary>
        private List<Dictionary<string, object?>> ConvertDataTableToJson(System.Data.DataTable dataTable)
        {
            var results = new List<Dictionary<string, object?>>();

            foreach (System.Data.DataRow row in dataTable.Rows)
            {
                var rowData = new Dictionary<string, object?>();

                foreach (System.Data.DataColumn column in dataTable.Columns)
                {
                    var value = row[column];

                    // Convert DBNull to null
                    if (value == DBNull.Value)
                    {
                        rowData[column.ColumnName] = null;
                    }
                    // Format dates as dd/MM/yyyy
                    else if (value is DateTime dateValue)
                    {
                        rowData[column.ColumnName] = dateValue.ToString("dd/MM/yyyy");
                    }
                    // Convert Oracle-specific types
                    else if (value.GetType().Name == "OracleDecimal")
                    {
                        var oracleDecimal = (dynamic)value;
                        rowData[column.ColumnName] = oracleDecimal.IsNull ? null : (decimal?)oracleDecimal.Value;
                    }
                    else if (value.GetType().Name == "OracleString")
                    {
                        var oracleString = (dynamic)value;
                        rowData[column.ColumnName] = oracleString.IsNull ? null : (string?)oracleString.Value;
                    }
                    else if (value.GetType().Name == "OracleClob")
                    {
                        var oracleClob = (dynamic)value;
                        rowData[column.ColumnName] = oracleClob.IsNull ? null : (string?)oracleClob.Value;
                    }
                    else if (value.GetType().Name == "OracleDate")
                    {
                        var oracleDate = (dynamic)value;
                        if (!oracleDate.IsNull)
                        {
                            DateTime dt = oracleDate.Value;
                            rowData[column.ColumnName] = dt.ToString("dd/MM/yyyy");
                        }
                        else
                        {
                            rowData[column.ColumnName] = null;
                        }
                    }
                    else
                    {
                        rowData[column.ColumnName] = value;
                    }
                }

                results.Add(rowData);
            }

            return results;
        }

        /// <summary>
        /// DELETE: api/request/{id}
        /// Deletes a request by ID
        /// Calls: P_IRT_REPORTHELPERTOOL.SP_DeleteRequest
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    Status = 0,
                    Message = "Invalid request ID"
                });
            }

            try
            {
                // Create parameters for stored procedure
                var requestIdParam = OracleDbService.CreateInputParameter(
                    "p_request_id",
                    OracleDbType.Int32,
                    id
                );

                var statusParam = OracleDbService.CreateOutputParameter(
                    "p_status",
                    OracleDbType.Int32
                );

                var messageParam = OracleDbService.CreateOutputParameter(
                    "p_message",
                    OracleDbType.NVarchar2,
                    4000
                );

                // Execute stored procedure to delete request
                var outParams = await _dbService.ExecuteStoredProcedureWithOutParamsAsync(
                    "P_IRT_REPORTHELPERTOOL.SP_DeleteRequest",
                    requestIdParam,
                    statusParam,
                    messageParam
                );

                // Extract output parameters
                var dbStatus = Convert.ToInt32(outParams["p_status"]);
                var dbMessage = outParams["p_message"]?.ToString() ?? "Unknown error";

                if (dbStatus == 1)
                {
                    return Ok(new
                    {
                        Status = 1,
                        Message = dbMessage
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Status = 0,
                        Message = dbMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting request {RequestId}", id);

                return StatusCode(500, new
                {
                    Status = 0,
                    Message = "Failed to delete request"
                });
            }
        }
    }
}
