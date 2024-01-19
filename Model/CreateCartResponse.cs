using CartServicePOC.DataModel;

namespace CartServicePOC.Model
{
    public class CartStatusResponse
    {
        public CartStatus CartStatus { get; set; }
    }

    public class CreateCartResponse
    {
        public Guid CartId { get; set; }
    }

}
