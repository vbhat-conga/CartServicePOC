using CartServicePOC.DataModel;
using CartServicePOC.Extensions;
using CartServicePOC.Helper;
using CartServicePOC.Model;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Diagnostics;

namespace CartServicePOC.Service.Cart
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

        public async Task<CartResponseDetail> GetCart(Guid id)
        {
            var cart = new CartResponseDetail
            {
                CartId = id,
                Status = CartStatus.Created
            };
            using var cacheActivity = _activitySource.StartActivity($"{nameof(CartService)}: GetCart : Getting from cache", ActivityKind.Server);
            {
                cacheActivity?.AddTag("CartId", id);
                var hasEntry = await _database.HashGetAllAsync($"h-cpq-{id}");
                var cartInfo = RedisExtension.ConvertFromRedis<CartResponseDetail>(hasEntry);
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

        public async Task<Guid> SaveCart(CreateCartRequest createCartRequest)
        {
            var cartdata = new CartData
            {
                CartId = Guid.NewGuid(),
                PriceListId = createCartRequest.PriceList.Id,
                Name = createCartRequest.Name,
                Status = CartStatus.Created
            };
            using var cacheActivity = _activitySource.StartActivity($"{nameof(CartService)} : Saving to cache", ActivityKind.Server);
            {
                cacheActivity?.AddTag("CartId", cartdata.CartId);
                var hashEntry = cartdata.ToHashEntries();
                await _database.HashSetAsync($"h-cpq-{cartdata.CartId}", hashEntry);
            }
            using var sqlActivity = _activitySource.StartActivity($"{nameof(CartService)} : Saving to sql", ActivityKind.Server);
            {
                sqlActivity?.AddTag("CartId", cartdata.CartId);
                await _dbContext.Carts.AddAsync(cartdata);
                await _dbContext.SaveChangesAsync();
            }

            return cartdata.CartId;
        }

        public async Task<bool> UpdateCart(UpdateCartRequest cartUpdateRequest)
        {
            using var cacheActivity = _activitySource.StartActivity($"{nameof(CartService)}: UpdateCart : Saving to cache", ActivityKind.Server);
            {
                cacheActivity?.AddTag("CartId", cartUpdateRequest.CartId);
                var hashEntry = cartUpdateRequest.ToHashEntries();
                await _database.HashSetAsync($"h-cpq-{cartUpdateRequest.CartId}", hashEntry);
            }
            using var sqlActivity = _activitySource.StartActivity($"{nameof(CartService)} : UpdateCart : Saving to sql", ActivityKind.Server);
            {
                sqlActivity?.AddTag("CartId", cartUpdateRequest.CartId);
                await _dbContext.Carts
                .Where(u => u.CartId == cartUpdateRequest.CartId)
                .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Price, cartUpdateRequest.Price)
                .SetProperty(b => b.Status, cartUpdateRequest.Status));
                await _dbContext.SaveChangesAsync();
            }

            return true;
        }
    }
}
