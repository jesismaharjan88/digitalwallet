using WalletService.Domain.Entities;

namespace WalletService.Domain.Repositories;

public interface ITransactionRepository
{
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByWalletIdAsync(
        Guid walletId, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default);
    Task<int> GetCountByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default);
}