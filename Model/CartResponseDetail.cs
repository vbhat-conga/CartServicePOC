using CartServicePOC.DataModel;

namespace CartServicePOC.Model
{
    public class CartResponseDetail
    {
        public Guid CartId { get; set; }
        public string Name { get; set; }
        public Guid PriceListId { get; set; }
        public CartStatus Status { get; set; }

        public IEnumerable<CartItemResponse> CartItems { get; set; }

        public double Price { get; set; } = 0.0;
    }

    public class CartItemResponse
    {
        public Guid CartItemId { get; set; }
        public bool IsPrimaryLine { get; set; }
        public LineType LineType { get; set; }
        public int Quantity { get; set; }
        public string? ExternalId { get; set; }
        public int PrimaryTaxLineNumber { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; } = "USD";
        public Guid ProductId { get; set; }

    }
}
