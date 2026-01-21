using StackExchange.Redis;
using System.Text.Json;

namespace WalletService.API.Services;

public class WalletCacheService : IWalletCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<WalletCacheService> _logger;
    private const string CacheKeyPrefix = "wallet:balance:";
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(15);

    public WalletCacheService(IConnectionMultiplexer redis, ILogger<WalletCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<decimal?> GetBalanceAsync(Guid walletId)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = GetCacheKey(walletId);
            var value = await db.StringGetAsync(key);

            if (value.HasValue)
            {
                _logger.LogInformation("Cache hit for wallet {WalletId}", walletId);
                return decimal.Parse(value!);
            }

            _logger.LogInformation("Cache miss for wallet {WalletId}", walletId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving balance from cache for wallet {WalletId}", walletId);
            return null;
        }
    }

    public async Task SetBalanceAsync(Guid walletId, decimal balance, TimeSpan? expiration = null)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = GetCacheKey(walletId);
            var value = balance.ToString();
            var exp = expiration ?? DefaultExpiration;

            await db.StringSetAsync(key, value, exp);
            _logger.LogInformation("Cached balance for wallet {WalletId}: {Balance}", walletId, balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting balance cache for wallet {WalletId}", walletId);
        }
    }

    public async Task RemoveBalanceAsync(Guid walletId)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = GetCacheKey(walletId);
            await db.KeyDeleteAsync(key);
            _logger.LogInformation("Removed cache for wallet {WalletId}", walletId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing balance cache for wallet {WalletId}", walletId);
        }
    }

    private static string GetCacheKey(Guid walletId) => $"{CacheKeyPrefix}{walletId}";
}
