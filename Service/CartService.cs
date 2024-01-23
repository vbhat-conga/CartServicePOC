using CartServicePOC.Controllers;
using CartServicePOC.DataModel;
using CartServicePOC.Extensions;
using CartServicePOC.Helper;
using CartServicePOC.Model;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text.Json;

namespace CartServicePOC.Service
{
    public class CartService : ICartService
    {
        private readonly CartDbContext _dbContext;
        private readonly IDatabase _database;
        private readonly ActivitySource _activitySource = new(Instrumentation.ActivitySourceName);
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
            using var cacheActivity = _activitySource.StartActivity($"{nameof(CartService)}: GetCart : Getting from cache", ActivityKind.Server);
            {
                cacheActivity?.AddTag("CartId", id);
                var hasEntry = await _database.HashGetAllAsync($"h-cpq-{id}");
                var cartInfo = RedisExtension.ConvertFromRedis<CartData>(hasEntry);
                if (cartInfo != null)
                    return cartInfo;
            }
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
            using var cacheActivity = _activitySource.StartActivity($"{nameof(CartService)} : Saving to cache", ActivityKind.Server);
            {
                cacheActivity?.AddTag("CartId", cart.CartId);
                var hashEntry = RedisExtension.ToHashEntries(cart);
                await _database.HashSetAsync($"h-cpq-{cart.CartId}", hashEntry);
            }
            using var sqlActivity = _activitySource.StartActivity($"{nameof(CartService)} : Saving to sql", ActivityKind.Server);
            {
                sqlActivity?.AddTag("CartId", cart.CartId);
                await _dbContext.Carts.AddAsync(cart);
                await _dbContext.SaveChangesAsync();
            }

            return cart.CartId;
        }

        public async Task<bool> UpdateCart(CartUpdateRequest cartUpdateRequest)
        {
            using var cacheActivity = _activitySource.StartActivity($"{nameof(CartService)}: UpdateCart : Saving to cache", ActivityKind.Server);
            {
                cacheActivity?.AddTag("CartId", cartUpdateRequest.CartId);
                var hashEntry = RedisExtension.ToHashEntries(cartUpdateRequest);
                await _database.HashSetAsync($"h-cpq-{cartUpdateRequest.CartId}", hashEntry);
            }
            using var sqlActivity = _activitySource.StartActivity($"{nameof(CartService)} : UpdateCart : Saving to sql", ActivityKind.Server);
            {
                sqlActivity?.AddTag("CartId", cartUpdateRequest.CartId);
                await _dbContext.Carts
                .Where(u => u.CartId == cartUpdateRequest.CartId)
                .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Price, cartUpdateRequest.Price)
                .SetProperty(b => b.StatusId, cartUpdateRequest.StatusId));
                await _dbContext.SaveChangesAsync();
            }
         
            return true;
        }
    }
}
