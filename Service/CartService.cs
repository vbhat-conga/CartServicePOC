using CartServicePOC.DataModel;
using CartServicePOC.Extensions;
using CartServicePOC.Model;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace CartServicePOC.Service
{
    public class CartService : ICartService
    {
        private readonly CartDbContext _dbContext;
        private readonly IDatabase _database;
        public CartService(CartDbContext dbContext)
        {
            _dbContext = dbContext;
            var redis = ConnectionMultiplexer.Connect("localhost:6379");
            _database = redis.GetDatabase();
        }

        public async Task<CartData> GetCart(Guid id)
        {
            var cart = new CartData
            {
                CartId = id,
                StatusId = CartStatus.Created
            };
            var hasEntry = await _database.HashGetAllAsync($"h-cpq-{id}");
            var cartInfo = RedisExtension.ConvertFromRedis<CartData>(hasEntry);
            if (cartInfo != null)
                return cartInfo;
            //var sets = await _database.SetMembersAsync($"se-cpq-{id}");
            //var batch = _database.CreateBatch();
            //var tasks = new List<Task<RedisValue>>();
            //foreach (var set in sets)
            //{
            //    tasks.Add(batch.HashGetAsync($"h-cpq-{set}", new RedisValue("Price")));
            //}
            //batch.Execute();
            //var values = await Task.WhenAll(tasks);
            //if (values.All(x => !x.IsNullOrEmpty))
            //{
            //    cart.StatusId = CartStatus.Priced;
            //}
            return cart;
        }

        public async Task<CartData> GetCartStatus(Guid id)
        {
            var cart = new CartData
            {
                CartId = id,
                StatusId = CartStatus.Created
            };
            var sets = await _database.SetMembersAsync($"se-cpq-{id}");
            var batch = _database.CreateBatch();
            var tasks = new List<Task<RedisValue>>();
            foreach ( var set in sets)
            {
                tasks.Add(batch.HashGetAsync($"h-cpq-{set}", new RedisValue("price")));
            }
            batch.Execute();
            var values = await Task.WhenAll(tasks);
            if(values.All(x=>!x.IsNullOrEmpty))
            {
                cart.StatusId=CartStatus.Priced;
            }
            return cart;
            //var hashEntries = await _database.HashGetAllAsync($"h-cpq-{id}");
            //var cart = RedisExtension.ConvertFromRedis<CartData>(hashEntries);
            //if (cart == null)
            //{
            //    var result = await _dbContext.Carts.FindAsync(id);
            //    if (result == null)
            //    {
            //        return new CartData();
            //    }
            //    return result;
            //}
            //else
            //{
            //    return cart;
            //}
        }

        public async Task<bool> IsCartExists(Guid id)
        {
            var cart = await _dbContext.Carts.FindAsync(id);
            return cart != null;

        }

        public async Task<bool> IsPriceIdExists(Guid id)
        {
            // make admin service call.
            return await Task.FromResult(true);
        }

        public async Task<Guid> SaveCart(CartData cart)
        {
            var hashEntry = RedisExtension.ToHashEntries(cart);
            await _database.HashSetAsync($"h-cpq-{cart.CartId}", hashEntry);
            await _dbContext.Carts.AddAsync(cart);
            await _dbContext.SaveChangesAsync();
            return cart.CartId;
        }

        public async Task<bool> UpdateCart(CartUpdateRequest cartUpdateRequest)
        {
            var hashEntry = RedisExtension.ToHashEntries(cartUpdateRequest);
            await _database.HashSetAsync($"h-cpq-{cartUpdateRequest.CartId}", hashEntry);
            await _dbContext.Carts
                            .Where(u => u.CartId == cartUpdateRequest.CartId)
                            .ExecuteUpdateAsync(s => s
                            .SetProperty(b => b.Price, cartUpdateRequest.Price)
                            .SetProperty(b => b.StatusId, cartUpdateRequest.StatusId));
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
