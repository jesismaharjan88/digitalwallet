namespace WalletService.API.DTOs;

public record WalletResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public decimal Balance { get; init; }
    public string Currency { get; init; } = "USD";
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
