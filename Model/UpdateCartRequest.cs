using CartServicePOC.DataModel;

namespace CartServicePOC.Model
{
    public class UpdateCartRequest
    {
        public Guid CartId { get; set; }
        public CartStatus Status { get; set; }
        public double Price { get; set; }

        public Guid PriceListId { get; set; }

    }
}
