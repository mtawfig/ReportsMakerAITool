using Microsoft.AspNetCore.Mvc;
using AIReportsTool.API.Models;
using AIReportsTool.API.Services;
using Oracle.ManagedDataAccess.Client;

namespace AIReportsTool.API.Controllers
{
    /// <summary>
    /// Controller for managing recent requests
    /// Returns list of recent requests from Oracle database
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RecentsController : ControllerBase
    {
        private readonly OracleDbService _dbService;
        private readonly ILogger<RecentsController> _logger;

        public RecentsController(OracleDbService dbService, ILogger<RecentsController> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/recents
        /// Returns list of recent requests from database
        /// Calls: P_IRT_REPORTHELPERTOOL.SP_GetRecentRequests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRecents()
        {
            try
            {
                // TODO: Add authentication/authorization validation here when implementing security

                // Create parameters
                var limitParam = OracleDbService.CreateInputParameter(
                    "p_limit",
                    OracleDbType.Int32,
                    10
                );
                var refCursorParam = OracleDbService.CreateRefCursorParameter("p_result");

                // Execute stored procedure
                var dataTable = await _dbService.ExecuteStoredProcedureAsync(
                    "P_IRT_REPORTHELPERTOOL.SP_GetRecentRequests",
                    limitParam,
                    refCursorParam
                );

                // Log available columns for debugging
                var columnNames = string.Join(", ", dataTable.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName));
                _logger.LogInformation($"RecentsController received columns: {columnNames}");

                // Map DataTable to RecentRequestModel list
                var recents = new List<RecentRequestModel>();
                foreach (System.Data.DataRow row in dataTable.Rows)
                {
                    recents.Add(new RecentRequestModel
                    {
                        RequestId = OracleDbService.GetColumnValue<int>(row, "REQUESTID", 0),
                        DateTime = OracleDbService.GetColumnValue<string>(row, "DATETIME", string.Empty) ?? string.Empty,
                        AppId = OracleDbService.GetColumnValue<int>(row, "APPID", 0),
                        AppNameEn = OracleDbService.GetColumnValue<string>(row, "APPNAMEEN", string.Empty) ?? string.Empty,
                        AppNameAr = OracleDbService.GetColumnValue<string>(row, "APPNAMEAR", string.Empty) ?? string.Empty,
                        Subject = OracleDbService.GetColumnValue<string>(row, "SUBJECT", string.Empty) ?? string.Empty,
                        Details = OracleDbService.GetColumnValue<string>(row, "DETAILS", string.Empty) ?? string.Empty,
                        Status = OracleDbService.GetColumnValue<int>(row, "STATUS", 0),
                        Response = OracleDbService.GetColumnValue<string>(row, "RESPONSE", string.Empty) ?? string.Empty
                    });
                }

                var response = new BaseResponseModel<List<RecentRequestModel>>
                {
                    Status = 1,
                    Message = "Success",
                    Data = recents
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent requests from database");

                var response = new BaseResponseModel<List<RecentRequestModel>>
                {
                    Status = 0,
                    Message = "Failed to retrieve recent requests",
                    Data = new List<RecentRequestModel>()
                };

                return StatusCode(500, response);
            }
        }
    }
}
