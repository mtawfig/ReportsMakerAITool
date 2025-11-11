namespace AIReportsTool.Web.Models
{
    /// <summary>
    /// Payload for submitting a new request to the API
    /// </summary>
    public class RequestPayloadModel
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int AppId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string RequestText { get; set; } = string.Empty;
    }
}
