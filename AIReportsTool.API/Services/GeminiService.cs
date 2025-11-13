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
            _httpClient = new HttpClient();
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
                            throw new Exception($"Gemini API returned error: {response.StatusCode}. Response: {responseContent}");
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
                throw new Exception($"Gemini API failed after {maxRetries} attempts. Last response: {responseContent}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                throw;
            }
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
            var prompt = $@"You are an AI assistant helping with the '{appName}' application.

Subject: {subject}

Request:
{requestText}

Please provide a detailed, well-formatted response that addresses this request. Include relevant analysis, insights, and recommendations where appropriate.";

            return await GenerateContentAsync(prompt);
        }
    }
}
