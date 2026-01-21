namespace WalletService.API.DTOs;

public record TransactionResponse
{
    public Guid Id { get; init; }
    public Guid WalletId { get; init; }
    public string Type { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal BalanceBefore { get; init; }
    public decimal BalanceAfter { get; init; }
    public string Currency { get; init; } = "USD";
    public string? Description { get; init; }
    public string? ReferenceId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
