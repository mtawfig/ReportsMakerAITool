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
            const int maxRetries = 3;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    _logger.LogInformation("Calling Gemini API with prompt length: {Length} (attempt {Attempt}/{Max})",
                        prompt.Length, retryCount + 1, maxRetries);

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

                    // Call Gemini API
                    var response = await _httpClient.PostAsync(endpoint, httpContent);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        // Check if it's a 503 (overloaded) error
                        if ((int)response.StatusCode == 503 && retryCount < maxRetries - 1)
                        {
                            retryCount++;
                            var delaySeconds = retryCount * 2; // 2, 4 seconds
                            _logger.LogWarning("Gemini API overloaded (503). Retrying in {Delay} seconds... (attempt {Attempt}/{Max})",
                                delaySeconds, retryCount + 1, maxRetries);
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                            continue; // Retry
                        }

                        _logger.LogError("Gemini API error: {StatusCode} - {Content}. Endpoint: {Endpoint}",
                            response.StatusCode, responseContent, endpoint);
                        throw new Exception($"Gemini API returned error: {response.StatusCode}. Response: {responseContent}");
                    }

                    // Parse the response
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
                                _logger.LogInformation("Gemini API returned response length: {Length}",
                                    generatedText.Length);
                                return generatedText;
                            }
                        }
                    }

                    _logger.LogWarning("Gemini API response did not contain expected structure");
                    return "No response generated from Gemini API";
                }
                catch (Exception ex) when (ex.Message.Contains("503") && retryCount < maxRetries - 1)
                {
                    retryCount++;
                    var delaySeconds = retryCount * 2;
                    _logger.LogWarning(ex, "Gemini API error. Retrying in {Delay} seconds... (attempt {Attempt}/{Max})",
                        delaySeconds, retryCount + 1, maxRetries);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calling Gemini API");
                    throw;
                }
            }

            throw new Exception("Gemini API is currently overloaded. Please try again in a few minutes.");
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
