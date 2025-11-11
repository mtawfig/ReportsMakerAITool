namespace AIReportsTool.Web.Models
{
    /// <summary>
    /// Represents an application for display in the UI
    /// </summary>
    public class AppModel
    {
        public int AppId { get; set; }
        public string AppNameEn { get; set; } = string.Empty;
        public string AppNameAr { get; set; } = string.Empty;
        public string AppUrl { get; set; } = string.Empty;
        public string AppIcon { get; set; } = string.Empty;
    }
}
