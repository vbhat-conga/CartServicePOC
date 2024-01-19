using CartServicePOC.DataModel;

namespace CartServicePOC.Model
{
    public class CartUpdateRequest
    {
        public Guid CartId { get; set; }
        public CartStatus StatusId { get; set; }
        public double Price { get; set; }

        public Guid PriceListId { get; set; }

    }
}
