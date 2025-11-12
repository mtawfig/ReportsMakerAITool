using Microsoft.AspNetCore.Mvc;
using AIReportsTool.API.Models;
using AIReportsTool.API.Services;
using Oracle.ManagedDataAccess.Client;

namespace AIReportsTool.API.Controllers
{
    /// <summary>
    /// Controller for managing applications list
    /// Returns list of applications from Oracle database
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AppsController : ControllerBase
    {
        private readonly OracleDbService _dbService;
        private readonly ILogger<AppsController> _logger;

        public AppsController(OracleDbService dbService, ILogger<AppsController> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/apps
        /// Returns list of available applications from database
        /// Calls: P_IRT_REPORTHELPERTOOL.SP_GetApplications
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetApps()
        {
            try
            {
                // TODO: Add authentication/authorization validation here when implementing security

                // Create REF CURSOR output parameter
                var refCursorParam = OracleDbService.CreateRefCursorParameter("p_result");

                // Execute stored procedure
                var dataTable = await _dbService.ExecuteStoredProcedureAsync(
                    "P_IRT_REPORTHELPERTOOL.SP_GetApplications",
                    refCursorParam
                );

                // Log available columns for debugging
                var columnNames = string.Join(", ", dataTable.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName));
                _logger.LogInformation($"AppsController received columns: {columnNames}");

                // Map DataTable to AppModel list
                var apps = new List<AppModel>();
                foreach (System.Data.DataRow row in dataTable.Rows)
                {
                    apps.Add(new AppModel
                    {
                        AppId = OracleDbService.GetColumnValue<decimal>(row, "AppId", 0) != 0
                            ? Convert.ToInt32(OracleDbService.GetColumnValue<decimal>(row, "AppId", 0))
                            : 0,
                        AppNameEn = OracleDbService.GetColumnValue<string>(row, "AppNameEn", string.Empty) ?? string.Empty,
                        AppNameAr = OracleDbService.GetColumnValue<string>(row, "AppNameAr", string.Empty) ?? string.Empty,
                        AppUrl = OracleDbService.GetColumnValue<string>(row, "AppUrl", string.Empty) ?? string.Empty,
                        AppIcon = OracleDbService.GetColumnValue<string>(row, "AppIcon", string.Empty) ?? string.Empty
                    });
                }

                var response = new BaseResponseModel<List<AppModel>>
                {
                    Status = 1,
                    Message = "Success",
                    Data = apps
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applications from database");

                var response = new BaseResponseModel<List<AppModel>>
                {
                    Status = 0,
                    Message = "Failed to retrieve applications",
                    Data = new List<AppModel>()
                };

                return StatusCode(500, response);
            }
        }
    }
}
