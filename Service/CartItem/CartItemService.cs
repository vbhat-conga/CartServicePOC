using CartServicePOC.DataModel;
using CartServicePOC.Extensions;
using CartServicePOC.Helper;
using CartServicePOC.Model;
using CartServicePOC.Service.Cart;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Diagnostics;
using Action = CartServicePOC.Model.Action;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CartServicePOC.Service.CartItem
{
    // TODO: Exception handling
    // Redis data structure to use?
    // Do we need to persist in DB?
    public class CartItemService : ICartItemService
    {
        private readonly CartDbContext _dbContext;
        private readonly IDatabase _database;
        private readonly ILogger<CartItemService> _logger;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ActivitySource _activitySource = new(Instrumentation.ActivitySourceName);
        public CartItemService(CartDbContext dbContext, ILogger<CartItemService> logger, IConnectionMultiplexer connectionMultiplexer)
        {
            _dbContext = dbContext;
            _connectionMultiplexer = connectionMultiplexer;
            _database = _connectionMultiplexer.GetDatabase();
            _logger = logger;
        }

        public async Task<IEnumerable<Guid>> AddLineItem(IEnumerable<AddCartItemRequest> cartItemRequests, Guid cartId)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartService)}: AddLineItem", ActivityKind.Server);
            {
                var batch = _database.CreateBatch();
                var taskList = new List<Task>();
                var batch1 = _database.CreateBatch();
                var taskList2 = new List<Task<bool>>();
                var itemData = new List<CartItemData>();
                cartItemRequests.ToList().ForEach(cartItem =>
                {
                    var item = new CartItemData
                    {
                        CartId = cartId,
                        CartItemId = cartItem.ItemId,
                        IsPrimaryLine = cartItem.IsPrimaryLine,
                        LineType = cartItem.LineType,
                        Quantity = cartItem.Quantity,
                        PrimaryTaxLineNumber = cartItem.PrimaryTaxLineNumber,
                        ProductId = cartItem.Product.Id
                    };
                    itemData.Add(item);
                    var id = item.CartItemId.ToString();
                    var hashEntry = item.ToHashEntries();
                    taskList.Add(batch.HashSetAsync($"h-cpq-{id}", hashEntry));
                    taskList2.Add(batch1.SetAddAsync($"se-cpq-{cartId}", id));
                });

                batch.Execute();
                batch1.Execute();
                await Task.WhenAll(taskList);
                await Task.WhenAll(taskList2);
                return itemData.Select(x => x.CartItemId);
            }
        }


        //TODO : we can split lines and publish a message so that
        // multiple config and pricing does its job on sub set and then need to aggregate.
        public async Task PublishMessage(Guid cartId, IEnumerable<AddCartItemRequest> cartItemRequest)
        {
            _logger.LogInformation("started sending message to stream");
            using var activity = _activitySource.StartActivity($"{nameof(CartService)}: PublishMessage", ActivityKind.Server);
            {
                var result = await _dbContext.Carts.SingleOrDefaultAsync(x => x.CartId == cartId);
                if (result == null)
                {
                    _logger.LogWarning($"cart Id:{cartId} doesn't exist");
                    return;
                }
                var cartItemInfo = cartItemRequest.Select(x =>
                new CartItemResponse
                {
                    CartItemId = x.ItemId,
                    ProductId = x.Product.Id,
                    LineType = x.LineType,
                    IsPrimaryLine = x.IsPrimaryLine,
                    ExternalId = x.ExternalId,
                    Quantity = x.Quantity,
                    PrimaryTaxLineNumber = x.PrimaryTaxLineNumber
                });

                var cartMessage = new CartMessage
                {
                    CartAction = CartAction.ConfigureAndPrice,
                    CartId = cartId,
                    CartItems = cartItemInfo,
                    PriceListId = result.PriceListId
                };

                InstrumentationHelper.AddActivityToRequest(activity, cartMessage, "POST api/{cartid}/items", "PublishMessage");
                var nameValueEntry = new NameValueEntry[]
                {
                    new NameValueEntry(nameof(cartItemRequest),JsonSerializer.Serialize(cartMessage))
                };
                await _database.StreamAddAsync("config-stream", nameValueEntry);
                _logger.LogInformation("Sent message to stream: config-stream");
            }
        }

        public async Task<bool> UpdateCartItem(UpdateCartItemRequest cartItemRequest, Guid cartId)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartService)}: UpdateCartItem", ActivityKind.Server);
            var batch = _database.CreateBatch();
            var tasks = new List<Task>();
            if (cartItemRequest.Action == Action.UpdatePrice)
            {
                foreach (var item in cartItemRequest.CartItems)
                {
                    var id = item.CartItemId.ToString();
                    var hashEntries = new HashEntry[3];

                    hashEntries = new HashEntry[]
                    {
                        new HashEntry(nameof(item.Price), item.Price),
                        new HashEntry(nameof(item.Currency), item.Currency),
                        new HashEntry(nameof(item.Quantity), item.Quantity),
                    };
                    tasks.Add(batch.HashSetAsync($"h-cpq-{id}", hashEntries));
                }
            }
            else if (cartItemRequest.Action == Action.Reprice)
            {
                await GetCartItemDataAndSendToConfig(cartItemRequest, cartId);
            }
            batch.Execute();
            await Task.WhenAll(tasks);
            return true;
        }

        public async Task<CartResponseDetail> GetCartItems(Guid id)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartService)}: GetCartItems", ActivityKind.Server);
            var cart = new CartResponseDetail
            {
                CartId = id,
                Status = CartStatus.Created,
                CartItems = new List<CartItemResponse>()
            };
            var hasEntry = await _database.HashGetAllAsync($"h-cpq-{id}");
            var cartInfo = RedisExtension.ConvertFromRedis<CartResponseDetail>(hasEntry);
            var sets = await _database.SetMembersAsync($"se-cpq-{id}");
            var batch = _database.CreateBatch();
            var tasks = new List<Task<HashEntry[]>>();
            foreach (var set in sets)
            {
                tasks.Add(batch.HashGetAllAsync($"h-cpq-{set}"));
            }
            batch.Execute();
            var values = await Task.WhenAll(tasks);
            var cartItems = values.Select(RedisExtension.ConvertFromRedis<CartItemResponse>);
            if (cartItems != null && cartItems.Any() && cartInfo != null)
            {
                cart.PriceListId = cartInfo.PriceListId;
                cart.Name = cartInfo.Name;
                cart.Status = cartInfo.Status;
                cart.CartItems = cartItems;
                cart.Price = cartInfo.Price;
            }
            return cart;
        }

        public async Task<IEnumerable<CartItemResponse>> GetCartItemsByIds(Guid id, GetItemsRequest lineItemQueryRequest)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartService)}: GetCartItemsByIds", ActivityKind.Server);
            var CartItems = new List<CartItemResponse>();
            var batch = _database.CreateBatch();
            var tasks = new List<Task<HashEntry[]>>();
            foreach (var itemid in lineItemQueryRequest.Ids)
            {
                tasks.Add(batch.HashGetAllAsync($"h-cpq-{itemid}"));
            }
            batch.Execute();
            var values = await Task.WhenAll(tasks);
            var cartItems = values.Select(RedisExtension.ConvertFromRedis<CartItemResponse>);
            return cartItems;
        }

        private async Task GetCartItemDataAndSendToConfig(UpdateCartItemRequest cartItemRequest, Guid cartId)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartService)}: UpdateCartItem : GetCartItemDataAndSendToConfig", ActivityKind.Server);
            var cartItemList = new List<CartItemResponse>();
            foreach (var item in cartItemRequest.CartItems)
            {
                var hashEntry = await _database.HashGetAllAsync($"h-cpq-{item.CartItemId}");
                var cartItem = RedisExtension.ConvertFromRedis<CartItemResponse>(hashEntry);
                cartItem.Quantity = item.Quantity;
                cartItemList.Add(cartItem);
            }
            var pricelistId = await _database.HashGetAsync($"h-cpq-{cartId}", "PriceListId");
            var cartMessage = new CartMessage
            {
                CartAction = CartAction.Reprice,
                CartId = cartId,
                CartItems = cartItemList,
                PriceListId = Guid.Parse(pricelistId.ToString())
            };
            InstrumentationHelper.AddActivityToRequest(activity, cartMessage, "PATCH api/{cartid}/items", "PublishMessage");
            var nameValueEntry = new NameValueEntry[]
            {
                new NameValueEntry(nameof(cartItemRequest),JsonSerializer.Serialize(cartMessage))
            };
            await _database.StreamAddAsync("config-stream", nameValueEntry);
            _logger.LogInformation("Sent message to stream: config-stream");
        }
    }
}
