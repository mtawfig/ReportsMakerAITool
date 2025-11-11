using Microsoft.AspNetCore.Mvc;
using AIReportsTool.API.Models;

namespace AIReportsTool.API.Controllers
{
    /// <summary>
    /// Controller for handling report generation requests
    /// Accepts POST requests with request details and returns success response
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RequestController : ControllerBase
    {
        /// <summary>
        /// POST: api/request
        /// Submits a new report generation request
        /// </summary>
        [HttpPost]
        public IActionResult SubmitRequest([FromBody] RequestPayloadModel payload)
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

            // TODO: In production, this would:
            // 1. Save the request to database
            // 2. Queue the request for AI processing
            // 3. Return a request ID for tracking

            // For now, return hardcoded success response
            var response = new RequestResponseModel
            {
                Status = 1,
                Message = "Success",
                ResponseText = $"Report generated successfully for '{payload.Subject}'. Your request has been processed and the report is ready."
            };

            return Ok(response);
        }
    }
}
