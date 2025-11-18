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
                // Determine response format (email or report)
                string responseFormat = DetermineResponseFormat(payload.ResponseFormat, payload.RequestText, payload.Subject);
                _logger.LogInformation("Determined response format: {Format}", responseFormat);

                // Call Gemini API to generate response
                _logger.LogInformation("Calling Gemini API for request: {Subject}", payload.Subject);
                string geminiResponse;

                try
                {
                    switch (responseFormat)
                    {
                        case "email":
                            geminiResponse = await _geminiService.GenerateEmailAsync(
                                "IntellegentReportMaker",
                                payload.Subject,
                                payload.RequestText
                            );
                            break;
                        case "table":
                            geminiResponse = await _geminiService.GenerateTableAsync(
                                "IntellegentReportMaker",
                                payload.Subject,
                                payload.RequestText
                            );
                            break;
                        case "chart":
                            geminiResponse = await _geminiService.GenerateChartAsync(
                                "IntellegentReportMaker",
                                payload.Subject,
                                payload.RequestText
                            );
                            break;
                        case "study":
                            geminiResponse = await _geminiService.GenerateStudyAsync(
                                "IntellegentReportMaker",
                                payload.Subject,
                                payload.RequestText
                            );
                            break;
                        default: // "report"
                            geminiResponse = await _geminiService.GenerateReportAsync(
                                "IntellegentReportMaker",
                                payload.Subject,
                                payload.RequestText
                            );
                            break;
                    }
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
                    responseFormat
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

                // Retry logic for SQL generation and execution (max 3 attempts)
                const int maxAttempts = 3;
                int attempt = 0;
                string sqlQuery = string.Empty;
                string lastError = string.Empty;
                System.Data.DataTable? queryResults = null;
                bool querySuccessful = false;

                while (attempt < maxAttempts && !querySuccessful)
                {
                    attempt++;
                    _logger.LogInformation("SQL generation and execution attempt {Attempt} of {MaxAttempts} for AppId: {AppId}",
                        attempt, maxAttempts, payload.AppId);

                    try
                    {
                        // Build the specialized prompt for Gemini
                        string geminiPrompt;

                        if (attempt == 1)
                        {
                            // First attempt: use standard prompt
                            geminiPrompt = BuildQueryGenerationPrompt(
                                payload.Subject,
                                payload.RequestText,
                                appDbDetails.DbProvider,
                                appDbDetails.DbStructureJson
                            );
                        }
                        else
                        {
                            // Retry attempts: include previous error information
                            geminiPrompt = BuildQueryGenerationPromptWithError(
                                payload.Subject,
                                payload.RequestText,
                                appDbDetails.DbProvider,
                                appDbDetails.DbStructureJson,
                                sqlQuery,
                                lastError,
                                attempt
                            );
                            _logger.LogWarning("Retrying SQL generation (attempt {Attempt}) with error feedback: {Error}",
                                attempt, lastError);
                        }

                        // Call Gemini API to generate query
                        _logger.LogInformation("Calling Gemini API to generate SQL query (attempt {Attempt}) for AppId: {AppId}",
                            attempt, payload.AppId);

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
                            _logger.LogError(geminiEx, "Gemini API call failed on attempt {Attempt}", attempt);
                            lastError = $"Gemini API error: {geminiEx.Message}";

                            if (attempt == maxAttempts)
                            {
                                return Ok(new RequestResponseModel
                                {
                                    Status = 0,
                                    Message = "Unable to generate SQL query after multiple attempts. Please try again or rephrase your request.",
                                    ResponseText = $"The AI service encountered an error after {maxAttempts} attempts.\n\nTechnical details logged to console."
                                });
                            }
                            continue;
                        }

                        // Extract SQL query from Gemini response
                        sqlQuery = ExtractSqlQueryFromResponse(geminiResponse);

                        if (string.IsNullOrWhiteSpace(sqlQuery))
                        {
                            _logger.LogWarning("Could not extract SQL query from Gemini response on attempt {Attempt}", attempt);
                            lastError = "Failed to extract SQL query from AI response";

                            if (attempt == maxAttempts)
                            {
                                return Ok(new RequestResponseModel
                                {
                                    Status = 0,
                                    Message = "Unable to generate a valid SQL query. Please try rephrasing your request.",
                                    ResponseText = $"Failed to extract SQL query after {maxAttempts} attempts.\n\nTechnical details logged to console."
                                });
                            }
                            continue;
                        }

                        // Log the generated SQL query
                        _logger.LogInformation("Generated SQL Query (attempt {Attempt}): {SqlQuery}", attempt, sqlQuery);

                        // Execute the SQL query based on database provider
                        try
                        {
                            queryResults = await ExecuteSqlQuery(
                                sqlQuery,
                                appDbDetails.DbProvider,
                                appDbDetails.DbConnection
                            );

                            // If we get here, query was successful
                            querySuccessful = true;
                            _logger.LogInformation("SQL query executed successfully on attempt {Attempt}", attempt);
                        }
                        catch (Exception queryEx)
                        {
                            lastError = queryEx.Message;
                            _logger.LogError(queryEx, "SQL execution error on attempt {Attempt}: {Error}. SQL: {SqlQuery}",
                                attempt, queryEx.Message, sqlQuery);

                            // If this was the last attempt, return user-friendly error
                            if (attempt == maxAttempts)
                            {
                                // Log technical details to console
                                Console.WriteLine("=== SQL EXECUTION FAILED AFTER ALL RETRIES ===");
                                Console.WriteLine($"Attempt: {attempt}/{maxAttempts}");
                                Console.WriteLine($"Database Provider: {appDbDetails.DbProvider}");
                                Console.WriteLine($"SQL Query:\n{sqlQuery}");
                                Console.WriteLine($"Error: {queryEx.Message}");
                                Console.WriteLine($"Stack Trace:\n{queryEx.StackTrace}");
                                Console.WriteLine("============================================");

                                return Ok(new RequestResponseModel
                                {
                                    Status = 0,
                                    Message = "We couldn't execute the database query successfully. The technical team has been notified.",
                                    ResponseText = $"After {maxAttempts} attempts, the query could not be executed.\n\n" +
                                                  $"Error: {GetUserFriendlyErrorMessage(queryEx.Message)}\n\n" +
                                                  $"Please try rephrasing your request or contact support if the issue persists.\n\n" +
                                                  $"Technical details have been logged for investigation."
                                });
                            }
                            // Otherwise, continue to next attempt
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error during SQL generation/execution on attempt {Attempt}", attempt);
                        lastError = ex.Message;

                        if (attempt == maxAttempts)
                        {
                            return Ok(new RequestResponseModel
                            {
                                Status = 0,
                                Message = "An unexpected error occurred. Please try again.",
                                ResponseText = $"Technical details logged to console."
                            });
                        }
                    }
                }

                // Verify we have results before continuing
                if (!querySuccessful || queryResults == null)
                {
                    _logger.LogError("Query execution loop completed but no results obtained for AppId: {AppId}", payload.AppId);
                    return Ok(new RequestResponseModel
                    {
                        Status = 0,
                        Message = "Unable to retrieve data. Please try again.",
                        ResponseText = string.Empty
                    });
                }

                // Convert DataTable to JSON-friendly format
                var resultData = ConvertDataTableToJson(queryResults);

                // Convert the result data to JSON string for Gemini
                var resultDataJson = JsonSerializer.Serialize(resultData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Determine response format (email or report)
                string responseFormat = DetermineResponseFormat(payload.ResponseFormat, payload.RequestText, payload.Subject);
                _logger.LogInformation("Determined response format: {Format}", responseFormat);

                // Call Gemini to generate a report or email based on the data
                _logger.LogInformation("Calling Gemini API to generate {Format} with data for AppId: {AppId}", responseFormat, payload.AppId);
                string reportResponse;

                try
                {
                    switch (responseFormat)
                    {
                        case "email":
                            reportResponse = await _geminiService.GenerateEmailWithDataAsync(
                                "IntellegentReportMaker",
                                payload.Subject,
                                payload.RequestText,
                                resultDataJson
                            );
                            break;
                        case "table":
                            reportResponse = await _geminiService.GenerateTableWithDataAsync(
                                "IntellegentReportMaker",
                                payload.Subject,
                                payload.RequestText,
                                resultDataJson
                            );
                            break;
                        case "chart":
                            reportResponse = await _geminiService.GenerateChartWithDataAsync(
                                "IntellegentReportMaker",
                                payload.Subject,
                                payload.RequestText,
                                resultDataJson
                            );
                            break;
                        case "study":
                            reportResponse = await _geminiService.GenerateStudyWithDataAsync(
                                "IntellegentReportMaker",
                                payload.Subject,
                                payload.RequestText,
                                resultDataJson
                            );
                            break;
                        default: // "report"
                            reportResponse = await _geminiService.GenerateReportWithDataAsync(
                                "IntellegentReportMaker",
                                payload.Subject,
                                payload.RequestText,
                                resultDataJson
                            );
                            break;
                    }
                }
                catch (Exception geminiEx)
                {
                    _logger.LogError(geminiEx, "Gemini API call failed for {Format} generation with data", responseFormat);
                    return Ok(new RequestResponseModel
                    {
                        Status = 0,
                        Message = $"Error generating {responseFormat} with data",
                        ResponseText = geminiEx.Message
                    });
                }

                var response = new RequestResponseModel
                {
                    Status = 1,
                    Message = responseFormat switch
                    {
                        "email" => "Email generated successfully",
                        "table" => "Table generated successfully",
                        "chart" => "Chart generated successfully",
                        "study" => "Study generated successfully",
                        _ => "Report generated successfully"
                    },
                    ResponseText = reportResponse
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
                        responseFormat
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
4. Generate a valid SQL query that is compatible with {dbProvider} database syntax.
5. The query should be optimized, efficient, and follow best practices for {dbProvider}.
6. Don't add comments within the SQL query.
7. ALWAYS apply the following intelligent matching rules to ensure flexible and accurate data retrieval.

**CRITICAL - SQL VALIDATION REQUIREMENTS:**
Before providing your response, you MUST validate the SQL query thoroughly. The query MUST be 100% syntactically correct and executable. Validate the following:

1. **GROUP BY Validation** (CRITICAL for {dbProvider}):
   - Every non-aggregated column in the SELECT clause MUST appear in the GROUP BY clause
   - Example WRONG: SELECT Name, COUNT(*) FROM Employees (missing: GROUP BY Name)
   - Example CORRECT: SELECT Name, COUNT(*) FROM Employees GROUP BY Name
   - For Oracle: ORA-00979 error occurs when GROUP BY is missing required columns

2. **Column Existence Validation**:
   - ONLY use columns that exist in the provided database structure JSON
   - Double-check spelling and exact column names
   - Verify table names match the database structure exactly

3. **JOIN Syntax Validation**:
   - Ensure all JOINs have proper ON clauses with valid foreign key relationships
   - Verify joined columns exist in both tables
   - Use proper JOIN syntax for {dbProvider}

4. **Aggregate Function Validation**:
   - When using COUNT, SUM, AVG, MAX, MIN, ensure proper GROUP BY
   - Don't mix aggregated and non-aggregated columns without GROUP BY

5. **Date Function Validation**:
   - Use correct date functions for {dbProvider}:
     * Oracle: SYSDATE, TRUNC, TO_DATE, ADD_MONTHS
     * SQL Server: GETDATE(), DATEADD, DATEDIFF
     * MySQL: NOW(), DATE_ADD, DATE_SUB

6. **WHERE Clause Validation**:
   - Ensure column names in WHERE exist in the tables
   - Use proper comparison operators for data types
   - Validate that LIKE patterns are syntactically correct

7. **Syntax Compatibility**:
   - Ensure TOP/LIMIT/ROWNUM syntax matches {dbProvider}
   - Use correct string concatenation (|| for Oracle, + for SQL Server, CONCAT for MySQL)
   - Verify quote usage (single quotes for strings)

**DOUBLE-CHECK BEFORE RESPONDING:**
- Re-read your generated SQL query line by line
- Verify every column in SELECT that is not wrapped in an aggregate function appears in GROUP BY
- Confirm all table and column names exist in the database structure JSON
- Test the logic mentally - would this query execute without errors?
- If you're not 100% certain the query is valid, revise it until you are certain

**CRITICAL - INTELLIGENT MATCHING RULES:**

5. **AUTOMATIC Name Variation Detection**: When searching for ANY person's name, AUTOMATICALLY include ALL common spelling variations WITHOUT the user having to specify them.
   - IMPORTANT: try to cover as many common variations as possible for each name.
   - Examples of automatic variations to include:
     * ""Mostafa"" → MUST include: Mostafa, Mustafa, Moustafa, Mustafah, Mostafah
     * ""Abdalla"" → MUST include: Abdallah, Abdullah, Abd Allah, Abdalla, Abdulla
     * ""Mohamed"" → MUST include: Mohammed, Muhammad, Mohamad, Mohammad
     * ""Ahmed"" → MUST include: Ahmad, Ahmmed, Achmed
     * ""Ali"" → MUST include: Aly, Alee
   - Use multiple OR conditions with LIKE patterns:
     WHERE UPPER(Name) LIKE '%MOSTAFA%' OR UPPER(Name) LIKE '%MUSTAFA%' OR UPPER(Name) LIKE '%MOUSTAFA%'
   - This applies to ALL name searches, not just when user explicitly asks for variations

6. **SEMANTIC/CONTEXTUAL Matching for Departments, Sections, and Designations** (CRITICAL):
   - When the user asks for a department, section, or designation, you MUST understand the SEMANTIC MEANING and CONTEXT, NOT just literal text matching.
   - DO NOT use simple LIKE '%text%' patterns that will match unrelated items with similar words.
   - Instead, analyze the MEANING of what the user is asking for and match items with SIMILAR CONTEXTUAL MEANING.

   **STEP 1: NORMALIZE SPECIAL CHARACTERS IN USER INPUT**
   When the user provides a section/department/designation name, automatically handle character variations:
   - ""&"" (ampersand) ↔ ""and"" (spelled out)
   - ""-"" (hyphen/dash) ↔ "" "" (space) ↔ """" (no separator)
   - ""/"" (slash) ↔ ""and"" ↔ "" "" (space)

   Examples of equivalent searches:
   - ""Programming & Applications"" = ""Programming and Applications"" = ""Programming Applications""
   - ""Quality-Control"" = ""Quality Control"" = ""QualityControl""
   - ""HR/Admin"" = ""HR and Admin"" = ""HR Admin""

   **STEP 2: CHECK DATABASE FOR EXACT OR CLOSE MATCHES FIRST**
   - FIRST, look at the actual section/department/designation values in the database structure JSON
   - If you find an EXACT match (accounting for character variations from Step 1), use that specific value
   - If you find a CLOSE match (same key words but slightly different), include both the exact database value AND semantic variations

   Example: If user asks for ""Programming Section"" and database has ""Programming & Applications Section"":
   - ✅ CORRECT: Match ""Programming & Applications Section"" specifically
   - ❌ WRONG: Return 0 rows because of exact match on ""Programming Section""
   - ❌ WRONG: Return all employees by using too broad a pattern

   **STEP 3: ADD SEMANTIC VARIATIONS ONLY WITHIN THE SAME DOMAIN**
   After finding close matches in the database, add semantically related variations ONLY if they make sense:

   **EXAMPLES OF CORRECT SEMANTIC MATCHING:**

   Example 1: User asks for ""Programming and applications Section""
   ✅ SHOULD MATCH (semantically related to software/programming):
     * ""Programming & Applications"" (character variation - MUST MATCH)
     * ""Programming and Applications"" (character variation - MUST MATCH)
     * ""Programming Section"" (partial match of same section)
     * ""Software Development Section"" (same semantic domain)
     * ""Software Engineering Section"" (same semantic domain)
     * ""Application Development"" (same semantic domain)
     * ""IT Development"" (same semantic domain)

   ❌ SHOULD NOT MATCH (different context despite containing similar words):
     * ""Safety Programs Section"" (safety context, not programming context)
     * ""Quality Programs"" (quality context, not programming context)
     * ""Training Programs"" (training context, not programming context)
     * ""Application Forms Department"" (forms/paperwork context, not software context)

   Example 2: User asks for ""Quality Section""
   ✅ SHOULD MATCH:
     * ""Quality Control""
     * ""Quality Assurance""
     * ""QC Department""
     * ""Quality Management""

   ❌ SHOULD NOT MATCH:
     * Unrelated sections with word ""quality""

   **IMPLEMENTATION APPROACH:**

   Step-by-step process:
   1. Parse the user's search term (e.g., ""Programming Section"")
   2. Generate character variations: ""Programming & Section"", ""Programming and Section"", ""Programming-Section""
   3. Look at database structure to find sections/departments containing these key words
   4. If you find actual database values, build your WHERE clause to match those FIRST
   5. Then add semantically related terms in the SAME domain

   Example SQL for ""Programming Section"":
   ```sql
   WHERE
     -- Exact match with character variations (if these exist in DB)
     UPPER(SectionName) IN (
       'PROGRAMMING SECTION',
       'PROGRAMMING & APPLICATIONS',
       'PROGRAMMING AND APPLICATIONS',
       'PROGRAMMING-APPLICATIONS'
     )
     -- Close partial matches containing key words
     OR UPPER(SectionName) LIKE '%PROGRAMMING%APPLICATION%'
     OR UPPER(SectionName) LIKE '%APPLICATION%PROGRAMMING%'
     -- Semantic variations (same domain only)
     OR UPPER(SectionName) LIKE '%SOFTWARE DEVELOPMENT%'
     OR UPPER(SectionName) LIKE '%SOFTWARE ENGINEERING%'
   ```

   **CRITICAL RULES:**
   - NEVER use overly broad patterns like `LIKE '%PROGRAM%'` (will match ""Training Programs"", etc.)
   - ALWAYS check database structure first to see what sections actually exist
   - ALWAYS handle ""&"" ↔ ""and"" character variations
   - When user says ""Programming Section"", it should match ""Programming & Applications Section"" if that exists in the database
   - Build WHERE clauses that are specific enough to avoid false matches but flexible enough to handle variations

7. **Fuzzy/Partial Text Matching for Free Text Fields**: When the user searches for descriptive text or comments, use LIKE patterns with wildcards (%) for flexible matching.
   - Always use UPPER() or LOWER() functions for case-insensitive searches.
   - This applies to free-text fields like descriptions, comments, notes, etc.

8. **Semantic Understanding for Calculated Fields**: Interpret user intent, not just literal column names. As examples:
   - If user asks for ""age > 40"" but there's no AGE column, look for DOB, BirthDate, DateOfBirth, or similar date columns, then Calculate age using appropriate date functions for {dbProvider}:
     * Oracle: FLOOR((SYSDATE - DOB) / 365.25)
     * SQL Server: DATEDIFF(year, BirthDate, GETDATE())
     * MySQL: TIMESTAMPDIFF(YEAR, DateOfBirth, CURDATE())

9. **Flexible Column Matching**: Match column names intelligently, not just exactly.
   - ""employee code"" could be: EmpCode, EmployeeCode, EMP_CODE, EMPLOYEE_ID, etc.
   - ""name"" could be: Name, EmpName, EmployeeName, FullName, FIRST_NAME + LAST_NAME, etc.
   - Look for similar or related columns in the database structure.

10. **Case-Insensitive Searches**: ALWAYS use UPPER() or LOWER() functions for text comparisons to ensure case-insensitive matching.

11. **Smart Sorting**: If user mentions ""ascending"" or ""descending"", apply ORDER BY with ASC or DESC respectively.

12. Include appropriate JOINs if multiple tables are needed based on foreign key relationships in the structure.

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
- CRITICAL: ALWAYS automatically include name variations even if user doesn't ask for them - this is MANDATORY for all name searches
- CRITICAL: For departments/sections/designations, you MUST use SEMANTIC/CONTEXTUAL matching - understand the MEANING and match only items in the same semantic domain. DO NOT use broad LIKE patterns that will match unrelated items with similar words";

            return prompt;
        }

        /// <summary>
        /// Builds a specialized prompt for Gemini to regenerate SQL queries with error feedback
        /// </summary>
        private string BuildQueryGenerationPromptWithError(
            string subject,
            string requestText,
            string dbProvider,
            string dbStructureJson,
            string previousSqlQuery,
            string errorMessage,
            int attemptNumber)
        {
            var prompt = $@"You are an expert SQL query generator. Your previous SQL query generated an error. You need to analyze the error and generate a corrected SQL query.

**User Request:**
{requestText}

**Database Information:**
Database Type: {dbProvider}
Database Structure (JSON):
{dbStructureJson}

**PREVIOUS ATTEMPT #{attemptNumber - 1} - FAILED:**

**Previous SQL Query:**
```sql
{previousSqlQuery}
```

**Error Received:**
{errorMessage}

**CRITICAL INSTRUCTIONS FOR RETRY:**

1. **Analyze the Error Carefully:**
   - Read the error message thoroughly to understand what went wrong
   - Common errors to look for:
     * ORA-00979 or ""not a GROUP BY expression"" → Add missing columns to GROUP BY
     * ""Invalid column name"" → Check column spelling and existence in database structure
     * ""Invalid object name"" → Check table name spelling and existence
     * ""Syntax error"" → Review SQL syntax for {dbProvider}

2. **Fix the Specific Issue:**
   - Do NOT just regenerate the same query
   - Address the root cause identified in the error message
   - If it's a GROUP BY error, ensure EVERY non-aggregated column in SELECT is in GROUP BY
   - If it's a column name error, verify the exact column name from the database structure JSON
   - If it's a syntax error, use correct {dbProvider} syntax

3. **Validate Your Corrected Query:**
   - Double-check that you've fixed the specific error mentioned
   - Verify all column names exist in the database structure
   - Ensure GROUP BY includes all non-aggregated SELECT columns
   - Confirm JOIN conditions are valid
   - Make sure the syntax is compatible with {dbProvider}

4. **Response Format:**
   Provide your corrected query in this format:

## Summary
[Brief explanation of what was wrong and how you fixed it]

## SQL Query
```sql
[Your CORRECTED SQL query here - make sure the error is fixed]
```

## Explanation
[Explain what you changed to fix the error]

**IMPORTANT:**
- This is attempt #{attemptNumber} of 3 maximum attempts
- You MUST fix the error from the previous attempt
- Do NOT repeat the same mistake
- Start directly with the Summary section
- The corrected query MUST be syntactically valid for {dbProvider}";

            return prompt;
        }

        /// <summary>
        /// Converts technical database error messages to user-friendly messages
        /// </summary>
        private string GetUserFriendlyErrorMessage(string technicalError)
        {
            if (string.IsNullOrWhiteSpace(technicalError))
                return "An unknown database error occurred.";

            // Convert common Oracle errors
            if (technicalError.Contains("ORA-00979"))
                return "There was an issue with data grouping in the query.";

            if (technicalError.Contains("ORA-00942"))
                return "The query referenced a table or view that doesn't exist.";

            if (technicalError.Contains("ORA-00904"))
                return "The query referenced a column that doesn't exist.";

            if (technicalError.Contains("ORA-01722"))
                return "There was a data type mismatch in the query.";

            if (technicalError.Contains("ORA-00933") || technicalError.Contains("ORA-00936"))
                return "There was a syntax error in the generated query.";

            // Convert common SQL Server errors
            if (technicalError.Contains("Invalid column name"))
                return "The query referenced a column that doesn't exist in the database.";

            if (technicalError.Contains("Invalid object name"))
                return "The query referenced a table that doesn't exist in the database.";

            if (technicalError.Contains("Incorrect syntax near"))
                return "There was a syntax error in the generated query.";

            if (technicalError.Contains("Column") && technicalError.Contains("is invalid in the select list because it is not contained in either an aggregate function or the GROUP BY clause"))
                return "There was an issue with data grouping in the query.";

            // Convert common MySQL errors
            if (technicalError.Contains("Unknown column"))
                return "The query referenced a column that doesn't exist in the database.";

            if (technicalError.Contains("Table") && technicalError.Contains("doesn't exist"))
                return "The query referenced a table that doesn't exist in the database.";

            if (technicalError.Contains("You have an error in your SQL syntax"))
                return "There was a syntax error in the generated query.";

            // Generic timeout or connection errors
            if (technicalError.Contains("timeout") || technicalError.Contains("Timeout"))
                return "The query took too long to execute. Try simplifying your request.";

            if (technicalError.Contains("connection") || technicalError.Contains("Connection"))
                return "There was a problem connecting to the database.";

            // Default message for unknown errors
            return "A database error occurred. Our team has been notified.";
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
                    Message = $"Failed to delete request: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Determines the response format (table, chart, study, email, or report) based on explicit format or intelligent detection
        /// </summary>
        /// <param name="explicitFormat">Explicitly specified format from the request payload</param>
        /// <param name="requestText">The request text to analyze for format detection</param>
        /// <param name="subject">The subject to analyze for format detection</param>
        /// <returns>The determined format: "table", "chart", "study", "email", or "report"</returns>
        private string DetermineResponseFormat(string? explicitFormat, string requestText, string subject)
        {
            // If format is explicitly specified, use it
            if (!string.IsNullOrWhiteSpace(explicitFormat))
            {
                var format = explicitFormat.ToLower();
                if (format == "table" || format == "chart" || format == "study" || format == "email")
                {
                    return format;
                }
                return "report"; // Default for unknown explicit formats
            }

            // Intelligent detection: check for format-related keywords in request text and subject
            var combinedText = $"{subject} {requestText}".ToLower();

            // Table indicators (highest priority after explicit format)
            var tableKeywords = new[]
            {
                "only table",
                "just table",
                "table only",
                "only a table",
                "just a table",
                "show me a table",
                "show me table",
                "display table",
                "display a table",
                "create table",
                "create a table",
                "generate table",
                "generate a table",
                "give me table",
                "give me a table",
                "i want table",
                "i want a table",
                "need table",
                "need a table"
            };

            // Chart indicators
            var chartKeywords = new[]
            {
                "only chart",
                "just chart",
                "chart only",
                "only a chart",
                "just a chart",
                "only pie chart",
                "only bar chart",
                "only line chart",
                "only area chart",
                "only scatter chart",
                "just pie chart",
                "just bar chart",
                "just line chart",
                "pie chart only",
                "bar chart only",
                "line chart only",
                "show me chart",
                "show me a chart",
                "display chart",
                "display a chart",
                "create chart",
                "create a chart",
                "generate chart",
                "generate a chart",
                "visualize with",
                "visualize using",
                "i want chart",
                "i want a chart",
                "need chart",
                "need a chart"
            };

            // Study indicators
            var studyKeywords = new[]
            {
                "study",
                "research study",
                "case study",
                "analytical study",
                "detailed study",
                "comprehensive study",
                "create study",
                "create a study",
                "generate study",
                "generate a study",
                "write study",
                "write a study",
                "prepare study",
                "prepare a study",
                "conduct study",
                "conduct a study"
            };

            // Email indicators
            var emailKeywords = new[]
            {
                "email",
                "e-mail",
                "compose",
                "write an email",
                "draft an email",
                "send an email",
                "reply to",
                "respond to",
                "correspondence",
                "message to",
                "letter to",
                "professional email",
                "polite email",
                "formal email",
                "business email"
            };

            // Check for table indicators first
            foreach (var keyword in tableKeywords)
            {
                if (combinedText.Contains(keyword))
                {
                    return "table";
                }
            }

            // Check for chart indicators
            foreach (var keyword in chartKeywords)
            {
                if (combinedText.Contains(keyword))
                {
                    return "chart";
                }
            }

            // Check for study indicators
            foreach (var keyword in studyKeywords)
            {
                if (combinedText.Contains(keyword))
                {
                    return "study";
                }
            }

            // Check for email indicators
            foreach (var keyword in emailKeywords)
            {
                if (combinedText.Contains(keyword))
                {
                    return "email";
                }
            }

            // Default to report if no specific indicators found
            return "report";
        }
    }
}
