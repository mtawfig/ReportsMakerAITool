namespace AIReportsTool.API.Models
{
    /// <summary>
    /// Payload model for submitting a new request
    /// </summary>
    public class RequestPayloadModel
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int AppId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string RequestText { get; set; } = string.Empty;
        public string? ResponseFormat { get; set; } = null; // "email" or "report" (default: auto-detect)
    }
}
