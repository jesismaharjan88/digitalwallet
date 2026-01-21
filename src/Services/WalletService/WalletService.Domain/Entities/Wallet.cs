namespace WalletService.Domain.Entities;

public class Wallet
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Balance { get; private set; }
    public string Currency { get; private set; } = "USD";
    public WalletStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Wallet() { }

    public static Wallet Create(Guid userId, string currency = "USD")
    {
        return new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = 0,
            Currency = currency,
            Status = WalletStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Credit(decimal amount, string description)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        if (Status != WalletStatus.Active)
            throw new InvalidOperationException("Wallet is not active");

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Debit(decimal amount, string description)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        if (Status != WalletStatus.Active)
            throw new InvalidOperationException("Wallet is not active");

        if (Balance < amount)
            throw new InvalidOperationException("Insufficient funds");

        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Freeze()
    {
        Status = WalletStatus.Frozen;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unfreeze()
    {
        Status = WalletStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        if (Balance != 0)
            throw new InvalidOperationException("Cannot close wallet with non-zero balance");

        Status = WalletStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum WalletStatus
{
    Active,
    Frozen,
    Closed
}