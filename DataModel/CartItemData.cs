using CartServicePOC.Model;

namespace CartServicePOC.DataModel
{
    public class CartItemData
    {
        public Guid CartItemId { get; set; }
        public bool IsPrimaryLine { get; set; }
        public LineType LineType { get; set; }
        public int Quantity { get; set; }
        public string? ExternalId { get; set; }
        public int PrimaryTaxLineNumber { get; set; }

        public Guid CartId { get; set; }
        public virtual CartData cart { get; set; }

        public double? Price { get; set; } = null;

        public string Currency { get; set; } = "USD";

        public Guid ProductId { get; set; }
    }
}
