namespace Demo.Contracts.Requests;

/// <summary>
/// Response containing product details from the Catalog service.
/// </summary>
public sealed class GetProductResponse
{
    public required Guid ProductId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
    public required int StockQuantity { get; init; }
    public required bool IsAvailable { get; init; }
}
