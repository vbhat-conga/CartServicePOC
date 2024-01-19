using CartServicePOC.DataModel;
using CartServicePOC.Model;

namespace CartServicePOC.Service
{
    public interface ICartService
    {
        Task<bool> IsPriceIdExists(Guid id);
        Task<Guid> SaveCart(CartData cart);
        Task<bool> IsCartExists(Guid id);
        Task<CartData> GetCartStatus(Guid id);
        Task<CartData> GetCart(Guid id);

        Task<bool> UpdateCart(CartUpdateRequest cartUpdateRequest);
    }
}
