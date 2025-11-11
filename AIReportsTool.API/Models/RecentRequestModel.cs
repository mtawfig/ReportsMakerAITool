namespace AIReportsTool.API.Models
{
    /// <summary>
    /// Represents a recent request with timestamp and status
    /// </summary>
    public class RecentRequestModel
    {
        public string DateTime { get; set; } = string.Empty;
        public int AppId { get; set; }
        public string AppNameEn { get; set; } = string.Empty;
        public string AppNameAr { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public int Status { get; set; }
        public string Response { get; set; } = string.Empty;
    }
}
