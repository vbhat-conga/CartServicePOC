namespace CartServicePOC.Model
{
    public class CartMessage
    {
        public Guid CartId { get; set; }
        public IEnumerable<CartItemRequest> CartItems { get; set; }
        public Guid PriceListId { get; set; }
    }
}
