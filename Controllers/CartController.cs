﻿using CartServicePOC.DataModel;
using CartServicePOC.Helper;
using CartServicePOC.Model;
using CartServicePOC.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;

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
        public async Task<ActionResult> CreateCart(CartRequest createCartRequests)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartController)} : CreateCart", ActivityKind.Server);
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<string>(string.Empty, "400", "Validation error"));
            }
            if (!await _cartService.IsPriceIdExists(createCartRequests.PriceList.Id))
            {
                return BadRequest(new ApiResponse<string>(string.Empty, "400", "Price list id doesn't exist"));
            }
            var cartdata = new CartData
            {
                CartId = Guid.NewGuid(),
                PriceListId = createCartRequests.PriceList.Id,
                Name = createCartRequests.Name,
                StatusId = CartStatus.Created
            };
            var id = await _cartService.SaveCart(cartdata);
            var apiResponse = new ApiResponse<CreateCartResponse>(
                new CreateCartResponse { CartId = id },
                "201");
            return Created("", apiResponse);
        }

        [HttpPost("{id}/items")]
        public async Task<ActionResult<ApiResponse<string>>> AddCartItem([FromRoute] Guid id, List<CartItemRequest> cartItemRequests)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartController)} : AddCartItem", ActivityKind.Server);
            activity?.SetTag("CartId", id);
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<string>(string.Empty, "400", "Validation error"));
            }
            await _cartItemService.PublishMessage(id, cartItemRequests);
            await _cartItemService.AddLineItem(cartItemRequests, id);
            var apiResponse = new ApiResponse<string>(
                "Success",
                "201");
            return Created("", apiResponse);
        }


        [HttpPut("{id}/items")]
        public async Task<ActionResult> UpdateCartItems([FromRoute] Guid id, List<CartItemUpdateRequest> cartItemUpdates)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartController)} : UpdateCartItems", ActivityKind.Server);
            activity?.SetTag("CartId", id);
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<string>(string.Empty, "400", "Validation error"));
            }
            if (await _cartItemService.UpdateCartItem(cartItemUpdates, id))
            {

                var apiResponse = new ApiResponse<string>(
                    "Success",
                    "200");
                return Created("", apiResponse);
            }

            return StatusCode(500);
        }


        [HttpGet("{id}/status")]
        public async Task<ActionResult> GetCartStatus([FromRoute] Guid id)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartController)} : UpdateCartItems", ActivityKind.Server);
            if (!await _cartService.IsCartExists(id))
            {
                return BadRequest(new ApiResponse<string>(string.Empty, "400", "cart id doesn't exist"));
            }

            var cart = await _cartService.GetCart(id);

            var apiResponse = new ApiResponse<CartData>(cart,
                "200");
            return Ok(apiResponse);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateCart([FromRoute] Guid id, CartUpdateRequest cartUpdateRequest)
        {

            if (!await _cartService.IsCartExists(id))
            {
                return BadRequest(new ApiResponse<string>(string.Empty, "400", "cart id doesn't exist"));
            }

            if (await _cartService.UpdateCart(cartUpdateRequest))
            {
                var apiResponse = new ApiResponse<string>("Sucess",
                "200");
                return Ok(apiResponse);
            }
            return StatusCode(500);
        }

        [HttpGet("{id}/items")]
        public async Task<ActionResult> GetLineItem([FromRoute] Guid id)
        {

            if (!await _cartService.IsCartExists(id))
            {
                return BadRequest(new ApiResponse<string>(string.Empty, "400", "cart id doesn't exist"));
            }
            var cartInfo = await _cartItemService.GetCartItems(id);
            if (cartInfo != null)
            {
                var apiResponse = new ApiResponse<CartDetailResponse>(cartInfo,
                   "200");
                return Ok(apiResponse);
            }
            return StatusCode(500);
        }
    }
}
