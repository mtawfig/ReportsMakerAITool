using Microsoft.AspNetCore.Mvc;
using AIReportsTool.API.Models;

namespace AIReportsTool.API.Controllers
{
    /// <summary>
    /// Controller for managing applications list
    /// Returns hardcoded list of 5 applications
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AppsController : ControllerBase
    {
        /// <summary>
        /// GET: api/apps
        /// Returns list of available applications
        /// </summary>
        [HttpGet]
        public IActionResult GetApps()
        {
            // TODO: Add authentication/authorization validation here when implementing security

            var apps = new List<AppModel>
            {
                new AppModel
                {
                    AppId = 1,
                    AppNameEn = "Sales Analyzer",
                    AppNameAr = "محلل المبيعات",
                    AppUrl = "https://app1.com",
                    AppIcon = "/icons/sales.png"
                },
                new AppModel
                {
                    AppId = 2,
                    AppNameEn = "HR Insights",
                    AppNameAr = "تحليلات الموارد البشرية",
                    AppUrl = "https://app2.com",
                    AppIcon = "/icons/hr.png"
                },
                new AppModel
                {
                    AppId = 3,
                    AppNameEn = "Finance Tracker",
                    AppNameAr = "متتبع المالية",
                    AppUrl = "https://app3.com",
                    AppIcon = "/icons/finance.png"
                },
                new AppModel
                {
                    AppId = 4,
                    AppNameEn = "Inventory Control",
                    AppNameAr = "مراقبة المخزون",
                    AppUrl = "https://app4.com",
                    AppIcon = "/icons/inventory.png"
                },
                new AppModel
                {
                    AppId = 5,
                    AppNameEn = "Customer Reports",
                    AppNameAr = "تقارير العملاء",
                    AppUrl = "https://app5.com",
                    AppIcon = "/icons/customers.png"
                }
            };

            var response = new BaseResponseModel<List<AppModel>>
            {
                Status = 1,
                Message = "Success",
                Data = apps
            };

            return Ok(response);
        }
    }
}
