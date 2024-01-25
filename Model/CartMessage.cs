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
        public IEnumerable<CartItemRequest> CartItems { get; set; }
        public Guid PriceListId { get; set; }
    }
}
