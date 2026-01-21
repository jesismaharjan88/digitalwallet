namespace Common.Contracts.Events;

/// <summary>
/// Event published when a new user is registered
/// </summary>
public record UserCreatedEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Event published when a wallet is created
/// </summary>
public record WalletCreatedEvent
{
    public Guid WalletId { get; init; }
    public Guid UserId { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Event published when a transaction is completed
/// </summary>
public record TransactionCompletedEvent
{
    public Guid TransactionId { get; init; }
    public Guid WalletId { get; init; }
    public Guid UserId { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal NewBalance { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime CompletedAt { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Event published when a transfer between wallets is completed
/// </summary>
public record TransferCompletedEvent
{
    public Guid TransferId { get; init; }
    public Guid FromWalletId { get; init; }
    public Guid ToWalletId { get; init; }
    public Guid FromUserId { get; init; }
    public Guid ToUserId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime CompletedAt { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Event published when a deposit is completed
/// </summary>
public record DepositCompletedEvent
{
    public Guid DepositId { get; init; }
    public Guid WalletId { get; init; }
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string PaymentMethod { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; }
}

/// <summary>
/// Event published when a withdrawal is completed
/// </summary>
public record WithdrawalCompletedEvent
{
    public Guid WithdrawalId { get; init; }
    public Guid WalletId { get; init; }
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string BankAccount { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; }
}

/// <summary>
/// Event published when a transaction fails
/// </summary>
public record TransactionFailedEvent
{
    public Guid TransactionId { get; init; }
    public Guid WalletId { get; init; }
    public Guid UserId { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string FailureReason { get; init; } = string.Empty;
    public DateTime FailedAt { get; init; }
}