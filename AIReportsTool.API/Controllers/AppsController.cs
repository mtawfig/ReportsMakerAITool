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
        private readonly ApplicationService _applicationService;
        private readonly ILogger<AppsController> _logger;

        public AppsController(
            OracleDbService dbService,
            ApplicationService applicationService,
            ILogger<AppsController> logger)
        {
            _dbService = dbService;
            _applicationService = applicationService;
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
                        AppId = OracleDbService.GetColumnValue<int>(row, "APPID", 0),
                        AppNameEn = OracleDbService.GetColumnValue<string>(row, "APPNAMEEN", string.Empty) ?? string.Empty,
                        AppNameAr = OracleDbService.GetColumnValue<string>(row, "APPNAMEAR", string.Empty) ?? string.Empty,
                        AppUrl = OracleDbService.GetColumnValue<string>(row, "APPURL", string.Empty) ?? string.Empty,
                        AppIcon = OracleDbService.GetColumnValue<string>(row, "APPICON", string.Empty) ?? string.Empty
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

        /// <summary>
        /// GET: api/apps/database-details/{appId}
        /// Returns database connection details for a specific application
        /// Calls: ApplicationService.GetApplicationDatabaseDetails
        /// </summary>
        /// <param name="appId">The application ID</param>
        /// <returns>Database details including connection string and structure JSON</returns>
        [HttpGet("database-details/{appId}")]
        public async Task<IActionResult> GetDatabaseDetails(int appId)
        {
            try
            {
                _logger.LogInformation("Getting database details for AppId: {AppId}", appId);

                var appDbDetails = await _applicationService.GetApplicationDatabaseDetails(appId);

                // Check if valid data was returned (Id = 0 means validation failed)
                if (appDbDetails.Id == 0)
                {
                    var errorResponse = new BaseResponseModel<AppDbDetailsModel>
                    {
                        Status = 0,
                        Message = "Application database details not found or invalid configuration",
                        Data = appDbDetails
                    };

                    return NotFound(errorResponse);
                }

                var response = new BaseResponseModel<AppDbDetailsModel>
                {
                    Status = 1,
                    Message = "Success",
                    Data = appDbDetails
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving database details for AppId: {AppId}", appId);

                var response = new BaseResponseModel<AppDbDetailsModel>
                {
                    Status = 0,
                    Message = "Failed to retrieve database details",
                    Data = new AppDbDetailsModel { Id = 0 }
                };

                return StatusCode(500, response);
            }
        }

    }
}
