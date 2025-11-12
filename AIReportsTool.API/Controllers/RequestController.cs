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
        private readonly GeminiService _geminiService;
        private readonly ILogger<RequestController> _logger;

        public RequestController(
            OracleDbService dbService,
            GeminiService geminiService,
            ILogger<RequestController> logger)
        {
            _dbService = dbService;
            _geminiService = geminiService;
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
                // Call Gemini API to generate response
                _logger.LogInformation("Calling Gemini API for request: {Subject}", payload.Subject);
                string geminiResponse;

                try
                {
                    geminiResponse = await _geminiService.GenerateReportAsync(
                        "IntellegentReportMaker",
                        payload.Subject,
                        payload.RequestText
                    );
                }
                catch (Exception geminiEx)
                {
                    _logger.LogError(geminiEx, "Gemini API call failed");
                    geminiResponse = $"Error generating AI response: {geminiEx.Message}";
                }

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

                // Execute stored procedure to insert request
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
                    // Update the request with Gemini response
                    await UpdateRequestResponseAsync(requestId, geminiResponse);

                    var response = new RequestResponseModel
                    {
                        Status = 1,
                        Message = dbMessage,
                        ResponseText = geminiResponse
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

        /// <summary>
        /// Updates a request with the Gemini API response
        /// </summary>
        private async Task UpdateRequestResponseAsync(int requestId, string responseText)
        {
            try
            {
                var requestIdParam = OracleDbService.CreateInputParameter(
                    "p_request_id",
                    OracleDbType.Int32,
                    requestId
                );

                var responseTextParam = OracleDbService.CreateInputParameter(
                    "p_response_text",
                    OracleDbType.Clob,
                    responseText
                );

                var statusParam = OracleDbService.CreateInputParameter(
                    "p_status",
                    OracleDbType.Int32,
                    1 // Mark as completed
                );

                var dbStatusParam = OracleDbService.CreateOutputParameter(
                    "p_db_status",
                    OracleDbType.Int32
                );

                var messageParam = OracleDbService.CreateOutputParameter(
                    "p_message",
                    OracleDbType.Varchar2,
                    4000
                );

                await _dbService.ExecuteStoredProcedureWithOutParamsAsync(
                    "P_IRT_REPORTHELPERTOOL.SP_UpdateRequestResponse",
                    requestIdParam,
                    responseTextParam,
                    statusParam,
                    dbStatusParam,
                    messageParam
                );

                _logger.LogInformation("Updated request {RequestId} with Gemini response", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating request {RequestId} with response", requestId);
                // Don't throw - we still want to return the response to the user even if update fails
            }
        }
    }
}
