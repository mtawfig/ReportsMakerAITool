using Microsoft.AspNetCore.Mvc;
using AIReportsTool.API.Models;

namespace AIReportsTool.API.Controllers
{
    /// <summary>
    /// Controller for managing recent requests
    /// Returns hardcoded list of 10 recent requests
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RecentsController : ControllerBase
    {
        /// <summary>
        /// GET: api/recents
        /// Returns list of recent requests
        /// </summary>
        [HttpGet]
        public IActionResult GetRecents()
        {
            // TODO: Add authentication/authorization validation here when implementing security

            var recents = new List<RecentRequestModel>
            {
                new RecentRequestModel
                {
                    DateTime = "10112025123045",
                    AppId = 1,
                    AppNameEn = "Sales Analyzer",
                    AppNameAr = "محلل المبيعات",
                    Subject = "Monthly Sales Report",
                    Details = "Generate Q1 summary with charts and KPIs",
                    Status = 1,
                    Response = "Report generated successfully"
                },
                new RecentRequestModel
                {
                    DateTime = "10112025113022",
                    AppId = 2,
                    AppNameEn = "HR Insights",
                    AppNameAr = "تحليلات الموارد البشرية",
                    Subject = "Employee Turnover Analysis",
                    Details = "Analyze employee turnover rates for 2024",
                    Status = 1,
                    Response = "Analysis completed successfully"
                },
                new RecentRequestModel
                {
                    DateTime = "10112025102015",
                    AppId = 3,
                    AppNameEn = "Finance Tracker",
                    AppNameAr = "متتبع المالية",
                    Subject = "Budget vs Actual Report",
                    Details = "Compare budget with actual expenses for Q1",
                    Status = 1,
                    Response = "Report generated successfully"
                },
                new RecentRequestModel
                {
                    DateTime = "10112025095030",
                    AppId = 4,
                    AppNameEn = "Inventory Control",
                    AppNameAr = "مراقبة المخزون",
                    Subject = "Stock Level Report",
                    Details = "Show current stock levels and reorder points",
                    Status = 1,
                    Response = "Inventory report generated"
                },
                new RecentRequestModel
                {
                    DateTime = "10112025084512",
                    AppId = 5,
                    AppNameEn = "Customer Reports",
                    AppNameAr = "تقارير العملاء",
                    Subject = "Customer Satisfaction Survey",
                    Details = "Analyze customer feedback from last quarter",
                    Status = 1,
                    Response = "Survey analysis completed"
                },
                new RecentRequestModel
                {
                    DateTime = "09112025163045",
                    AppId = 1,
                    AppNameEn = "Sales Analyzer",
                    AppNameAr = "محلل المبيعات",
                    Subject = "Top Products Report",
                    Details = "List top 10 selling products by revenue",
                    Status = 1,
                    Response = "Product rankings generated"
                },
                new RecentRequestModel
                {
                    DateTime = "09112025151230",
                    AppId = 2,
                    AppNameEn = "HR Insights",
                    AppNameAr = "تحليلات الموارد البشرية",
                    Subject = "Recruitment Pipeline",
                    Details = "Show current open positions and candidates",
                    Status = 1,
                    Response = "Pipeline report generated"
                },
                new RecentRequestModel
                {
                    DateTime = "09112025140015",
                    AppId = 3,
                    AppNameEn = "Finance Tracker",
                    AppNameAr = "متتبع المالية",
                    Subject = "Cash Flow Statement",
                    Details = "Generate cash flow report for last month",
                    Status = 1,
                    Response = "Statement generated successfully"
                },
                new RecentRequestModel
                {
                    DateTime = "09112025125545",
                    AppId = 4,
                    AppNameEn = "Inventory Control",
                    AppNameAr = "مراقبة المخزون",
                    Subject = "Low Stock Alert",
                    Details = "List items below minimum stock levels",
                    Status = 1,
                    Response = "Alert report generated"
                },
                new RecentRequestModel
                {
                    DateTime = "09112025114020",
                    AppId = 5,
                    AppNameEn = "Customer Reports",
                    AppNameAr = "تقارير العملاء",
                    Subject = "Customer Lifetime Value",
                    Details = "Calculate CLV for top 100 customers",
                    Status = 1,
                    Response = "CLV analysis completed"
                }
            };

            var response = new BaseResponseModel<List<RecentRequestModel>>
            {
                Status = 1,
                Message = "Success",
                Data = recents
            };

            return Ok(response);
        }
    }
}
