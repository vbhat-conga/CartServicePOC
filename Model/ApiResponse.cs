using System.Text.Json;
using System.Text.Json.Serialization;

namespace CartServicePOC.Model
{
    public class ApiResponse<T>
    {
        public ApiResponse(T data, int statusCode)
        {
            Data = data;
            StatusCode = statusCode;
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public T Data { get; set; }
        public int StatusCode { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
