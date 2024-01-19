namespace CartServicePOC.Model
{
    public class CartItemUpdateRequest
    {
        public Guid CartItemId { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; }
    }
}
