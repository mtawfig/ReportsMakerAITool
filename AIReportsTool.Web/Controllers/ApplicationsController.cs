using Microsoft.AspNetCore.Mvc;
using AIReportsTool.Web.Models;
using System.Text.Json;

namespace AIReportsTool.Web.Controllers
{
    /// <summary>
    /// Controller for fetching applications from the API
    /// </summary>
    public class ApplicationsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ApplicationsController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// GET: /Applications/GetApps
        /// Fetches applications list from API and returns JSON
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetApps()
        {
            try
            {
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5100";
                var client = _httpClientFactory.CreateClient();

                var response = await client.GetAsync($"{apiBaseUrl}/api/Apps");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<List<AppModel>>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return Json(apiResponse);
                }
                else
                {
                    return Json(new ApiResponseModel<List<AppModel>>
                    {
                        Status = 0,
                        Message = "Failed to fetch applications",
                        Data = new List<AppModel>()
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponseModel<List<AppModel>>
                {
                    Status = 0,
                    Message = $"Error: {ex.Message}",
                    Data = new List<AppModel>()
                });
            }
        }
    }
}
