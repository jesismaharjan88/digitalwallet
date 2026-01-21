namespace WalletService.API.DTOs;

public record TransactionHistoryResponse
{
    public IEnumerable<TransactionResponse> Transactions { get; init; } = Array.Empty<TransactionResponse>();
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}
