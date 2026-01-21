using WalletService.Domain.Entities;

namespace WalletService.Domain.Repositories;

public interface IWalletRepository
{
    Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Wallet> AddAsync(Wallet wallet, CancellationToken cancellationToken = default);
    Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default);
    Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}