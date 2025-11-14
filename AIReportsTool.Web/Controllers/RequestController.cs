using Microsoft.AspNetCore.Mvc;
using AIReportsTool.Web.Models;
using System.Text;
using System.Text.Json;

namespace AIReportsTool.Web.Controllers
{
    /// <summary>
    /// Controller for submitting report generation requests to the API
    /// </summary>
    public class RequestController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public RequestController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// POST: /Request/Submit
        /// Submits a new report generation request to the API
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] RequestPayloadModel payload)
        {
            try
            {
                if (payload == null)
                {
                    return Json(new RequestResponseModel
                    {
                        Status = 0,
                        Message = "Invalid payload",
                        ResponseText = string.Empty
                    });
                }

                // Get PC username from environment
                if (string.IsNullOrWhiteSpace(payload.Username))
                {
                    payload.Username = Environment.UserName;
                }

                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5100";
                var client = _httpClientFactory.CreateClient();

                var jsonContent = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{apiBaseUrl}/api/Request", httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<RequestResponseModel>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return Json(apiResponse);
                }
                else
                {
                    return Json(new RequestResponseModel
                    {
                        Status = 0,
                        Message = "Failed to submit request",
                        ResponseText = string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new RequestResponseModel
                {
                    Status = 0,
                    Message = $"Error: {ex.Message}",
                    ResponseText = string.Empty
                });
            }
        }

        /// <summary>
        /// POST: /Request/GenerateQuery
        /// Generates an SQL query based on user request and application database structure
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateQuery([FromBody] RequestPayloadModel payload)
        {
            try
            {
                if (payload == null)
                {
                    return Json(new RequestResponseModel
                    {
                        Status = 0,
                        Message = "Invalid payload",
                        ResponseText = string.Empty
                    });
                }

                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5100";
                var client = _httpClientFactory.CreateClient();

                var jsonContent = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{apiBaseUrl}/api/Request/generate-query", httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<RequestResponseModel>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return Json(apiResponse);
                }
                else
                {
                    return Json(new RequestResponseModel
                    {
                        Status = 0,
                        Message = "Failed to generate query",
                        ResponseText = string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new RequestResponseModel
                {
                    Status = 0,
                    Message = $"Error: {ex.Message}",
                    ResponseText = string.Empty
                });
            }
        }
    }
}
