using CartServicePOC.DataModel;
using CartServicePOC.Model;

namespace CartServicePOC.Service.CartItem
{
    public interface ICartItemService
    {
        Task<IEnumerable<Guid>> AddLineItem(IEnumerable<AddCartItemRequest> cartItemRequests, Guid guid);
        Task PublishMessage(Guid id, IEnumerable<AddCartItemRequest> cartItemRequest);
        Task<bool> UpdateCartItem(UpdateCartItemRequest cartItemRequests, Guid guid);
        Task<CartResponseDetail> GetCartItems(Guid id);
        Task<IEnumerable<CartItemResponse>> GetCartItemsByIds(Guid id, GetItemsRequest lineItemQueryRequest);

    }
}
