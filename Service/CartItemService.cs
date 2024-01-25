using CartServicePOC.DataModel;
using CartServicePOC.Extensions;
using CartServicePOC.Helper;
using CartServicePOC.Model;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Diagnostics;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CartServicePOC.Service
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

        public async Task<IEnumerable<Guid>> AddLineItem(IEnumerable<CartItemRequest> cartItemRequests, Guid cartId)
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
                    var hashEntry = RedisExtension.ToHashEntries(item);
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
        public async Task PublishMessage(Guid cartId, IEnumerable<CartItemRequest> cartItemRequest)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartService)}: PublishMessage", ActivityKind.Server);
            {
                var result = await _dbContext.Carts.SingleOrDefaultAsync(x => x.CartId == cartId);
                if (result == null)
                    return;

                var cartMessage = new CartMessage
                {
                    CartId = cartId,
                    CartItems = cartItemRequest,
                    PriceListId = result.PriceListId
                };
                InstrumentationHelper.AddActivityToRequest(activity, cartMessage, "POST api/{cartid}/items", "PublishMessage");
                var nameValueEntry = new NameValueEntry[]
                {
                new NameValueEntry(nameof(cartItemRequest),JsonSerializer.Serialize(cartMessage))
                };
                await _database.StreamAddAsync("config-stream", nameValueEntry);
            }
        }

        public async Task<bool> UpdateCartItem(IEnumerable<CartItemUpdateRequest> cartItemRequests, Guid cartId)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartService)}: UpdateCartItem", ActivityKind.Server);
            var batch = _database.CreateBatch();
            var tasks = new List<Task>();

            foreach (var cartItemRequest in cartItemRequests)
            {
                var id = cartItemRequest.CartItemId.ToString();
                var data = new HashEntry[]
                {
                        new HashEntry(nameof(cartItemRequest.Price), cartItemRequest.Price),
                        new HashEntry(nameof(cartItemRequest.Currency), cartItemRequest.Currency),
                };
                tasks.Add(batch.HashSetAsync($"h-cpq-{id}", data));
            }
            batch.Execute();
            await Task.WhenAll(tasks);          
            return true;
        }

        public async Task<CartDetailResponse> GetCartItems(Guid id)
        {
            using var activity = _activitySource.StartActivity($"{nameof(CartService)}: GetCartItems", ActivityKind.Server);
            var cart = new CartDetailResponse
            {
                CartId = id,
                StatusId = CartStatus.Created,
                CartItems = new List<CartItemInfo>()
            };
            var hasEntry = await _database.HashGetAllAsync($"h-cpq-{id}");
            var cartInfo = RedisExtension.ConvertFromRedis<CartDetailResponse>(hasEntry);
            var sets = await _database.SetMembersAsync($"se-cpq-{id}");
            var batch = _database.CreateBatch();
            var tasks = new List<Task<HashEntry[]>>();
            foreach (var set in sets)
            {
                tasks.Add(batch.HashGetAllAsync($"h-cpq-{set}"));
            }
            batch.Execute();
            var values = await Task.WhenAll(tasks);
            var cartItems = values.Select(RedisExtension.ConvertFromRedis<CartItemInfo>);
            if (cartItems != null && cartItems.Any() && cartInfo != null)
            {
                cart.PriceListId = cartInfo.PriceListId;
                cart.Name = cartInfo.Name;
                cart.StatusId = cartInfo.StatusId;
                cart.CartItems = cartItems;
                cart.Price = cartInfo.Price;
            }
            return cart;
        }
    }
}
