namespace WalletService.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid WalletId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BalanceBefore { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string? Description { get; private set; }
    public string? ReferenceId { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Transaction() { }

    public static Transaction Create(
        Guid walletId,
        TransactionType type,
        decimal amount,
        decimal balanceBefore,
        decimal balanceAfter,
        string currency,
        string? description = null,
        string? referenceId = null)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            Type = type,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            Currency = currency,
            Description = description,
            ReferenceId = referenceId,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer_In,
    Transfer_Out,
    Fee,
    Refund
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed,
    Reversed
}