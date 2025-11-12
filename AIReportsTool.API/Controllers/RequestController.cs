using Microsoft.AspNetCore.Mvc;
using AIReportsTool.API.Models;
using AIReportsTool.API.Services;
using Oracle.ManagedDataAccess.Client;

namespace AIReportsTool.API.Controllers
{
    /// <summary>
    /// Controller for handling report generation requests
    /// Accepts POST requests with request details and saves to Oracle database
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RequestController : ControllerBase
    {
        private readonly OracleDbService _dbService;
        private readonly ILogger<RequestController> _logger;

        public RequestController(OracleDbService dbService, ILogger<RequestController> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        /// <summary>
        /// POST: api/request
        /// Submits a new report generation request
        /// Calls: P_IRT_REPORTHELPERTOOL.SP_InsertRequest
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitRequest([FromBody] RequestPayloadModel payload)
        {
            // TODO: Add authentication/authorization validation here when implementing security
            // TODO: Validate token when implementing security

            // Validate required fields
            if (payload == null)
            {
                return BadRequest(new RequestResponseModel
                {
                    Status = 0,
                    Message = "Invalid payload",
                    ResponseText = string.Empty
                });
            }

            if (payload.AppId <= 0)
            {
                return BadRequest(new RequestResponseModel
                {
                    Status = 0,
                    Message = "Invalid application ID",
                    ResponseText = string.Empty
                });
            }

            if (string.IsNullOrWhiteSpace(payload.Subject))
            {
                return BadRequest(new RequestResponseModel
                {
                    Status = 0,
                    Message = "Subject is required",
                    ResponseText = string.Empty
                });
            }

            if (string.IsNullOrWhiteSpace(payload.RequestText))
            {
                return BadRequest(new RequestResponseModel
                {
                    Status = 0,
                    Message = "Request text is required",
                    ResponseText = string.Empty
                });
            }

            try
            {
                // Create parameters for stored procedure
                var appIdParam = OracleDbService.CreateInputParameter(
                    "p_app_id",
                    OracleDbType.Int32,
                    payload.AppId
                );

                var usernameParam = OracleDbService.CreateInputParameter(
                    "p_username",
                    OracleDbType.Varchar2,
                    payload.Username ?? string.Empty
                );

                var subjectParam = OracleDbService.CreateInputParameter(
                    "p_subject",
                    OracleDbType.Varchar2,
                    payload.Subject
                );

                var requestTextParam = OracleDbService.CreateInputParameter(
                    "p_request_text",
                    OracleDbType.Clob,
                    payload.RequestText
                );

                var responseFormatParam = OracleDbService.CreateInputParameter(
                    "p_response_format",
                    OracleDbType.Varchar2,
                    DBNull.Value // Will be set when format selection is implemented in UI
                );

                var tokenParam = OracleDbService.CreateInputParameter(
                    "p_token",
                    OracleDbType.Varchar2,
                    string.IsNullOrWhiteSpace(payload.Token) ? DBNull.Value : payload.Token
                );

                // Output parameters
                var requestIdParam = OracleDbService.CreateOutputParameter(
                    "p_request_id",
                    OracleDbType.Int32
                );

                var statusParam = OracleDbService.CreateOutputParameter(
                    "p_status",
                    OracleDbType.Int32
                );

                var messageParam = OracleDbService.CreateOutputParameter(
                    "p_message",
                    OracleDbType.Varchar2,
                    4000
                );

                // Execute stored procedure
                var outParams = await _dbService.ExecuteStoredProcedureWithOutParamsAsync(
                    "P_IRT_REPORTHELPERTOOL.SP_InsertRequest",
                    appIdParam,
                    usernameParam,
                    subjectParam,
                    requestTextParam,
                    responseFormatParam,
                    tokenParam,
                    requestIdParam,
                    statusParam,
                    messageParam
                );

                // Extract output parameters
                var dbStatus = Convert.ToInt32(outParams["p_status"]);
                var dbMessage = outParams["p_message"]?.ToString() ?? "Unknown error";
                var requestId = outParams["p_request_id"] != DBNull.Value
                    ? Convert.ToInt32(outParams["p_request_id"])
                    : 0;

                if (dbStatus == 1)
                {
                    // TODO: In production, this would queue the request for AI processing
                    // For now, return a mock success response
                    var response = new RequestResponseModel
                    {
                        Status = 1,
                        Message = dbMessage,
                        ResponseText = $"Report generated successfully for '{payload.Subject}'. Your request has been processed and saved with ID: {requestId}."
                    };

                    return Ok(response);
                }
                else
                {
                    // Database returned an error
                    var response = new RequestResponseModel
                    {
                        Status = 0,
                        Message = dbMessage,
                        ResponseText = string.Empty
                    };

                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting request to database");

                var response = new RequestResponseModel
                {
                    Status = 0,
                    Message = "Failed to submit request",
                    ResponseText = string.Empty
                };

                return StatusCode(500, response);
            }
        }
    }
}
