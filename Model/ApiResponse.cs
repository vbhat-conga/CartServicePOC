using System.Text.Json.Serialization;

namespace CartServicePOC.Model
{
    public class ApiResponse<T>
    {
        public ApiResponse(T data, string statusCode, string? errorMessage=null)
        {
            Data = data;
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
        }

        public T Data { get; set; }
        public string StatusCode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorMessage { get; set; }
    }
}
