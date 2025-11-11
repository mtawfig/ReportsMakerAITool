namespace AIReportsTool.Web.Models
{
    /// <summary>
    /// Generic API response wrapper
    /// </summary>
    public class ApiResponseModel<T>
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}
