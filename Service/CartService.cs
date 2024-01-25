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
    //TODO: cart is getting saved in Db and redis. DO we need?
    //TODO: Redis data structure to use.
    //Exception, validation etc.
    public class CartService : ICartService
    {
        private readonly string _adminServiceUrl;
        private readonly IConfiguration _configuration;
        private readonly CartDbContext _dbContext;
        private readonly IDatabase _database;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ActivitySource _activitySource = new(Instrumentation.ActivitySourceName);
        public CartService(CartDbContext dbContext, IConnectionMultiplexer connectionMultiplexer, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _configuration = configuration;
            _dbContext = dbContext;            
            _connectionMultiplexer = connectionMultiplexer;
            _database = _connectionMultiplexer.GetDatabase();
            _httpClientFactory = httpClientFactory;
            _adminServiceUrl = _configuration.GetValue<string>("adminUrl") ?? "https://localhost:7190/api";
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
            return cart;
        }
        public async Task<bool> IsCartExists(Guid id)
        {
            var cart = await _dbContext.Carts.FindAsync(id);
            return cart != null;

        }

        public async Task<bool> IsPriceIdExists(Guid id)
        {
            var client = _httpClientFactory.CreateClient();
            var httpResponse = await client.GetAsync($"{_adminServiceUrl}/pricelist/{id}");
            if (httpResponse != null && httpResponse.IsSuccessStatusCode)
            {
                return true;
            }
            return false;
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
