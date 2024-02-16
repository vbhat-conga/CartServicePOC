using CartServicePOC.DataModel;
using System.Text.Json.Serialization;

namespace CartServicePOC.Model
{
    public class BaseMessage
    {
        public Dictionary<string, byte[]> AdditonalInfo { get; set; }

        public BaseMessage()
        {
            AdditonalInfo = new();
        }
    }
    public class CartMessage: BaseMessage
    {
        public Guid CartId { get; set; }
        public IEnumerable<CartItemResponse> CartItems { get; set; }
        public Guid PriceListId { get; set; }
        public CartAction CartAction { get; set; } = CartAction.ConfigureAndPrice;

    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CartAction
    {
        ConfigureAndPrice,
        Price,
        Reprice
    }
}
