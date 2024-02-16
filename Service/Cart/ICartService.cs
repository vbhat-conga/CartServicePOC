using CartServicePOC.DataModel;
using CartServicePOC.Model;

namespace CartServicePOC.Service.Cart
{
    public interface ICartService
    {
        Task<bool> IsPriceIdExists(Guid id);
        Task<Guid> SaveCart(CreateCartRequest cart);
        Task<bool> IsCartExists(Guid id);
        Task<CartResponseDetail> GetCart(Guid id);
        Task<bool> UpdateCart(UpdateCartRequest cartUpdateRequest);
    }
}
