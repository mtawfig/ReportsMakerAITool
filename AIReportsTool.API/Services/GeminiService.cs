using System.IO.Pipelines;
using System.Text;
using System.Text.Json;

namespace AIReportsTool.API.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _baseUrl;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = new HttpClient
            {
                // Increase timeout to 5 minutes for long-running AI requests
                Timeout = TimeSpan.FromMinutes(5)
            };
            _apiKey = configuration["GeminiApi:ApiKey"]
                ?? throw new ArgumentNullException("Gemini API key is not configured");

            if (_apiKey == "YOUR_GEMINI_API_KEY_HERE")
            {
                throw new InvalidOperationException("Please replace YOUR_GEMINI_API_KEY_HERE with your actual Gemini API key in appsettings.json");
            }

            _model = configuration["GeminiApi:Model"] ?? "gemini-1.5-flash-latest";
            _baseUrl = configuration["GeminiApi:BaseUrl"]
                ?? "https://generativelanguage.googleapis.com/v1";
            _logger = logger;
        }

        /// <summary>
        /// Generates content using Gemini API
        /// </summary>
        /// <param name="prompt">The input prompt/question</param>
        /// <returns>Generated text response</returns>
        public async Task<string> GenerateContentAsync(string prompt)
        {
            try
            {
                _logger.LogInformation("Calling Gemini API with prompt length: {Length}", prompt.Length);

                // Construct the API endpoint
                var endpoint = $"{_baseUrl}/models/{_model}:generateContent?key={_apiKey}";

                // Prepare the request payload
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Retry configuration
                const int maxRetries = 6; // total attempts (1 + 5 retries)
                TimeSpan baseDelay = TimeSpan.FromMilliseconds(400);
                string responseContent = string.Empty;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        _logger.LogInformation("Gemini API attempt {Attempt}/{Max}", attempt, maxRetries);

                        // Call Gemini API
                        var response = await _httpClient.PostAsync(endpoint, httpContent);
                        responseContent = await response.Content.ReadAsStringAsync();

                        // Check for retry-able errors
                        if (!response.IsSuccessStatusCode)
                        {
                            var statusCode = (int)response.StatusCode;

                            // Check if we should retry (429 rate limit, 503 overload, 504 timeout, 500 server error)
                            if ((statusCode == 429 || statusCode == 503 || statusCode == 504 || statusCode == 500)
                                && attempt < maxRetries)
                            {
                                // Calculate delay with exponential backoff
                                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));

                                _logger.LogWarning(
                                    "Gemini API returned {StatusCode}. Retrying in {Delay}ms... (attempt {Attempt}/{Max})",
                                    statusCode, delay.TotalMilliseconds, attempt, maxRetries);

                                await Task.Delay(delay);
                                continue; // Retry
                            }

                            // Non-retryable error or max retries reached
                            _logger.LogError("Gemini API error: {StatusCode} - {Content}. Endpoint: {Endpoint}",
                                response.StatusCode, responseContent, endpoint);

                            // Parse and throw user-friendly error message
                            var friendlyError = ParseGeminiError(responseContent, (int)response.StatusCode);
                            throw new Exception(friendlyError);
                        }

                        // Success - parse the response
                        using var jsonDoc = JsonDocument.Parse(responseContent);
                        var root = jsonDoc.RootElement;

                        // Extract the generated text
                        if (root.TryGetProperty("candidates", out var candidates) &&
                            candidates.GetArrayLength() > 0)
                        {
                            var firstCandidate = candidates[0];
                            if (firstCandidate.TryGetProperty("content", out var content) &&
                                content.TryGetProperty("parts", out var parts) &&
                                parts.GetArrayLength() > 0)
                            {
                                var firstPart = parts[0];
                                if (firstPart.TryGetProperty("text", out var textElement))
                                {
                                    var generatedText = textElement.GetString() ?? string.Empty;
                                    _logger.LogInformation("Gemini API returned response length: {Length} (attempt {Attempt})",
                                        generatedText.Length, attempt);
                                    return generatedText;
                                }
                            }
                        }

                        _logger.LogWarning("Gemini API response did not contain expected structure");
                        return "No response generated from Gemini API";
                    }
                    catch (HttpRequestException ex) when (attempt < maxRetries)
                    {
                        // Network error, timeout, or connection issue
                        var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));

                        _logger.LogWarning(ex,
                            "Network error calling Gemini API. Retrying in {Delay}ms... (attempt {Attempt}/{Max})",
                            delay.TotalMilliseconds, attempt, maxRetries);

                        await Task.Delay(delay);
                    }
                    catch (TaskCanceledException ex) when (attempt < maxRetries)
                    {
                        // Request timeout
                        var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));

                        _logger.LogWarning(ex,
                            "Request timeout calling Gemini API. Retrying in {Delay}ms... (attempt {Attempt}/{Max})",
                            delay.TotalMilliseconds, attempt, maxRetries);

                        await Task.Delay(delay);
                    }
                }

                // All retries exhausted
                var finalError = string.IsNullOrEmpty(responseContent)
                    ? "We're having trouble connecting right now. Please wait a few minutes and try again."
                    : ParseGeminiError(responseContent, 503); // Assume 503 if we exhausted retries
                throw new Exception(finalError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                throw;
            }
        }

        /// <summary>
        /// Parses Gemini API error response and returns a user-friendly message
        /// </summary>
        private string ParseGeminiError(string errorResponse, int statusCode)
        {
            try
            {
                // Try to parse the JSON error response
                using var jsonDoc = JsonDocument.Parse(errorResponse);
                var root = jsonDoc.RootElement;

                // Check if response has error object
                if (root.TryGetProperty("error", out var errorObj))
                {
                    var errorCode = errorObj.TryGetProperty("code", out var codeEl) ? codeEl.GetInt32() : statusCode;
                    var errorMessage = errorObj.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : null;

                    // Return user-friendly message based on error code
                    return GetFriendlyErrorMessage(errorCode, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not parse Gemini error response");
            }

            // Fallback: return friendly message based on status code only
            return GetFriendlyErrorMessage(statusCode, null);
        }

        /// <summary>
        /// Returns a user-friendly error message based on error code
        /// </summary>
        private string GetFriendlyErrorMessage(int errorCode, string? originalMessage)
        {
            return errorCode switch
            {
                400 => "Oops! Something's not quite right with your request. Please try again with different wording.",
                401 => "We couldn't verify your access. Please contact our support team for assistance.",
                403 => "You don't have permission to use this feature right now. Please contact support if you think this is a mistake.",
                404 => "We're having trouble connecting to our system. Please try again in a few minutes or contact support.",
                429 => "Whoa! Slow down a bit. You've sent too many requests. Please wait a moment and try again.",
                500 => "Something went wrong on our end. Don't worry, it's not your fault! Please try again in a few minutes.",
                503 => "We're experiencing high demand right now. Please wait a moment and try again.",
                504 => "Your request is taking longer than expected. Please try again.",
                _ => !string.IsNullOrEmpty(originalMessage) && !originalMessage.Contains("{") && !originalMessage.Contains("}")
                    ? originalMessage // Use original message if it's not JSON
                    : "Something unexpected happened. Please try again, and if the problem continues, contact our support team."
            };
        }

        /// <summary>
        /// Generates a report based on the user's request
        /// </summary>
        /// <param name="appName">Application name</param>
        /// <param name="subject">Request subject</param>
        /// <param name="requestText">Detailed request text</param>
        /// <returns>Generated report response</returns>
        public async Task<string> GenerateReportAsync(string appName, string subject, string requestText)
        {
            // Construct a comprehensive prompt for Gemini
            // I removed this from the line after var prompt: Subject: {subject}
            var prompt = $@"You are an AI assistant helping with the '{appName}' application.
Request:
{requestText}
Please provide a detailed, well-formatted response that addresses this request. Include relevant analysis, insights, and recommendations where appropriate.
IMPORTANT INSTRUCTIONS:
- DO NOT include any code snippets in the response (no JSON, JavaScript, CSS, Python, SQL, or any other programming language code)
- If the user requests charts (pie charts, bar charts, line charts, etc.), DO NOT include any textual description or section explaining what the chart would display. The charts will be rendered by the application itself, so only include textual analysis.
- If the user requests charts(pie chart, bar chart, line chart, etc.), then generate the chart in the same type requested by the user. 
- If the user requests a chart but did not mention the chart type then use the most appropriate chart type based on the data to be displayed.";

            return await GenerateContentAsync(prompt);
        }

        /// <summary>
        /// Generates a professional email based on the user's request
        /// </summary>
        /// <param name="appName">Application name</param>
        /// <param name="subject">Request subject</param>
        /// <param name="requestText">Detailed request text describing the email to generate</param>
        /// <returns>Generated email response</returns>
        public async Task<string> GenerateEmailAsync(string appName, string subject, string requestText)
        {
            // Construct an email-specific prompt for Gemini
            var prompt = $@"You are an AI assistant helping with the '{appName}' application to generate professional emails.

Subject: {subject}

Request:
{requestText}

IMPORTANT INSTRUCTIONS:
- Generate a professional, well-formatted email that addresses the request
- Include all standard email components:
  * A clear subject line (if not already provided)
  * An appropriate greeting/salutation
  * The body of the email with proper formatting and paragraphs
  * A professional closing and signature placeholder
- Use a professional and courteous tone throughout
- Format the email using Markdown for better presentation
- DO NOT include any code snippets, JSON, or programming language syntax
- If the request involves data or reports, describe them in natural language within the email body
- Keep the email concise and focused on the main purpose
- Use proper business email etiquette and formatting

Please generate the complete email now:";

            return await GenerateContentAsync(prompt);
        }

        /// <summary>
        /// Generates a report based on the user's request and provided data from database query results
        /// </summary>
        /// <param name="appName">Application name</param>
        /// <param name="subject">Request subject</param>
        /// <param name="requestText">Detailed request text containing user's instructions</param>
        /// <param name="queryData">JSON data containing the results from the executed SQL query</param>
        /// <returns>Generated report response based on the provided data</returns>
        public async Task<string> GenerateReportWithDataAsync(string appName, string subject, string requestText, string queryData)
        {
            // Construct a comprehensive prompt for Gemini that instructs it to use the provided data
            var prompt = $@"You are an AI assistant helping with the '{appName}' application.

Subject: {subject}

User's Request:
{requestText}

Database Query Results (JSON format):
{queryData}

IMPORTANT INSTRUCTIONS:
- Generate the report based ENTIRELY on the data provided above in the 'Database Query Results' section
- Use the user's request to understand what type of report to create, what summary to include, and what table fields to display
- Format the response as a well-structured report with:
  * A clear summary section that analyzes the data
  * A properly formatted table displaying the relevant fields from the data
  * Any insights, trends, or key findings based on the actual data provided
- Use Markdown formatting for better presentation
- Include the table with proper headers and all relevant data rows
- DO NOT make up or assume any data that is not present in the query results
- If the user requested specific calculations or aggregations, perform them based on the provided data
- DO NOT include any code snippets in the response (no JSON, JavaScript, CSS, Python, SQL, or any other programming language code)
- If the user requests charts (pie charts, bar charts, line charts, etc.):
  * DO NOT include any textual description or section explaining what the chart would display
  * The charts will be rendered by the application itself automatically
  * CRITICAL: If the user requests MULTIPLE charts (e.g., 2 pie charts, a bar chart and a line chart), you MUST create a SEPARATE table for EACH chart
  * Each table should contain ONLY the data for that specific chart (e.g., if user wants a pie chart of sections per department and another pie chart of employees per department, create two separate tables)
  * Add a clear heading above each table indicating what it represents (e.g., '### Sections per Department', '### Employees per Department')
  * Each table should have exactly 2 columns: the category/label column and the value column
  * DO NOT combine multiple charts' data into one table
- Only include the data in tabular format and other textual analysis

Please provide a detailed, well-formatted report that addresses the user's request using the provided data.";

            return await GenerateContentAsync(prompt);
        }

        /// <summary>
        /// Generates a professional email based on the user's request and provided data from database query results
        /// </summary>
        /// <param name="appName">Application name</param>
        /// <param name="subject">Request subject</param>
        /// <param name="requestText">Detailed request text containing user's instructions for the email</param>
        /// <param name="queryData">JSON data containing the results from the executed SQL query</param>
        /// <returns>Generated email response based on the provided data</returns>
        public async Task<string> GenerateEmailWithDataAsync(string appName, string subject, string requestText, string queryData)
        {
            // Construct an email-specific prompt for Gemini that incorporates the data
            var prompt = $@"You are an AI assistant helping with the '{appName}' application to generate professional emails with data.

Subject: {subject}

User's Request:
{requestText}

Database Query Results (JSON format):
{queryData}

IMPORTANT INSTRUCTIONS:
- Generate a professional, well-formatted email that addresses the request
- Include all standard email components:
  * A clear subject line (if not already provided)
  * An appropriate greeting/salutation
  * The body of the email with proper formatting and paragraphs
  * A professional closing and signature placeholder
- Incorporate the data from the 'Database Query Results' into the email in a natural, professional way
- Present the data using a properly formatted table within the email if appropriate
- Use the user's request to understand what information to highlight and how to present it in the email
- Use a professional and courteous tone throughout
- Format the email using Markdown for better presentation
- DO NOT include any code snippets, JSON, or programming language syntax
- DO NOT make up or assume any data that is not present in the query results
- Keep the email concise and focused on the main purpose while including the relevant data
- Use proper business email etiquette and formatting

Please generate the complete email now:";

            return await GenerateContentAsync(prompt);
        }

        /// <summary>
        /// Generates only a table based on the user's request
        /// </summary>
        /// <param name="appName">Application name</param>
        /// <param name="subject">Request subject</param>
        /// <param name="requestText">Detailed request text</param>
        /// <returns>Generated table response</returns>
        public async Task<string> GenerateTableAsync(string appName, string subject, string requestText)
        {
            // Construct a table-specific prompt for Gemini
            var prompt = $@"You are an AI assistant helping with the '{appName}' application.

Subject: {subject}

Request:
{requestText}

IMPORTANT INSTRUCTIONS:
- Generate ONLY a table that addresses this request
- DO NOT include any introductory text, explanations, summaries, or analysis
- DO NOT include any concluding remarks or additional information
- The response should be ONLY the table in Markdown format
- Include proper table headers
- Ensure the table is well-formatted and easy to read
- DO NOT include any code snippets, JSON, or programming language syntax
- Start your response directly with the table (using Markdown table syntax)

Please provide only the table now:";

            return await GenerateContentAsync(prompt);
        }

        /// <summary>
        /// Generates only a table based on the user's request and provided data from database query results
        /// </summary>
        /// <param name="appName">Application name</param>
        /// <param name="subject">Request subject</param>
        /// <param name="requestText">Detailed request text containing user's instructions</param>
        /// <param name="queryData">JSON data containing the results from the executed SQL query</param>
        /// <returns>Generated table response based on the provided data</returns>
        public async Task<string> GenerateTableWithDataAsync(string appName, string subject, string requestText, string queryData)
        {
            // Construct a table-specific prompt for Gemini that instructs it to use the provided data
            var prompt = $@"You are an AI assistant helping with the '{appName}' application.

Subject: {subject}

User's Request:
{requestText}

Database Query Results (JSON format):
{queryData}

IMPORTANT INSTRUCTIONS:
- Generate ONLY a table based ENTIRELY on the data provided above in the 'Database Query Results' section
- DO NOT include any introductory text, explanations, summaries, or analysis
- DO NOT include any concluding remarks or additional information
- The response should be ONLY the table in Markdown format
- Use the user's request to understand what table fields to display and how to organize the data
- Include proper table headers
- Ensure the table is well-formatted with all relevant data rows
- DO NOT make up or assume any data that is not present in the query results
- DO NOT include any code snippets, JSON, or programming language syntax
- Start your response directly with the table (using Markdown table syntax)

Please provide only the table now:";

            return await GenerateContentAsync(prompt);
        }

        /// <summary>
        /// Generates only chart(s) based on the user's request
        /// </summary>
        /// <param name="appName">Application name</param>
        /// <param name="subject">Request subject</param>
        /// <param name="requestText">Detailed request text</param>
        /// <returns>Generated chart response (table format for chart data)</returns>
        public async Task<string> GenerateChartAsync(string appName, string subject, string requestText)
        {
            // Construct a chart-specific prompt for Gemini
            var prompt = $@"You are an AI assistant helping with the '{appName}' application.

Subject: {subject}

Request:
{requestText}

IMPORTANT INSTRUCTIONS:
- Generate ONLY the data table(s) needed for the requested chart(s)
- DO NOT include any introductory text, explanations, summaries, or analysis
- DO NOT include any concluding remarks or additional information
- DO NOT include any textual description of what the chart would display
- The chart(s) will be rendered by the application itself automatically
- If the user requested MULTIPLE charts (e.g., 2 pie charts, a bar chart and a line chart):
  * Create a SEPARATE table for EACH chart
  * Each table should contain ONLY the data for that specific chart
  * Add a clear heading above each table indicating what chart it represents (e.g., '### Pie Chart: Sales by Region', '### Bar Chart: Revenue by Month')
  * Each table should have exactly 2 columns: the category/label column and the value column
  * DO NOT combine multiple charts' data into one table
- If only one chart was requested, provide one table with the chart data
- Use Markdown table format for the data
- Ensure each table has proper headers
- DO NOT include any code snippets, JSON, or programming language syntax
- Start your response directly with the table(s) (using Markdown table syntax with optional headings)

Please provide only the chart data table(s) now:";

            return await GenerateContentAsync(prompt);
        }

        /// <summary>
        /// Generates only chart(s) based on the user's request and provided data from database query results
        /// </summary>
        /// <param name="appName">Application name</param>
        /// <param name="subject">Request subject</param>
        /// <param name="requestText">Detailed request text containing user's instructions</param>
        /// <param name="queryData">JSON data containing the results from the executed SQL query</param>
        /// <returns>Generated chart response (table format for chart data) based on the provided data</returns>
        public async Task<string> GenerateChartWithDataAsync(string appName, string subject, string requestText, string queryData)
        {
            // Construct a chart-specific prompt for Gemini that instructs it to use the provided data
            var prompt = $@"You are an AI assistant helping with the '{appName}' application.

Subject: {subject}

User's Request:
{requestText}

Database Query Results (JSON format):
{queryData}

IMPORTANT INSTRUCTIONS:
- Generate ONLY the data table(s) needed for the requested chart(s) based ENTIRELY on the data provided above
- DO NOT include any introductory text, explanations, summaries, or analysis
- DO NOT include any concluding remarks or additional information
- DO NOT include any textual description of what the chart would display
- The chart(s) will be rendered by the application itself automatically
- If the user requested MULTIPLE charts (e.g., 2 pie charts, a bar chart and a line chart):
  * Create a SEPARATE table for EACH chart
  * Each table should contain ONLY the data for that specific chart
  * Add a clear heading above each table indicating what chart it represents (e.g., '### Pie Chart: Sales by Region', '### Bar Chart: Revenue by Month')
  * Each table should have exactly 2 columns: the category/label column and the value column
  * DO NOT combine multiple charts' data into one table
- If only one chart was requested, provide one table with the chart data
- Use the user's request to understand what chart type(s) to create and what data to include
- Use Markdown table format for the data
- Ensure each table has proper headers
- DO NOT make up or assume any data that is not present in the query results
- DO NOT include any code snippets, JSON, or programming language syntax
- Start your response directly with the table(s) (using Markdown table syntax with optional headings)

Please provide only the chart data table(s) now:";

            return await GenerateContentAsync(prompt);
        }

        /// <summary>
        /// Generates a formal study based on the user's request
        /// </summary>
        /// <param name="appName">Application name</param>
        /// <param name="subject">Request subject</param>
        /// <param name="requestText">Detailed request text</param>
        /// <returns>Generated study response in standard academic format</returns>
        public async Task<string> GenerateStudyAsync(string appName, string subject, string requestText)
        {
            // Construct a study-specific prompt for Gemini
            var prompt = $@"You are an AI assistant helping with the '{appName}' application to generate formal academic studies.

Subject: {subject}

Request:
{requestText}

IMPORTANT INSTRUCTIONS:
- Generate a comprehensive, formal study that addresses this request
- Use standard academic study format with the following sections:
  * **Abstract**: A brief summary of the study (100-200 words)
  * **Introduction**: Background, context, and objectives
  * **Methodology**: Approach and methods used (if applicable)
  * **Results/Findings**: Key findings and data analysis
  * **Discussion**: Interpretation of results and implications
  * **Conclusion**: Summary and recommendations
  * **References** (if applicable): Any sources or citations
- Use professional academic language and tone
- Include proper headings and subheadings using Markdown formatting
- Support findings with data, examples, and analysis where appropriate
- Maintain objectivity and scholarly rigor throughout
- Use tables or charts if they enhance the presentation of data
- DO NOT include any code snippets, JSON, or programming language syntax
- Format the study using Markdown for better presentation

Please generate the complete formal study now:";

            return await GenerateContentAsync(prompt);
        }

        /// <summary>
        /// Generates a formal study based on the user's request and provided data from database query results
        /// </summary>
        /// <param name="appName">Application name</param>
        /// <param name="subject">Request subject</param>
        /// <param name="requestText">Detailed request text containing user's instructions</param>
        /// <param name="queryData">JSON data containing the results from the executed SQL query</param>
        /// <returns>Generated study response in standard academic format based on the provided data</returns>
        public async Task<string> GenerateStudyWithDataAsync(string appName, string subject, string requestText, string queryData)
        {
            // Construct a study-specific prompt for Gemini that incorporates the data
            var prompt = $@"You are an AI assistant helping with the '{appName}' application to generate formal academic studies with data.

Subject: {subject}

User's Request:
{requestText}

Database Query Results (JSON format):
{queryData}

IMPORTANT INSTRUCTIONS:
- Generate a comprehensive, formal study based ENTIRELY on the data provided above in the 'Database Query Results' section
- Use standard academic study format with the following sections:
  * **Abstract**: A brief summary of the study based on the data (100-200 words)
  * **Introduction**: Background, context, and objectives
  * **Methodology**: Data collection and analysis approach
  * **Results/Findings**: Detailed presentation and analysis of the provided data
  * **Discussion**: Interpretation of results, patterns, and implications
  * **Conclusion**: Summary of key findings and recommendations
  * **References** (if applicable): Any sources or citations
- Use professional academic language and tone
- Include proper headings and subheadings using Markdown formatting
- Present the data using properly formatted tables within the study
- Include statistical analysis, trends, and patterns observed in the data
- DO NOT make up or assume any data that is not present in the query results
- Maintain objectivity and scholarly rigor throughout
- Support all findings and conclusions with the actual data provided
- DO NOT include any code snippets, JSON, or programming language syntax
- Format the study using Markdown for better presentation

Please generate the complete formal study now:";

            return await GenerateContentAsync(prompt);
        }
    }
}
