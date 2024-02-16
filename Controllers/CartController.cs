using CartServicePOC.Exceptions;
using CartServicePOC.Helper;
using CartServicePOC.Model;
using CartServicePOC.Service.Cart;
using CartServicePOC.Service.CartItem;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CartServicePOC.Controllers
{
    // TODO: Exception handling
    // Validation.
    // Model(view) VS Data model folder, today it has been just copied with very few change.
    // Data store, today getting stored in Redis
    // Remove unused dependency.
    // Refactoring
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ILogger<CartController> _logger;
        private readonly ICartService _cartService;
        private readonly ICartItemService _cartItemService;
        private readonly ActivitySource _activitySource = new(Instrumentation.ActivitySourceName);
        public CartController(ILogger<CartController> logger, ICartService cartService, ICartItemService cartItemService)
        {
            _logger = logger;
            _cartService = cartService;
            _cartItemService = cartItemService;

        }

        [HttpPost]
        public async Task<ActionResult> CreateCart(CreateCartRequest createCartRequests)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartController)} : CreateCart", ActivityKind.Server);
            if (!await _cartService.IsPriceIdExists(createCartRequests.PriceList.Id))
            {
                throw new PriceListIdNotFoundException($"Price list id : {createCartRequests.PriceList.Id} doesn't exist");
            }
            var id = await _cartService.SaveCart(createCartRequests);
            var apiResponse = new ApiResponse<CreateCartResponse>(
                new CreateCartResponse { CartId = id },
                201);
            return Created(Request.Path, apiResponse);
        }


        [HttpGet("{id}/status")]
        public async Task<ActionResult> GetCartStatus([FromRoute] Guid id)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartController)} : GetCartStatus", ActivityKind.Server);
            activity?.SetTag("CartId", id);
            if (!await _cartService.IsCartExists(id))
            {
                throw new CartNotFoundException($"cart with cart id : {id} doesn't exist");
            }
            var cart = await _cartService.GetCart(id);
            var apiResponse = new ApiResponse<CartResponseDetail>(cart,
                200);
            return Ok(apiResponse);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateCart([FromRoute] Guid id, UpdateCartRequest cartUpdateRequest)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartController)} : UpdateCart", ActivityKind.Server);
            activity?.SetTag("CartId", id);
            if (!await _cartService.IsCartExists(id))
            {
                throw new CartNotFoundException($"cart with cart id : {id} doesn't exist");
            }
            if (await _cartService.UpdateCart(cartUpdateRequest))
            {
                return NoContent();
            }
            throw new Exception("Error while updating cart");
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetCartById([FromRoute] Guid id, [FromQuery]GetCartQueryParameter getCartQueryParameter)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartController)} : GetCartById", ActivityKind.Server);
            activity?.SetTag("CartId", id);
            if (!await _cartService.IsCartExists(id))
                throw new CartNotFoundException($"cart with cart id : {id} doesn't exist");
            
            if(getCartQueryParameter.IncludeLineItems)
            {
                var cartInfo = await _cartItemService.GetCartItems(id);
                if (cartInfo != null)
                {
                    var cartdetailResponse = new ApiResponse<CartResponseDetail>(cartInfo, 200);
                    return Ok(cartdetailResponse);
                }
            }
            var cart = await _cartService.GetCart(id);
            var cartResponse = new ApiResponse<CartResponseDetail>(cart,200);
            return Ok(cartResponse);
        }


        [HttpPost("{id}/items")]
        public async Task<ActionResult<ApiResponse<string>>> AddCartItem([FromRoute] Guid id, List<AddCartItemRequest> cartItemRequests)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartController)} : AddCartItem", ActivityKind.Server);
            activity?.SetTag("CartId", id);
            await _cartItemService.PublishMessage(id, cartItemRequests);
            await _cartItemService.AddLineItem(cartItemRequests, id);
            var apiResponse = new ApiResponse<string>("Success",201);
            return Created(Request.Path, apiResponse);
        }


        [HttpPatch("{id}/items")]
        public async Task<ActionResult> UpdateCartItems([FromRoute] Guid id, UpdateCartItemRequest cartItemUpdates)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartController)} : UpdateCartItems", ActivityKind.Server);
            activity?.SetTag("CartId", id);
            if (await _cartItemService.UpdateCartItem(cartItemUpdates, id))
            {
                var apiResponse = new ApiResponse<string>("Success",200);
                return NoContent();
            }
            throw new System.Exception("Error while updating cart items");
        }

        [HttpPost("{id}/items/query")]
        public async Task<ActionResult> GetLineItem([FromRoute] Guid id, GetItemsRequest lineItemQueryRequest)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartController)} : GetLineItem", ActivityKind.Server);
            activity?.SetTag("CartId", id);
            if (!await _cartService.IsCartExists(id))
                throw new CartNotFoundException($"cart with cart id : {id} doesn't exist");

            var cartItemInfo = await _cartItemService.GetCartItemsByIds(id, lineItemQueryRequest);
            if (cartItemInfo != null)
            {
                var apiResponse = new ApiResponse<IEnumerable<CartItemResponse>>(cartItemInfo,
                   200);
                return Ok(apiResponse);
            }
            throw new System.Exception("Error while getting cart items");
        }
    }
}
