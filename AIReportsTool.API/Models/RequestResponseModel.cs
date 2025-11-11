namespace AIReportsTool.API.Models
{
    /// <summary>
    /// Response model for request submission
    /// </summary>
    public class RequestResponseModel
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ResponseText { get; set; } = string.Empty;
    }
}
