using Microsoft.EntityFrameworkCore;
using WalletService.Domain.Entities;
using WalletService.Domain.Repositories;

namespace WalletService.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly WalletDbContext _context;

    public TransactionRepository(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<IEnumerable<Transaction>> GetByWalletIdAsync(
        Guid walletId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions.CountAsync(t => t.WalletId == walletId, cancellationToken);
    }
}