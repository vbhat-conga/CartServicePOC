using Microsoft.Build.Framework;

namespace CartServicePOC.Model
{
    public class CartItem
    {
        [Required]
        public Guid CartItemId { get; set; }
        public double? Price { get; set; }
        public string? Currency { get; set; }      
        public int Quantity { get; set; }

    }

    public class UpdateCartItemRequest
    {
        [Required]
        public Action Action { get; set; } = Action.UpdatePrice;
        [Required]
        public List<CartItem> CartItems { get; set; }
    }

    public enum Action
    {
        UpdatePrice,
        Reprice
    }
}
