namespace WalletService.API.Services;

public interface IWalletCacheService
{
    Task<decimal?> GetBalanceAsync(Guid walletId);
    Task SetBalanceAsync(Guid walletId, decimal balance, TimeSpan? expiration = null);
    Task RemoveBalanceAsync(Guid walletId);
}
