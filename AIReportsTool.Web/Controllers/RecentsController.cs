using Microsoft.AspNetCore.Mvc;
using AIReportsTool.Web.Models;
using System.Text.Json;

namespace AIReportsTool.Web.Controllers
{
    /// <summary>
    /// Controller for fetching recent requests from the API
    /// </summary>
    public class RecentsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public RecentsController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// GET: /Recents/GetRecents
        /// Fetches recent requests from API and returns JSON
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRecents()
        {
            try
            {
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5100";
                var client = _httpClientFactory.CreateClient();

                var response = await client.GetAsync($"{apiBaseUrl}/api/recents");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<List<RecentRequestModel>>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return Json(apiResponse);
                }
                else
                {
                    return Json(new ApiResponseModel<List<RecentRequestModel>>
                    {
                        Status = 0,
                        Message = "Failed to fetch recent requests",
                        Data = new List<RecentRequestModel>()
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponseModel<List<RecentRequestModel>>
                {
                    Status = 0,
                    Message = $"Error: {ex.Message}",
                    Data = new List<RecentRequestModel>()
                });
            }
        }
    }
}
