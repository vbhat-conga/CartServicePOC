using CartServicePOC.DataModel;
using CartServicePOC.Extensions;
using CartServicePOC.Model;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CartServicePOC.Service
{
    public class CartItemService : ICartItemService
    {
        private readonly CartDbContext _dbContext;
        private readonly IDatabase _database;
        private readonly ILogger<CartItemService> _logger;
        public CartItemService(CartDbContext dbContext, ILogger<CartItemService> logger)
        {
            _dbContext = dbContext;
            var redis = ConnectionMultiplexer.Connect("localhost:6379");
            _database = redis.GetDatabase();
            _logger = logger;

        }

        public async Task<IEnumerable<Guid>> AddLineItem(IEnumerable<CartItemRequest> cartItemRequests, Guid cartId)
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
                    ProductId=cartItem.Product.Id
                };
                itemData.Add(item);
                var id = item.CartItemId.ToString();
                var hashEntry = RedisExtension.ToHashEntries(item);
                taskList.Add(batch.HashSetAsync($"h-cpq-{id}", hashEntry));
                taskList2.Add(batch1.SetAddAsync($"se-cpq-{cartId}", id));
            });

            batch.Execute();
            batch1.Execute();
            //var jsonString = JsonSerializer.Serialize(itemData);
            //var redisTask = _database.StringSetAsync($"s-cpq-{CartId}-lines", jsonString);
           // var sqlTask = _dbContext.CartItemDatas.AddRangeAsync(itemData);
            //await redisTask;
            await Task.WhenAll(taskList);
            await Task.WhenAll(taskList2);
            //await sqlTask;
            //await _dbContext.SaveChangesAsync();
            //var listData = itemData.Select(x => x.CartItemId).OrderBy(y=>y).ToList();
            //await File.WriteAllTextAsync(@"C:\Users\vbhat\source\repos\CartServicePOC\bin\Debug\net7.0\test2.txt", string.Join(",", listData));
            return itemData.Select(x => x.CartItemId);
        }



        public async Task PublishMessage(Guid cartId, IEnumerable<CartItemRequest> cartItemRequest)
        {
            var result = await _dbContext.Carts.SingleOrDefaultAsync(x=>x.CartId == cartId);
            if(result == null)
                return;
          
            var cartMessage = new CartMessage
            {
                CartId = cartId,
                CartItems = cartItemRequest,
                PriceListId = result.PriceListId
            };
            var nameValueEntry = new NameValueEntry[]
            {
                new NameValueEntry(nameof(cartItemRequest),JsonSerializer.Serialize(cartMessage))
            };

            await _database.StreamAddAsync("config-stream", nameValueEntry);
        }

        public async Task<bool> UpdateCartItem(IEnumerable<CartItemUpdateRequest> cartItemRequests, Guid cartId)
        {
            var batch =_database.CreateBatch();
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
            //await _database.ha
            //await _database.HashSetAsync($"h-cpq-{cartId}", new HashEntry[] { new HashEntry("StatusId", CartStatus.Priced.ToString()) });
            //var result = await _database.StringGetAsync($"s-cpq-{cartId}-lines");
            //if(!result.IsNullOrEmpty)
            //{
            //  var redisCartItems = JsonSerializer.Deserialize<List<CartItemData>>(result!);
            //  if(redisCartItems != null && redisCartItems.Any())
            //    {
            //        foreach(var item in redisCartItems)
            //        {
            //            var newItem = cartItemRequests.FirstOrDefault(y => y.CartItemId == item.CartItemId);
            //            item.Price = newItem.Price;
            //            item.Currency = newItem.Currency;
            //        }
            //        var jsonString = JsonSerializer.Serialize(redisCartItems);
            //        if(await _database.StringSetAsync($"s-cpq-{cartId}-lines", jsonString))
            //        {
            //            await
            //        }
            //    }
            //}
            //var cartItems = _dbContext.CartItemDatas.Where(x => x.CartId == cartId).ToList();
            //if (cartItems != null && cartItems.Any())
            //{
            //    cartItems.ForEach(async x =>
            //    {
            //        var updateItem = cartItemRequests.FirstOrDefault(y => x.CartItemId == y.CartItemId);
            //        if (updateItem != null)
            //        {
            //            x.Price = updateItem.Price;
            //            x.Currency = updateItem.Currency;
            //            await _dbContext.CartItemDatas
            //                .Where(u => u.CartItemId == x.CartItemId)
            //                .ExecuteUpdateAsync(s => s
            //                .SetProperty(b => b.Price, x.Price)
            //                .SetProperty(b => b.Currency, x.Currency));
            //        }
            //    });
            //    await _dbContext.CartItemDatas.LoadAsync();
            //    if (await _dbContext.CartItemDatas.Where(item=>item.CartId==cartId).AllAsync(x => x.Price != null))
            //    {
            //        await _dbContext.Carts
            //              .Where(u => u.CartId == cartId)
            //              .ExecuteUpdateAsync(s => s
            //              .SetProperty(b => b.StatusId, CartStatus.Priced));
            //    }
            //    return true;
            //}
            return true;
        }

        public async Task<CartDetailResponse> GetCartItems(Guid id)
        {
            var cart = new CartDetailResponse
            {
                CartId = id,
                StatusId = CartStatus.Created,
                CartItems= new List<CartItemInfo>()
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
