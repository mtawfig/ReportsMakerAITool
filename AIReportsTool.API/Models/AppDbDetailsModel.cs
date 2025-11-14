using AIReportsTool.API.Services;

namespace AIReportsTool.API.Models
{
    public class AppDbDetailsModel
    {
        public int Id { get; set; }
        public int AppId { get; set; }
        public string DbProvider { get; set; } = string.Empty;
        public string DbConnection { get; set; } = string.Empty;
        public string DbStructureJson { get; set; } = string.Empty;
    }
}

