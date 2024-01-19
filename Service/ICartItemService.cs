using CartServicePOC.DataModel;
using CartServicePOC.Model;

namespace CartServicePOC.Service
{
    public interface ICartItemService
    {
        Task<IEnumerable<Guid>> AddLineItem(IEnumerable<CartItemRequest> cartItemRequests, Guid guid);
        Task PublishMessage(Guid id, IEnumerable<CartItemRequest> cartItemRequest);
        Task<bool> UpdateCartItem(IEnumerable<CartItemUpdateRequest> cartItemRequests, Guid guid);
        Task<CartDetailResponse> GetCartItems(Guid id);

    }
}
