namespace AIReportsTool.API.Models
{
    /// <summary>
    /// Base response model providing consistent structure for all API responses
    /// </summary>
    public class BaseResponseModel<T>
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}
